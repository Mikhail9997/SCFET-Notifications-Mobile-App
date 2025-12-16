using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Services;

namespace Scfet.Notification.ViewModels
{
    public partial class LoginViewModel:BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly NotificationService _notificationService;

        public LoginViewModel(IApiService apiService, NotificationService notificationService)
        {
            _apiService = apiService;
            Title = isAuth ? "Выйти" :"Войти";
            _notificationService = notificationService;
        }

        [ObservableProperty]
        private string email = "student@scfet.ru";

        [ObservableProperty]
        private string password = "student123";

        public bool isAuth => Preferences.ContainsKey("auth_token");

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
                var result = await _apiService.LoginAsync(Email, Password);
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
            var confirm = await Shell.Current.DisplayAlert("Выход", "Вы уверены, что хотите выйти?", "Да", "Нет");
            if (confirm)
            {
                await _notificationService.DisconnectAsync();
                await _apiService.Logout();

                OnPropertyChanged(nameof(isAuth));
                Title = "Войти";

                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}
