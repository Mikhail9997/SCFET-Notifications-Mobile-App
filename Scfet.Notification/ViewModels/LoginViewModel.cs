using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class LoginViewModel:BaseViewModel
    {
        private readonly IAuthApiService _authApiService;
        private readonly SignalRService _notificationService;
        private readonly LoginService _loginService;

        public LoginViewModel(IAuthApiService authApiService, SignalRService notificationService, LoginService loginService)
        {
            _authApiService = authApiService;
            _loginService = loginService;
            _notificationService = notificationService;
        }

        [ObservableProperty]
        private string email = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        public bool isAuth;

        public async Task InitializeAsync()
        {
            IsAuth = await _loginService.IsLoggedIn();
            Title = IsAuth ? "Выйти" : "Войти";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Заполните все поля", "OK");
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authApiService.LoginAsync(Email, Password);
                if (result != null && result.Success && result.Data != null)
                {
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"{result?.Message ?? "Произошла неизвестная ошибка"}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка подключения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            if (IsBusy) return;
            try
            {
                var confirm = await Shell.Current.DisplayAlert("Выход", "Вы уверены, что хотите выйти?", "Да", "Нет");
                if (confirm)
                {
                    IsBusy = true;
                    await _notificationService.DisconnectAsync();
                    await _authApiService.Logout();

                    IsAuth = await _loginService.IsLoggedIn();
                    Title = IsAuth ? "Выйти" : "Войти";

                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка подключения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
