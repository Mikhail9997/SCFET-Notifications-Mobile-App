using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Handlers
{
    public class AuthHandler: DelegatingHandler
    {
        public AuthHandler()
        {
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Очищаем данные авторизации
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

            return response;
        }
    }
}
