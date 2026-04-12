using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services.Api
{
    public interface IAuthApiService
    {
        Task<AuthResponse<User>> LoginAsync(string email, string password);
        Task Logout();
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    }

    public class AuthApiService : BaseApiService, IAuthApiService
    {
        public AuthApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
        }

        public async Task<AuthResponse<User>> LoginAsync(string email, string password)
        {
            var authError = new AuthResponse<User>
            {
                Message = "Произошла неизвестная ошибка",
                Success = false
            };

            try
            {
                var loginData = new { email, password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync("auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(responseContent))
                    return authError;

                var authResponse = DeserializeResponse<AuthResponse<User>>(responseContent);

                if (authResponse?.Data != null)
                {
                    var data = authResponse.Data;
                    var auth = new Auth
                    {
                        AccessToken = data.AccessToken,
                        RefreshToken = data.RefreshToken,
                        UserId = data.UserId.ToString(),
                        Email = data.Email,
                        FullName = data.FullName,
                        Role = data.Role
                    };
                    await LoginService.Login(auth);
                    return authResponse;
                }

                return authResponse ?? authError;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return authError;
            }
        }

        public async Task Logout()
        {
            try
            {
                var userId = Preferences.Get("user_id", string.Empty);
                if (!string.IsNullOrEmpty(userId))
                {
                    await HttpClient.PostAsync("auth/revoke-token", null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
            finally
            {
                await LoginService.Logout();
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                var passwordData = new
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    ConfirmNewPassword = newPassword
                };

                var json = JsonSerializer.Serialize(passwordData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync("auth/change-password", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }
    }
}
