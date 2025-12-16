using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Models;
using Scfet.Notification.Services;

namespace Scfet.Notification.ViewModels
{
    public partial class ProfileViewModel(IApiService apiService, NotificationService notificationService, FileService fileService) : BaseViewModel
    {
        private readonly IApiService _apiService = apiService;
        private readonly NotificationService _notificationService = notificationService;
        private readonly FileService _fileService = fileService;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private string firstName;

        [ObservableProperty]
        private string lastName;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private bool isCacheEnable;

        [ObservableProperty]
        private string cacheSize;

        public string UserRoleDisplay => CurrentUser?.Role switch
        {
            "Student" => "Студент",
            "Teacher" => "Преподаватель",
            "Administrator" => "Администратор",
            _ => "Пользователь"
        };

        public bool CanSendNotifications => CurrentUser?.Role == "Teacher" ||
            CurrentUser?.Role == "Administrator";

        public bool IsCurrentUserEnable => CurrentUser != null;

        public async Task InitializeAsync()
        {
            await LoadProfileAsync();
            UpdateStatusCache();
        }

        [RelayCommand]
        private async Task LoadProfileAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                var profile = await _apiService.GetCurrentUserAsync();
                if (profile.User == null)
                {
                    OnPropertyChanged(nameof(IsCurrentUserEnable));
                    return;
                }
                CurrentUser = profile.User;
                FirstName = CurrentUser.FirstName;
                LastName = CurrentUser.LastName;
                Email = CurrentUser.Email;

                OnPropertyChanged(nameof(UserRoleDisplay));
                OnPropertyChanged(nameof(CanSendNotifications));
                OnPropertyChanged(nameof(IsCurrentUserEnable));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки профиля: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task UpdateProfileAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Заполните все поля", "OK");
                return;
            }

            IsBusy = true;

            try
            {
                var success = await _apiService.UpdateProfileAsync(FirstName, LastName, Email);
                if (success)
                {
                    await Shell.Current.DisplayAlert("Успех", "Профиль обновлен", "OK");

                    IsBusy = false;

                    await LoadProfileAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Ошибка обновления профиля", "OK");
                }
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            var currentPassword = await Shell.Current.DisplayPromptAsync("Смена пароля", "Введите текущий пароль:", "OK", "Отмена", "Пароль", -1, Keyboard.Default, "");
            if (string.IsNullOrEmpty(currentPassword)) return;

            var newPassword = await Shell.Current.DisplayPromptAsync("Смена пароля", "Введите новый пароль:", "OK", "Отмена", "Пароль", -1, Keyboard.Default, "");
            if (string.IsNullOrEmpty(newPassword)) return;

            var confirmPassword = await Shell.Current.DisplayPromptAsync("Смена пароля", "Подтвердите новый пароль:", "OK", "Отмена", "Пароль", -1, Keyboard.Default, "");
            if (string.IsNullOrEmpty(confirmPassword)) return;

            if (newPassword != confirmPassword)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                return;
            }

            try
            {
                var success = await _apiService.ChangePasswordAsync(currentPassword, newPassword);
                if (success)
                {
                    await Shell.Current.DisplayAlert("Успех", "Пароль изменен", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Неверный текущий пароль", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
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
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        private void UpdateStatusCache()
        {
            var size = _fileService.GetCacheSizeInBytes();
            if(size > 0)
            {
                IsCacheEnable = true;
                CacheSize = _fileService.FormatFileSize(size);
                return;
            }
            IsCacheEnable = false;
            CacheSize = string.Empty;
        }

        [RelayCommand]
        private async Task CleanCacheAsync()
        {
            bool confirm = await Shell.Current.DisplayAlert(
            "Очистка кэша",
            "Вы уверены, что хотите удалить все временные файлы?",
            "Да", "Нет");

            if (!confirm) return;

            try
            {
                if(await _fileService.DeleteCacheAsync())
                {
                    await Shell.Current.DisplayAlert("Готово", "кеш успешно очищен", "ОК");
                    UpdateStatusCache();
                    return;
                }
                await Shell.Current.DisplayAlert("Ошибка", "не удалось очистить кеш", "ОК");     
            }
            catch
            {
                await Shell.Current.DisplayAlert("Ошибка", "не удалось очистить кеш", "ОК");
            }
        }

        [RelayCommand]
        private async Task GoToAsync(string path)
        {
            await Shell.Current.GoToAsync(path);
        }
    }
}
