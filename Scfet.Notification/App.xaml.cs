
using Microsoft.Maui.Controls;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;
using Scfet.Notification.ViewModels;
using Scfet.Notification.Views;

namespace Scfet.Notification
{
    public partial class App : Application
    {
        private readonly IProfileApiService _apiService;
        private readonly LoginService _loginService;
        private readonly NotificationPermissionsService _permissionsService;
        private readonly AppShellViewModel _appShellViewModel;
        public App(IProfileApiService apiService, 
            LoginService loginService,
            NotificationPermissionsService permissionsService, 
            AppShellViewModel appShellViewModel)
        {
            InitializeComponent();
            _apiService = apiService;
            _loginService = loginService;
            _permissionsService = permissionsService;
            _appShellViewModel = appShellViewModel;
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
            var isAutoLoggedIn = await _loginService.TryAutoLoginAsync();
            if (isAutoLoggedIn)
            {
                var result = await _apiService.GetCurrentUserAsync();
                if (result.Code == 404)
                {
                    await _loginService.LogoutWithRedirect();
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
            return new Window(new AppShell(_appShellViewModel));
        }
    }
}