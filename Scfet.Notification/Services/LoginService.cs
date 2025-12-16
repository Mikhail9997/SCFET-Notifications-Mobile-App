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
        public void Login(Auth authResponse)
        {
            Preferences.Set("auth_token", authResponse.Token);
            Preferences.Set("user_id", authResponse.UserId.ToString());
            Preferences.Set("user_email", authResponse.Email);
            Preferences.Set("user_name", authResponse.FullName);
            Preferences.Set("user_role", authResponse.Role);
        } 

        public async Task Logout()
        {
            Preferences.Remove("auth_token");
            Preferences.Remove("user_id");
            Preferences.Remove("user_email");
            Preferences.Remove("user_name");
            Preferences.Remove("user_role");

            // Перенаправляем на логин
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync("//LoginPage");
            });
        }
    }
}
