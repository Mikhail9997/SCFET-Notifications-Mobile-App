
using Microsoft.Maui.Controls;
using Scfet.Notification.Services;
using Scfet.Notification.ViewModels;
using Scfet.Notification.Views;

namespace Scfet.Notification
{
    public partial class App : Application
    {
        private readonly IApiService _apiService;
        private readonly LoginService _loginService;
        private readonly NotificationPermissionsService _permissionsService;
        public App(IApiService apiService, LoginService loginService, NotificationPermissionsService permissionsService)
        {
            InitializeComponent();
            _apiService = apiService;
            _loginService = loginService;
            _permissionsService = permissionsService;
        }

        protected override async void OnStart()
        {
            // Проверка авторизации при запуске
            CheckAuthStatus();
            // Проверка разрешения на уведомления
            CheckNotificationPermissionOnStart();
        }

        private async void CheckAuthStatus()
        {
            var token = Preferences.Get("auth_token", string.Empty);

            if (!string.IsNullOrEmpty(token))
            {
                var result = await _apiService.GetCurrentUserAsync();
                if (result.Code == 404)
                {
                    await _loginService.Logout();
                    return;
                }
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        private void CheckNotificationPermissionOnStart()
        {

            var hasPermission = _permissionsService.CheckAndRequestNotificationPermission();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}