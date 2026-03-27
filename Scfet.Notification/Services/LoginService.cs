using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services
{
    public class LoginService
    {
        private Auth? _currentAuth;

        public void Login(Auth auth)
        {
            _currentAuth = auth;

            // Сохраняем пользовательские данные
            Preferences.Set("user_id", auth.UserId);
            Preferences.Set("user_email", auth.Email);
            Preferences.Set("user_role", auth.Role);
            Preferences.Set("user_name", auth.FullName);
        }

        public void UpdateAuth(Auth auth)
        {
            _currentAuth = auth;
        }

        public async Task Logout()
        {
            _currentAuth = null;

            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");

            // Очищаем Preferences
            Preferences.Remove("user_id");
            Preferences.Remove("user_email");
            Preferences.Remove("user_name");
            Preferences.Remove("user_role");
        }

        public async Task LogoutWithRedirect()
        {
            _currentAuth = null;

            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");

            // Очищаем Preferences
            Preferences.Remove("user_id");
            Preferences.Remove("user_email");
            Preferences.Remove("user_name");
            Preferences.Remove("user_role");

            await Shell.Current.GoToAsync("//LoginPage");
        }

        public Auth? GetCurrentAuth()
        {
            return _currentAuth;
        }

        public async Task<bool> IsLoggedIn()
        {
            string? token = await SecureStorage.GetAsync("access_token");
            return _currentAuth != null &&
                   !string.IsNullOrEmpty(token);
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                var accessToken = await SecureStorage.GetAsync("access_token");
                var refreshToken = await SecureStorage.GetAsync("refresh_token");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                // Восстанавливаем пользовательские данные из Preferences
                var userId = Preferences.Get("user_id", string.Empty);
                var email = Preferences.Get("user_email", string.Empty);
                var role = Preferences.Get("user_role", string.Empty);
                var fullName = Preferences.Get("user_name", string.Empty);

                if (!string.IsNullOrEmpty(userId))
                {
                    _currentAuth = new Auth
                    {
                        Token = accessToken,
                        UserId = userId,
                        Email = email,
                        FullName = fullName,
                        Role = role
                    };

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto login error: {ex.Message}");
                return false;
            }
        }
    }
}
