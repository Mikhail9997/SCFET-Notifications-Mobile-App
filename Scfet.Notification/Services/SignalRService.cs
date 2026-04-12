using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Models.SignalR;
using Scfet.Notification.Policies;

namespace Scfet.Notification.Services
{
    public class SignalRService
    {
        private HubConnection _hubConnection;
        private HubConnection _channelHubConnection;
        private readonly ITokenService _tokenService;
        private readonly LoginService _loginService;
        private bool _isReconnecting = false;
        private CancellationTokenSource _reconnectCts;
        private System.Timers.Timer _tokenCheckTimer;
        private DateTime _lastTokenRefreshTime;

        private readonly string BaseUrl = "https://amorously-preeminent-godwit.cloudpub.ru";
        //http://localhost:5050/notificationHub
        //https://amorously-preeminent-godwit.cloudpub.ru
        //http://81.94.159.27:5050

        #region Notification Events
        public event Action<Models.Notification>? OnNotificationReceived;
        public event Action<Guid>? OnNotificationRemove;
        public event Action<Guid>? OnNotificationRead;
        public event Action<Models.Notification>? OnNotificationUpdate;
        public event Action<Reply>? OnReplyRead;
        public event Action<Reply>? OnReplyUpdate;
        public event Action<Guid>? OnReplyRemove;
        #endregion

        #region Channel Events
        public event Action<ChannelInvitationDto>? OnChannelInvitation;
        public event Action<ChannelInvitationDto>? OnInvitationAccepted;
        public event Action<ChannelInvitationDto>? OnInvitationDeclined;
        public event Action<ChannelInvitationDto>? OnInvitationCancelled;
        public event Action<UserJoinedEvent>? OnUserJoined;
        public event Action<UserLeftEvent>? OnUserLeft;
        public event Action<UserTypingEvent>? OnUserTyping;
        public event Action<NewMessageEvent>? OnNewMessage;
        public event Action<MessageUpdatedEvent>? OnMessageUpdated;
        public event Action<MessageDeletedEvent>? OnMessageDeleted;
        #endregion

        public event Action? OnConnectionLost;
        public event Action? OnConnectionRestored;

        public SignalRService(ITokenService tokenService, LoginService loginService)
        {
            _tokenService = tokenService;
            _loginService = loginService;

            _tokenService.OnTokensInvalid += OnTokensInvalid;

            _tokenCheckTimer = new System.Timers.Timer(300000);
            _tokenCheckTimer.Elapsed += async (sender, e) => await CheckAndRefreshTokenIfNeededAsync();
            _tokenCheckTimer.AutoReset = true;
        }

        private void OnTokensInvalid()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisconnectAsync();
                OnConnectionLost?.Invoke();
                await Application.Current.MainPage.DisplayAlert(
                    "Сессия истекла",
                    "Пожалуйста, войдите снова",
                    "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            });
        }

        public async Task ConnectAsync()
        {
            await ConnectNotificationHubAsync();
            await ConnectChannelHubAsync();
        }

        #region Notification Hub
        private async Task ConnectNotificationHubAsync()
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                return;

            var token = await _tokenService.GetValidAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No valid token available for SignalR connection");
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{BaseUrl}/notificationHub", options =>
                {
                    options.AccessTokenProvider = async () => await _tokenService.GetValidAccessTokenAsync();
                    options.SkipNegotiation = true;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect(new SignalRRetryPolicy())
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                .Build();

            SetupNotificationHubCallbacks();

            _hubConnection.Closed += OnConnectionClosedAsync;
            _hubConnection.Reconnecting += OnReconnectingAsync;
            _hubConnection.Reconnected += OnReconnectedAsync;

            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("NotificationHub connected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationHub connection error: {ex.Message}");
            }
        }

        private void SetupNotificationHubCallbacks()
        {
            _hubConnection.On<Models.Notification>("ReceiveNotification", (notification) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnNotificationReceived?.Invoke(notification));
            });

            _hubConnection.On<Guid>("RemovedNotification", (notificationId) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnNotificationRemove?.Invoke(notificationId));
            });

            _hubConnection.On<Guid>("NotificationRead", (notificationId) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnNotificationRead?.Invoke(notificationId));
            });

            _hubConnection.On<Models.Notification>("UpdateNotification", (notification) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnNotificationUpdate?.Invoke(notification));
            });

            _hubConnection.On<Reply>("ReceiveNotificationReply", (reply) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnReplyRead?.Invoke(reply));
            });

            _hubConnection.On<Guid>("RemoveNotificationReply", (id) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnReplyRemove?.Invoke(id));
            });

            _hubConnection.On<Reply>("UpdateNotificationReply", (reply) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnReplyUpdate?.Invoke(reply));
            });

            // Каналы для NotificationHub
            _hubConnection.On<ChannelInvitationDto>("ChannelInvitation", (invitation) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnChannelInvitation?.Invoke(invitation));
            });

            _hubConnection.On<ChannelInvitationDto>("InvitationAccepted", (invitation) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnInvitationAccepted?.Invoke(invitation));
            });

            _hubConnection.On<ChannelInvitationDto>("InvitationDeclined", (invitation) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnInvitationDeclined?.Invoke(invitation));
            });

            _hubConnection.On<ChannelInvitationDto>("InvitationCancelled", (invitation) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnInvitationCancelled?.Invoke(invitation));
            });
        }
        #endregion

        #region Channel Hub
        private async Task ConnectChannelHubAsync()
        {
            if (_channelHubConnection?.State == HubConnectionState.Connected)
                return;

            var token = await _tokenService.GetValidAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No valid token available for ChannelHub connection");
                return;
            }

            _channelHubConnection = new HubConnectionBuilder()
                .WithUrl($"{BaseUrl}/channelHub", options =>
                {
                    options.AccessTokenProvider = async () => await _tokenService.GetValidAccessTokenAsync();
                    options.SkipNegotiation = true;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect(new SignalRRetryPolicy())
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                .Build();

            SetupChannelHubCallbacks();

            _channelHubConnection.Closed += OnChannelConnectionClosedAsync;
            _channelHubConnection.Reconnecting += OnChannelReconnectingAsync;
            _channelHubConnection.Reconnected += OnChannelReconnectedAsync;

            try
            {
                await _channelHubConnection.StartAsync();
                Console.WriteLine("ChannelHub connected successfully");
                _tokenCheckTimer.Start();
                _lastTokenRefreshTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChannelHub connection error: {ex.Message}");
            }
        }

        private void SetupChannelHubCallbacks()
        {
            _channelHubConnection.On<UserJoinedEvent>("UserJoined", (eventData) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnUserJoined?.Invoke(eventData));
            });

            _channelHubConnection.On<UserLeftEvent>("UserLeft", (eventData) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnUserLeft?.Invoke(eventData));
            });

            _channelHubConnection.On<UserTypingEvent>("UserTyping", (eventData) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnUserTyping?.Invoke(eventData));
            });

            _channelHubConnection.On<NewMessageEvent>("NewMessage", (message) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnNewMessage?.Invoke(message));
            });

            _channelHubConnection.On<MessageUpdatedEvent>("MessageUpdated", (message) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnMessageUpdated?.Invoke(message));
            });

            _channelHubConnection.On<MessageDeletedEvent>("MessageDeleted", (eventData) =>
            {
                MainThread.BeginInvokeOnMainThread(() => OnMessageDeleted?.Invoke(eventData));
            });
        }

        private async Task OnChannelConnectionClosedAsync(Exception? exception)
        {
            Console.WriteLine($"ChannelHub connection closed: {exception?.Message}");

            if (!_reconnectCts?.Token.IsCancellationRequested ?? false)
            {
                await Task.Delay(5000);
                await TryReconnectChannelHubAsync();
            }
        }

        private async Task OnChannelReconnectingAsync(Exception? exception)
        {
            _isReconnecting = true;
            Console.WriteLine($"ChannelHub reconnecting: {exception?.Message}");
            await _tokenService.RefreshTokenAsync();
        }

        private async Task OnChannelReconnectedAsync(string? connectionId)
        {
            _isReconnecting = false;
            Console.WriteLine($"ChannelHub reconnected with ID: {connectionId}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnConnectionRestored?.Invoke();
            });
        }

        private async Task TryReconnectChannelHubAsync()
        {
            try
            {
                if (_channelHubConnection?.State != HubConnectionState.Connected)
                {
                    var token = await _tokenService.GetValidAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        await _channelHubConnection.StartAsync();
                        Console.WriteLine("ChannelHub reconnected successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChannelHub reconnection failed: {ex.Message}");
                await Task.Delay(10000);
                await TryReconnectChannelHubAsync();
            }
        }
        #endregion

        #region Channel Hub Methods
        public async Task JoinChannelAsync(Guid channelId)
        {
            if (_channelHubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _channelHubConnection.InvokeAsync("JoinChannel", channelId);
                    Console.WriteLine($"Joined channel: {channelId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error joining channel {channelId}: {ex.Message}");
                }
            }
        }

        public async Task LeaveChannelAsync(Guid channelId)
        {
            if (_channelHubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _channelHubConnection.InvokeAsync("LeaveChannel", channelId);
                    Console.WriteLine($"Left channel: {channelId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error leaving channel {channelId}: {ex.Message}");
                }
            }
        }

        public async Task SendTypingStatusAsync(Guid channelId, bool isTyping)
        {
            if (_channelHubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _channelHubConnection.InvokeAsync("Typing", channelId, isTyping);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending typing status: {ex.Message}");
                }
            }
        }
        #endregion

        #region Common Methods
        private async Task CheckAndRefreshTokenIfNeededAsync()
        {
            try
            {
                if (!(await _loginService.IsLoggedIn()))
                    return;

                var accessToken = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(accessToken))
                    return;

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(accessToken))
                    return;

                var jwtToken = handler.ReadJwtToken(accessToken);
                var timeUntilExpiry = jwtToken.ValidTo - DateTime.UtcNow;

                if (timeUntilExpiry.TotalMinutes < 10)
                {
                    Console.WriteLine("Token is about to expire, refreshing...");
                    await _tokenService.RefreshTokenAsync();
                    _lastTokenRefreshTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token check error: {ex.Message}");
            }
        }

        private async Task OnConnectionClosedAsync(Exception? exception)
        {
            Console.WriteLine($"NotificationHub connection closed: {exception?.Message}");
            MainThread.BeginInvokeOnMainThread(() => OnConnectionLost?.Invoke());
        }

        private async Task OnReconnectingAsync(Exception? exception)
        {
            _isReconnecting = true;
            Console.WriteLine($"NotificationHub reconnecting: {exception?.Message}");
            await _tokenService.RefreshTokenAsync();
        }

        private async Task OnReconnectedAsync(string? connectionId)
        {
            _isReconnecting = false;
            Console.WriteLine($"NotificationHub reconnected with ID: {connectionId}");
            MainThread.BeginInvokeOnMainThread(() => OnConnectionRestored?.Invoke());
        }

        public async Task DisconnectAsync()
        {
            _tokenCheckTimer.Stop();
            _tokenCheckTimer.Dispose();
            _tokenService.OnTokensInvalid -= OnTokensInvalid;
            _reconnectCts?.Cancel();

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
                    Console.WriteLine($"Error disconnecting NotificationHub: {ex.Message}");
                }
            }

            if (_channelHubConnection != null)
            {
                _channelHubConnection.Closed -= OnChannelConnectionClosedAsync;
                _channelHubConnection.Reconnecting -= OnChannelReconnectingAsync;
                _channelHubConnection.Reconnected -= OnChannelReconnectedAsync;

                try
                {
                    await _channelHubConnection.StopAsync();
                    await _channelHubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting ChannelHub: {ex.Message}");
                }
            }
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("MarkAsRead", notificationId);
            }
        }

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public bool IsChannelHubConnected => _channelHubConnection?.State == HubConnectionState.Connected;
        #endregion
    }
}
