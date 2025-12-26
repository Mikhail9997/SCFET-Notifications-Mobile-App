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

namespace Scfet.Notification.ViewModels
{
    public partial class CreateNotificationViewModel:BaseViewModel
    {
        private readonly IApiService _apiService;
        
        public CreateNotificationViewModel(IApiService apiService)
        {
            _apiService = apiService;

            _ = InitializeFields();
        }

        [ObservableProperty]
        private User currentUser;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;

        [ObservableProperty]
        private Group selectedGroup;

        [ObservableProperty]
        private User selectedUser;

        [ObservableProperty]
        private List<Group> _groups = new();

        [ObservableProperty]
        private List<User> _students = new();

        [ObservableProperty]
        private List<User> _teachers = new();

        [ObservableProperty] 
        private List<User> _administrators = new ();

        [ObservableProperty]
        private List<User> _users = new();

        [ObservableProperty]
        private FileResult _selectedImage;

        [ObservableProperty]
        private ImageSource _imagePreview;

        [ObservableProperty]
        private ObservableCollection<PickerItem> notificationTypes;

        [ObservableProperty]
        private ObservableCollection<PickerItem> audienceTypes;

        [ObservableProperty]
        private PickerItem selectedType;

        [ObservableProperty]
        private PickerItem selectedAudience;

        [ObservableProperty]
        private UserFilter userFilter = new();

        [ObservableProperty]
        private GroupFilter groupFilter = new();

        [ObservableProperty]
        private bool isUserFiltersVisible = false;

        [ObservableProperty]
        private bool isGroupFiltersVisible = false;

        public bool ShowGroupSelector => SelectedAudience?.Key == "group";

        public bool ShowUserSelector => SelectedAudience?.Key == "specific";

        private bool IsAdministrator => CurrentUser?.Role == "Administrator";

        partial void OnSelectedAudienceChanged(PickerItem value)
        {
            OnPropertyChanged(nameof(ShowGroupSelector));
            OnPropertyChanged(nameof(ShowUserSelector));

            IsUserFiltersVisible = ShowUserSelector;
            IsGroupFiltersVisible = ShowGroupSelector;
        }

        partial void OnSelectedGroupChanged(Group value)
        {
            if (value != null && value.StudentCount == 0)
            {
                Shell.Current.DisplayAlert("Ошибка", "Нет студентов для выбранной группы", "Ок");
                SelectedGroup = null;
            }
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                var groupsTask = _apiService.GetGroupsAsync(GroupFilter);
                var studentsTask = _apiService.GetStudentsAsync(UserFilter);
                var teachersTask = _apiService.GetTeachersAsync(UserFilter);

                await Task.WhenAll(groupsTask, studentsTask, teachersTask);

                Groups = await groupsTask;
                Students = await studentsTask;
                Teachers = await teachersTask;

                if (IsAdministrator)
                {
                    Administrators = await _apiService.GetAdministratorsAsync(UserFilter);
                }

                UpdateUsersCollection();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки данных: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SendNotificationAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Заполните заголовок и сообщение", "OK");
                return;
            }

            IsBusy = true;

            try
            {
                var notification = new CreateNotification
                {
                    Title = Title,
                    Message = Message,
                    Type = Enum.TryParse<NotificationType>(SelectedType.Key, out var type)
                        ? type
                        : NotificationType.Info,
                    Image = SelectedImage ?? null
                };

                // Определяем получателей на основе выбранной аудитории
                switch (SelectedAudience.Key)
                {
                    case "group" when SelectedGroup != null:
                        notification.TargetGroupId = SelectedGroup.Id;
                        break;
                    case "specific" when SelectedUser != null:
                        notification.TargetUserIds = new List<Guid> { SelectedUser.UserId };
                        break;
                    case "students":
                        // Отправка всем студентам
                        notification.TargetUserIds = Students.Select(t => t.UserId).ToList();
                        break;
                    case "teachers":
                        // Отправка всем преподавателям
                        notification.TargetUserIds = Teachers.Select(t => t.UserId).ToList();
                        break;
                    case "administrators":
                        //Отправка всем администраторам
                        notification.TargetUserIds = Administrators.Select(a => a.UserId).ToList();
                        break;
                    case "all":
                        // Отправка всем - бэкенд сам определит получателей
                        break;
                    default:
                        await Shell.Current.DisplayAlert("Ошибка", "Выберите получателей", "OK");
                        return;
                }

                var success = await _apiService.SendNotificationAsync(notification);
                if (success)
                {
                    await Shell.Current.DisplayAlert("Успех", "Уведомление отправлено", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Ошибка отправки уведомления", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectImageAsync()
        {
            try
            {
                FileResult result = null;

                // Проверяем, если это Xiaomi устройство
                if (DeviceInfo.Manufacturer?.ToLower().Contains("xiaomi") == true)
                {
                    result = await PickImageForXiaomi();
                }
                else
                {
                    result = await PickImageStandard();
                }

                if (result != null)
                {
                    await ProcessSelectedImage(result);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Информация", "Файл не выбран", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task<FileResult> PickImageForXiaomi()
        {
            try
            {
                // Для Xiaomi пробуем несколько подходов

                // 1. Сначала пробуем MediaPicker с задержкой
                await Task.Delay(100);
                var mediaResult = await MediaPicker.Default.PickPhotoAsync();
                if (mediaResult != null) return mediaResult;

                // 2. Пробуем FilePicker с явным указанием MIME types
                var fileOptions = new PickOptions
                {
                    PickerTitle = "Выберите изображение",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                    { DevicePlatform.Android, new[]
                        {
                            "image/png",
                            "image/jpeg",
                            "image/jpg"
                        }
                    },
                        })
                };

                await Task.Delay(100);
                var fileResult = await FilePicker.Default.PickAsync(fileOptions);
                if (fileResult != null) return fileResult;

                // 3. Пробуем снова с базовыми настройками
                await Task.Delay(100);
                var basicOptions = new PickOptions
                {
                    PickerTitle = "Выберите изображение"
                };
                return await FilePicker.Default.PickAsync(basicOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выбора изображения на Xiaomi: {ex.Message}");
                return null;
            }
        }

        private async Task<FileResult> PickImageStandard()
        {
            // Стандартная логика для других устройств
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Разрешение требуется",
                        "Необходимо разрешение на доступ к хранилищу для выбора изображения", "OK");
                    return null;
                }
            }

            var options = new PickOptions
            {
                PickerTitle = "Выберите изображение",
                FileTypes = FilePickerFileType.Images
            };

            return await FilePicker.Default.PickAsync(options);
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
        private void ClearImage()
        {
            SelectedImage = null;
            ImagePreview = null;
        }

        [RelayCommand]
        private async Task ApplyUserFiltersAsync()
        {
            if (!await ValidateFiltersAsync()) return;

            try
            {
                IsBusy = true;

                var studentsTask = _apiService.GetStudentsAsync(UserFilter);
                var teachersTask = _apiService.GetTeachersAsync(UserFilter);

                await Task.WhenAll(studentsTask, teachersTask);

                Students = await studentsTask;
                Teachers = await teachersTask;

                if (IsAdministrator)
                {
                    Administrators = await _apiService.GetAdministratorsAsync(UserFilter);
                }

                UpdateUsersCollection();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка фильтрации: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ApplyGroupFiltersAsync()
        {
            if (!await ValidateFiltersAsync()) return;

            try
            {
                IsBusy = true;
                Groups = await _apiService.GetGroupsAsync(GroupFilter);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка фильтрации: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ResetUserFiltersAsync()
        {
            UserFilter = new UserFilter();
            await ApplyUserFiltersAsync();
        }

        [RelayCommand]
        private async Task ResetGroupFiltersAsync()
        {
            GroupFilter = new GroupFilter();
            await ApplyGroupFiltersAsync();
        }

        private void UpdateUsersCollection()
        {
            Users = Students
                .Concat(Teachers)
                .Concat(Administrators)
                .Where(u => u.Email != CurrentUser?.Email)
                .DistinctBy(u => u.UserId)
                .ToList();
        }

        private async Task<bool> ValidateFiltersAsync()
        {
            if(UserFilter.FirstName?.Length > 100 || 
                UserFilter.LastName?.Length > 100 || 
                UserFilter.Email?.Length > 100 ||
                GroupFilter.Name?.Length > 100)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Длина не должна превышать 100 символов", "OK");
                return false;
            }
            return true;
        }

        private async Task InitializeFields()
        {
            var profile = await _apiService.GetCurrentUserAsync();
            CurrentUser = profile.User;
            OnPropertyChanged(nameof(IsAdministrator));

            NotificationTypes = new ObservableCollection<PickerItem>
            {
                new PickerItem { Key = "Info", DisplayValue = "Информация" },
                new PickerItem { Key = "Warning", DisplayValue = "Предупреждение" },
                new PickerItem { Key = "Urgent", DisplayValue = "Срочный" },
                new PickerItem { Key = "Event", DisplayValue = "Событие" }
            };

            AudienceTypes = new ObservableCollection<PickerItem>(GetAvailableAudienceTypes());

            SelectedType = NotificationTypes.First();
            SelectedAudience = AudienceTypes.First();

            IsUserFiltersVisible = ShowUserSelector;
            IsGroupFiltersVisible = ShowGroupSelector;
        }

        private IEnumerable<PickerItem> GetAvailableAudienceTypes()
        {
            yield return new PickerItem { Key = "all", DisplayValue = "Все" };
            yield return new PickerItem { Key = "students", DisplayValue = "Студенты" };
            yield return new PickerItem { Key = "teachers", DisplayValue = "Учителя" };

            if (IsAdministrator)
            {
                yield return new PickerItem { Key = "administrators", DisplayValue = "Администраторам" };
            }

            yield return new PickerItem { Key = "group", DisplayValue = "Группа" };
            yield return new PickerItem { Key = "specific", DisplayValue = "Конкретный" };
        }
    }

    public class PickerItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
    }

}
