using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services
{
    public interface ITokenService
    {
        Task<string?> GetValidAccessTokenAsync();
        Task<bool> RefreshTokenAsync();
        Task<bool> IsTokenValidAsync();
        Task ClearTokensAsync();
        event Action OnTokensRefreshed;
        event Action OnTokensInvalid;
    }

    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly LoginService _loginService;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private bool _isRefreshing = false;
        private DateTime? _lastRefreshAttempt = null;

        private const string BaseUrl = "https://amorously-preeminent-godwit.cloudpub.ru/api";
        // http://81.94.159.27:5050/api
        // https://amorously-preeminent-godwit.cloudpub.ru/api

        public event Action? OnTokensRefreshed;
        public event Action? OnTokensInvalid;

        public TokenService(LoginService loginService)
        {
            _loginService = loginService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<string?> GetValidAccessTokenAsync()
        {
            var accessToken = await SecureStorage.GetAsync("access_token");

            if (string.IsNullOrEmpty(accessToken))
                return null;

            // Проверяем, не истек ли токен
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(accessToken))
                {
                    JwtSecurityToken jwtToken = handler.ReadJwtToken(accessToken);
                    var expires = jwtToken.ValidTo;

                    // Если токен еще валиден более 5 минут, возвращаем его
                    if (expires > DateTime.UtcNow.AddMinutes(5))
                        return accessToken;

                    // Если токен скоро истечет, пробуем обновить
                    if (expires > DateTime.UtcNow)
                    {
                        var refreshed = await RefreshTokenAsync();
                        if (refreshed)
                            return await SecureStorage.GetAsync("access_token");
                    }
                    else
                    {
                        // Токен уже истек
                        var refreshed = await RefreshTokenAsync();
                        if (refreshed)
                            return await SecureStorage.GetAsync("access_token");
                    }
                }
            }
            catch
            {
                // Не удалось прочитать токен
            }

            return null;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            // Проверяем, не пытаемся ли мы уже обновить токен
            if (_isRefreshing)
            {
                // Ждем, пока другой поток завершит обновление
                await _refreshLock.WaitAsync();
                try
                {
                    // После ожидания проверяем результат
                    var token = await SecureStorage.GetAsync("access_token");
                    return !string.IsNullOrEmpty(token);
                }
                finally
                {
                    _refreshLock.Release();
                }
            }

            // Защита от слишком частых запросов
            if (_lastRefreshAttempt.HasValue &&
                (DateTime.UtcNow - _lastRefreshAttempt.Value).TotalSeconds < 2)
            {
                return false;
            }

            _isRefreshing = true;
            _lastRefreshAttempt = DateTime.UtcNow;

            await _refreshLock.WaitAsync();
            try
            {
                var accessToken = await SecureStorage.GetAsync("access_token");
                var refreshToken = await SecureStorage.GetAsync("refresh_token");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    await _loginService.LogoutWithRedirect();
                    return false;
                }

                Console.WriteLine("Starting token refresh...");

                var refreshRequest = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                var json = JsonSerializer.Serialize(refreshRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/auth/refresh-token", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var tokenData = JsonSerializer.Deserialize<TokenData>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (tokenData != null)
                    {
                        Console.WriteLine("Token refresh successful");

                        // Сохраняем новые токены
                        await SecureStorage.SetAsync("access_token", tokenData.AccessToken);
                        await SecureStorage.SetAsync("refresh_token", tokenData.RefreshToken);

                        // Обновляем в LoginService
                        var auth = _loginService.GetCurrentAuth();
                        if (auth != null)
                        {
                            auth.AccessToken = tokenData.AccessToken;
                            auth.RefreshToken = tokenData.RefreshToken;
                            _loginService.UpdateAuth(auth);
                        }

                        OnTokensRefreshed?.Invoke();
                        return true;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Refresh token is invalid");
                    await ClearTokensAsync();
                    OnTokensInvalid?.Invoke();
                }
                else
                {
                    Console.WriteLine($"Token refresh failed: {response.StatusCode}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token refresh error: {ex.Message}");
                return false;
            }
            finally
            {
                _isRefreshing = false;
                _refreshLock.Release();
            }
        }

        public async Task<bool> IsTokenValidAsync()
        {
            var token = await GetValidAccessTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public async Task ClearTokensAsync()
        {
            await _loginService.Logout();
        }
    }
}
