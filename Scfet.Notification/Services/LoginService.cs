using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services
{
    public class LoginService
    {
        private Auth? _currentAuth;
        private readonly string _userDataFileName = "user_data.json";
        private string _userDataPath;

        public LoginService()
        {
            _userDataPath = Path.Combine(FileSystem.AppDataDirectory, _userDataFileName);
        }

        public async Task Login(Auth auth)
        {
            _currentAuth = auth;

            SaveUserDataToJson(auth);

            // Сохраняем пользовательские данные
            await SecureStorage.SetAsync("access_token", auth.AccessToken);
            await SecureStorage.SetAsync("refresh_token", auth.RefreshToken);

            Preferences.Set("user_id", auth.UserId);
            Preferences.Set("user_email", auth.Email);
            Preferences.Set("user_role", auth.Role);
            Preferences.Set("user_name", auth.FullName);
        }

        private void SaveUserDataToJson(Auth auth)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                string json = JsonSerializer.Serialize(auth, options);
                File.WriteAllText(_userDataPath, json);

                Console.WriteLine($"User data saved to: {_userDataPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user data: {ex.Message}");
            }
        }

        private Auth? LoadUserDataFromJson()
        {
            try
            {
                if (File.Exists(_userDataPath))
                {
                    string json = File.ReadAllText(_userDataPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<Auth>(json, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user data: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                // Пробуем загрузить из JSON файла
                var authFromJson = LoadUserDataFromJson();

                if (authFromJson != null && !string.IsNullOrEmpty(authFromJson.AccessToken))
                {
                    _currentAuth = authFromJson;

                    // Восстанавливаем токены в SecureStorage
                    await SecureStorage.SetAsync("access_token", authFromJson.AccessToken);
                    if (!string.IsNullOrEmpty(authFromJson.RefreshToken))
                    {
                        await SecureStorage.SetAsync("refresh_token", authFromJson.RefreshToken);
                    }

                    return true;
                }

                // загрузка из SecureStorage
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
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        UserId = userId,
                        Email = email,
                        FullName = fullName,
                        Role = role
                    };

                    SaveUserDataToJson(_currentAuth);

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
        public Auth? GetCurrentAuth()
        {
            if(_currentAuth != null)
            {
                return _currentAuth;
            }
            Auth? authJson = LoadUserDataFromJson();
            return authJson;
        }

        public async Task<bool> IsLoggedIn()
        {
            string? token = await SecureStorage.GetAsync("access_token");
            return _currentAuth != null &&
                   !string.IsNullOrEmpty(token);
        }

        public void UpdateAuth(Auth auth)
        {
            _currentAuth = auth;
            SaveUserDataToJson(auth);
        }

        private void Clear()
        {

            if (File.Exists(_userDataPath))
            {
                File.Delete(_userDataPath);
            }

            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");

            // Очищаем Preferences
            Preferences.Remove("user_id");
            Preferences.Remove("user_email");
            Preferences.Remove("user_name");
            Preferences.Remove("user_role");
        }

        public async Task Logout()
        {
            _currentAuth = null;
            Clear();
        }

        public async Task LogoutWithRedirect()
        {
            await Logout();

            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
