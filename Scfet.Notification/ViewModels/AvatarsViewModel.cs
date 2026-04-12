using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Models;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class AvatarsViewModel: ObservableObject
    {
        private readonly IProfileApiService _profileApiService;
        private readonly IPickImageService _pickImageService;
        private readonly LoginService _loginService;

        public AvatarsViewModel(IProfileApiService profileApiService,
            LoginService loginService, 
            IPickImageService pickImageService)
        {
            _profileApiService = profileApiService;
            _loginService = loginService;
            _pickImageService = pickImageService;
        }

        [ObservableProperty]
        private string avatarUrl = string.Empty;

        // Presets
        [ObservableProperty]
        private ObservableCollection<AvatarPreset> presets = new();

        [ObservableProperty]
        private AvatarPreset? selectedPreset;

        //Images
        [ObservableProperty]
        private FileResult _selectedImage;

        [ObservableProperty]
        private ImageSource _imagePreview;

        // UI
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isLoadAvatarsFailed;

        [ObservableProperty]
        private bool isAvatarUploading;

        [ObservableProperty]
        private bool isAllowCustomAvatar;

        public async Task InitializeAsync()
        {
            InitializeFields();
            await StartAsync();
        }
        public async Task StartAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                await LoadPresets();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadPresets()
        {
            try
            {
                var response = await _profileApiService.GetAllAvatarsAsync();

                if (response == null || response?.Data == null || !response.Success)
                {
                    IsLoadAvatarsFailed = true;
                    return;
                }
                IsLoadAvatarsFailed = false;

                Presets.Clear();
                foreach(var preset in response.Data)
                {
                    Presets.Add(preset);
                }

                if (Presets.Any() && SelectedPreset == null)
                {
                    SelectedPreset = Presets.First();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        private void InitializeFields()
        {
            var auth = _loginService.GetCurrentAuth();
            IsAllowCustomAvatar = auth.Role == "Teacher" || auth.Role == "Administrator";
        }

        private async Task ProcessSelectedImage(FileResult result)
        {
            if (result == null) return;

            // Проверяем размер файла (макс 15MB)
            var fileInfo = new FileInfo(result.FullPath);
            if (fileInfo.Exists && fileInfo.Length > 15 * 1024 * 1024)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Размер изображения не должен превышать 15MB", "OK");
                return;
            }

            SelectedImage = result;
            var stream = await result.OpenReadAsync();
            ImagePreview = ImageSource.FromStream(() => stream);
        }

        [RelayCommand]
        private async Task SelectImageAsync()
        {
            var fileResult = await _pickImageService.SelectImageAsync();
            if(fileResult != null)
            {
                await ProcessSelectedImage(fileResult);
            }
        }

        [RelayCommand]
        private void ClearImage()
        {
            SelectedImage = null;
            ImagePreview = null;
        }

        [RelayCommand]
        public async Task UploadAvatar()
        {
            IsAvatarUploading = true;
            try
            {
                // Если есть кастомное изображение и пользователь может его загружать
                if (IsAllowCustomAvatar && SelectedImage != null)
                {
                    var response = await _profileApiService.UploadCustomAvatarAsync(SelectedImage);

                    if (response == null || response.Success == false)
                    {
                        await Shell.Current.DisplayAlert("Ошибка",
                            $"Ошибка загрузки аватарки: {response?.Message ?? "проверьте интернет соединение"}",
                            "ОК");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Успех", "Аватарка успешно обновлена", "ОК");
                        await Shell.Current.GoToAsync("..");
                    }
                }

                // Иначе используем выбранный пресет
                else if (SelectedPreset != null)
                {
                    var response = await _profileApiService.UploadAvatarAsync(SelectedPreset.Key);

                    if (response == null || response.Success == false)
                    {
                        await Shell.Current.DisplayAlert("Ошибка",
                            $"Ошибка выбора аватарки: {response?.Message ?? "проверьте интернет соединение"}",
                            "ОК");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Успех", "Аватарка успешно обновлена", "ОК");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Выберите аватарку или загрузите свою", "ОК");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsAvatarUploading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                IsLoadAvatarsFailed = false;
                await StartAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }
    }
}
