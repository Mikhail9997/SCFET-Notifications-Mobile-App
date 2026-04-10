using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Scfet.Notification.Models;
using Scfet.Notification.Policies;

namespace Scfet.Notification.Services
{
    public class NotificationService
    {
        private HubConnection _hubConnection;
        private readonly ITokenService _tokenService;
        private readonly LoginService _loginService;
        private bool _isReconnecting = false;
        private CancellationTokenSource _reconnectCts;
        private System.Timers.Timer _tokenCheckTimer;
        private DateTime _lastTokenRefreshTime;

        private readonly string BaseUrl = "http://81.94.159.27:5050";
        //http://localhost:5050/notificationHub
        //https://amorously-preeminent-godwit.cloudpub.ru
        //http://81.94.159.27:5050

        public event Action<Models.Notification>? OnNotificationReceived;

        public event Action<Guid>? OnNotificationRemove;
        public event Action<Guid>? OnNotificationRead;
        public event Action<Models.Notification>? OnNotificationUpdate;

        public event Action<Reply> OnReplyRead;
        public event Action<Reply> OnReplyUpdate;
        public event Action<Guid> OnReplyRemove;

        public event Action? OnConnectionLost;
        public event Action? OnConnectionRestored;


        public NotificationService(ITokenService tokenService, LoginService loginService)
        {
            _tokenService = tokenService;
            _loginService = loginService;          

            // Подписываемся на события TokenService
            _tokenService.OnTokensInvalid += OnTokensInvalid;

            // Таймер для периодической проверки токена (каждые 5 минут)
            _tokenCheckTimer = new System.Timers.Timer(300000); // 5 минут
            _tokenCheckTimer.Elapsed += async (sender, e) => await CheckAndRefreshTokenIfNeededAsync();
            _tokenCheckTimer.AutoReset = true;
        }

        private void OnTokensInvalid()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisconnectAsync();
                OnConnectionLost?.Invoke();

                // Показываем сообщение
                await Application.Current.MainPage.DisplayAlert(
                    "Сессия истекла",
                    "Пожалуйста, войдите снова",
                    "OK");

                await Shell.Current.GoToAsync("//LoginPage");
            });
        }

        public async Task ConnectAsync()
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                return;

            _reconnectCts = new CancellationTokenSource();

            // Используем TokenService для получения токена
            var token = await _tokenService.GetValidAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No valid token available for SignalR connection");
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{BaseUrl}/notificationHub", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        return await _tokenService.GetValidAccessTokenAsync();
                    };

                    options.SkipNegotiation = true;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect(new SignalRRetryPolicy())
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                .Build();

            SetupHubCallbacks();

            _hubConnection.Closed += OnConnectionClosedAsync;
            _hubConnection.Reconnecting += OnReconnectingAsync;
            _hubConnection.Reconnected += OnReconnectedAsync;

            try
            {
                await _hubConnection.StartAsync(_reconnectCts.Token);
                Console.WriteLine("SignalR connected successfully");

                // Запускаем таймер проверки токена
                _tokenCheckTimer.Start();
                _lastTokenRefreshTime = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                Console.WriteLine($"SignalR connection error: {ex.Message}");
                await HandleConnectionErrorAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected SignalR error: {ex.Message}");
            }
        }

        private void SetupHubCallbacks()
        {
            _hubConnection.On<Models.Notification>("ReceiveNotification", (notification) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationReceived?.Invoke(notification);
                });
            });

            _hubConnection.On<Guid>("RemovedNotification", (notificationId) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationRemove?.Invoke(notificationId);
                });
            });

            _hubConnection.On<Guid>("NotificationRead", (notificationId) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationRead?.Invoke(notificationId);
                });
            });

            _hubConnection.On<Models.Notification>("UpdateNotification", (notification) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnNotificationUpdate?.Invoke(notification);
                });
            });

            // Ответы
            _hubConnection.On<Reply>("ReceiveNotificationReply", (reply) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnReplyRead?.Invoke(reply);
                });
            });

            _hubConnection.On<Guid>("RemoveNotificationReply", (id) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnReplyRemove?.Invoke(id);
                });
            });

            _hubConnection.On<Reply>("UpdateNotificationReply", (reply) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnReplyUpdate?.Invoke(reply);
                });
            });
        }

        // периодическая проверка и обновление токена
        private async Task CheckAndRefreshTokenIfNeededAsync()
        {
            try
            {
                if (!(await _loginService.IsLoggedIn()) || _hubConnection?.State != HubConnectionState.Connected)
                {
                    return;
                }

                var accessToken = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(accessToken))
                {
                    return;
                }

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(accessToken))
                {
                    return;
                }

                var jwtToken = handler.ReadJwtToken(accessToken);
                var expires = jwtToken.ValidTo;

                // Проверяем, сколько времени осталось до истечения токена
                var timeUntilExpiry = expires - DateTime.UtcNow;

                Console.WriteLine($"Token expiry check: Expires at {expires}, Time until expiry: {timeUntilExpiry.TotalMinutes:F1} minutes");

                // Если токен истекает в течение 10 минут, обновляем его через TokenService
                if (timeUntilExpiry.TotalMinutes < 10)
                {
                    Console.WriteLine("Token is about to expire, refreshing...");
                    await _tokenService.RefreshTokenAsync();

                    // Обновляем время последнего обновления
                    _lastTokenRefreshTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token check error: {ex.Message}");
            }
        }

        private async Task<string?> TryRefreshTokenSilentlyAsync()
        {
            try
            {
                var accessToken = await SecureStorage.GetAsync("access_token");
                var refreshToken = await SecureStorage.GetAsync("refresh_token");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine("Cannot refresh: missing tokens");
                    return null;
                }

                using var tempHttpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                var refreshRequest = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                var json = JsonSerializer.Serialize(refreshRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine("Attempting to refresh token...");
                var response = await tempHttpClient.PostAsync($"{BaseUrl}/api/auth/refresh-token", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonSerializer.Deserialize<TokenData>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (tokenData != null)
                    {
                        Console.WriteLine("Token refreshed successfully");

                        // Сохраняем новые токены
                        await SecureStorage.SetAsync("access_token", tokenData.AccessToken);
                        await SecureStorage.SetAsync("refresh_token", tokenData.RefreshToken);

                        // Обновляем в LoginService
                        var auth = _loginService.GetCurrentAuth();
                        if (auth != null)
                        {
                            auth.AccessToken = tokenData.AccessToken;
                            _loginService.UpdateAuth(auth);
                        }

                        return tokenData.AccessToken;
                    }
                }
                else
                {
                    Console.WriteLine($"Token refresh failed with status: {response.StatusCode}");

                    // Если refresh токен невалиден, очищаем данные
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await HandleInvalidRefreshTokenAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Silent token refresh error: {ex.Message}");
            }

            return null;
        }

        private async Task HandleInvalidRefreshTokenAsync()
        {
            Console.WriteLine("Refresh token is invalid, logging out...");

            // Очищаем токены
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");

            // Вызываем logout
            await _loginService.Logout();

            // Останавливаем таймер
            _tokenCheckTimer.Stop();

            // Оповещаем UI о необходимости перелогиниться
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnConnectionLost?.Invoke();

                // Можно показать сообщение пользователю
                Application.Current.MainPage.DisplayAlert("Сессия истекла", "Пожалуйста, войдите снова", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            });
        }

        private async Task OnConnectionClosedAsync(Exception? exception)
        {
            Console.WriteLine($"SignalR connection closed: {exception?.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnConnectionLost?.Invoke();
            });

            // Пробуем переподключиться через некоторое время
            await Task.Delay(5000);

            if (!_reconnectCts.Token.IsCancellationRequested)
            {
                await TryReconnectAsync();
            }
        }

        private async Task OnReconnectingAsync(Exception? exception)
        {
            _isReconnecting = true;
            Console.WriteLine($"SignalR reconnecting: {exception?.Message}");

            // Пробуем обновить токен при переподключении
            await _tokenService.RefreshTokenAsync();
        }

        private async Task OnReconnectedAsync(string? connectionId)
        {
            _isReconnecting = false;
            Console.WriteLine($"SignalR reconnected with ID: {connectionId}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnConnectionRestored?.Invoke();
            });
        }

        private async Task HandleConnectionErrorAsync()
        {
            // Проверяем, не истек ли токен
            var token = await SecureStorage.GetAsync("access_token");
            if (string.IsNullOrEmpty(token))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnConnectionLost?.Invoke();
                });
            }
        }

        private async Task TryReconnectAsync()
        {
            try
            {
                if (_hubConnection?.State != HubConnectionState.Connected &&
                    !_reconnectCts.Token.IsCancellationRequested)
                {
                    // Используем TokenService для получения токена
                    var token = await _tokenService.GetValidAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        await _hubConnection.StartAsync(_reconnectCts.Token);
                        Console.WriteLine("SignalR reconnected successfully");
                    }
                    else
                    {
                        Console.WriteLine("Cannot reconnect: no valid token");
                        await HandleConnectionErrorAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reconnection failed: {ex.Message}");

                // Пробуем снова через увеличенный интервал
                await Task.Delay(10000);
                if (!_reconnectCts.Token.IsCancellationRequested)
                {
                    await TryReconnectAsync();
                }
            }
        }

        public async Task DisconnectAsync()
        {
            // Останавливаем таймер
            _tokenCheckTimer.Stop();
            _tokenCheckTimer.Dispose();

            // Отписываемся от событий
            _tokenService.OnTokensInvalid -= OnTokensInvalid;

            _reconnectCts.Cancel();

            if (_hubConnection != null)
            {
                _hubConnection.Closed -= OnConnectionClosedAsync;
                _hubConnection.Reconnecting -= OnReconnectingAsync;
                _hubConnection.Reconnected -= OnReconnectedAsync;

                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting SignalR: {ex.Message}");
                }
            }
        }

        // Добавляем метод для принудительной проверки токена
        public async Task<bool> CheckTokenValidityAsync()
        {
            return await TryRefreshTokenSilentlyAsync() != null;
        }

        public DateTime LastTokenRefreshTime => _lastTokenRefreshTime;

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("MarkAsRead", notificationId);
            }
        }

        public bool isConnected => _hubConnection?.State == HubConnectionState.Connected;
    }
}
