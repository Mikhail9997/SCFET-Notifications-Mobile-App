using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Scfet.Notification.Models;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;
using Scfet.Notification.Utils;

namespace Scfet.Notification.ViewModels
{
    public partial class NotificationsViewModel : BaseViewModel
    {
        private readonly INotificationsApiService _notificationsApiService;
        private readonly IFavoritesApiService _favoritesApiService;
        private readonly SignalRService _notificationService;
        private readonly FileService _fileService;
        private readonly LoginService _loginService;

        public NotificationsViewModel(INotificationsApiService notificationsApiService,
            SignalRService notificationService, FileService fileService,
            LoginService loginService, IFavoritesApiService favoritesApiService)
        {
            _notificationsApiService = notificationsApiService;
            _notificationService = notificationService;
            _fileService = fileService;
            _loginService = loginService;
            _favoritesApiService = favoritesApiService;

            _notificationService.OnNotificationReceived += OnNotificationReceived;
            _notificationService.OnNotificationRemove += OnNotificationRemove;
            _notificationService.OnNotificationRead += OnNotificationRead;
            _notificationService.OnNotificationUpdate += OnNotificationUpdate;

            Title = "Уведомления";
            _ = InitializeFields();
        }

        [ObservableProperty]
        public Guid currentUserId; 

        [ObservableProperty]
        public ObservableCollection<Models.Notification> notifications = [];

        [ObservableProperty]
        public PagedResult<Models.Notification> pageResult = new();

        [ObservableProperty]
        public Filter filter = new();

        [ObservableProperty]
        public List<int> pageSizes = new List<int> { 5, 10, 20 };

        [ObservableProperty]
        public List<PickerItem<SortOrder>> sortOrderItems = new();

        [ObservableProperty]
        public List<PickerItem<SortBy>> sortByItems = new();

        [ObservableProperty]
        public List<PickerItem<string>> dateRangeOptions = new();

        [ObservableProperty]
        private PickerItem<string> selectedDateRange;

        [ObservableProperty]
        public PickerItem<SortOrder>? selectedSortOrder;

        [ObservableProperty]
        public PickerItem<SortBy>? selectedSortBy;

        [ObservableProperty]
        private bool showCustomDateInput;

        [ObservableProperty]
        public DateTime? selectedStartDate;

        [ObservableProperty]
        public DateTime? selectedEndDate;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isPagination;

        [ObservableProperty]
        private bool isPaginationEnable;

        [ObservableProperty]
        private bool isLoadNotificationsFailed;

        [ObservableProperty]
        private bool isStartLoadNotificationsFailed;

        public bool IsShowScrollButtons => IsBusy != true && IsStartLoadNotificationsFailed != true && Notifications.Any();


        public async Task InitializeAsync()
        {
            await StartAsync();          
        }

        [RelayCommand]
        private async Task StartAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            await _notificationService.ConnectAsync();

            OnPropertyChanged(nameof(IsShowScrollButtons));

            ResetPagination();
            try
            {
                await LoadNotificationsAsync();

                Notifications.Clear();
                if (IsLoadNotificationsFailed)
                {
                    IsStartLoadNotificationsFailed = true;
                    return;
                }

                IsStartLoadNotificationsFailed = false;

                if (PageResult?.Items != null)
                {
                    foreach (var notification in PageResult.Items)
                    {
                        Notifications.Add(notification);
                    }
                }

                OnPropertyChanged(nameof(Notifications));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsShowScrollButtons));
            }
        }

        [RelayCommand]
        private async Task LoadNotificationsAsync()
        {
            try
            {
                var pageResult = await _notificationsApiService.GetNotificationsAsync((Filter)Filter);

                if (pageResult == null)
                {
                    IsLoadNotificationsFailed = true;
                    return;
                }

                IsLoadNotificationsFailed = false;
                PageResult = pageResult;

                if (!pageResult.Items.Any()) return;

                ValidatePagination();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task LoadNotificationsPaginationAsync()
        {
            if (IsPagination) return;

            IsPagination = true;

            try
            {
                var nextPage = (Notifications.Count / Filter.PageSize) + 1;
                Filter.Page = nextPage;

                await LoadNotificationsAsync();

                if (PageResult == null || IsLoadNotificationsFailed)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "не удалось загрузить уведомления.\nПроверьте подключение к интернету", "ОК");
                    return;
                }

                if (PageResult?.Items != null && PageResult.Items.Any())
                {
                    var existingIds = Notifications.Select(n => n.Id).ToHashSet();
                    foreach (var notification in PageResult.Items)
                    {
                        // Проверяем, нет ли уже такого уведомления
                        if (!existingIds.Contains(notification.Id))
                        {
                            Notifications.Add(notification);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
                // Откатываем страницу при ошибке
                Filter.Page -= 1;
            }
            finally
            {
                IsPagination = false;
            }
        }

        private bool ValidatePagination()
        {
            int page = PageResult.Page;
            int totalPages = PageResult.TotalPages;

            if (page >= totalPages)
            {
                IsPaginationEnable = false;
                return false;
            }
            IsPaginationEnable = true;
            return true;
        }

        private void ResetPagination()
        {
            if(Filter != null)
            {
                Filter.Page = 1;
            }
        }

        [RelayCommand]
        public async Task ApplyFiltersAsync()
        {
            Filter.Page = 1;
            Filter.SortBy = SelectedSortBy?.Value ?? SortBy.CreatedAt;
            Filter.SortOrder = SelectedSortOrder?.Value ?? SortOrder.Descending;
            Filter.StartDate = SelectedStartDate;
            Filter.EndDate = SelectedEndDate;
            IsPaginationEnable = false;
            await StartAsync();
        }

        [RelayCommand]
        public async Task ResetFiltersAsync()
        {
            Filter = new();
            IsPaginationEnable = false;
            SelectedSortBy = SortByItems[0];
            SelectedSortOrder = SortOrderItems[1];
            SelectedDateRange = DateRangeOptions[0];
            Filter.StartDate = null;
            Filter.EndDate = null;
            await StartAsync();
        }

        private void ApplyDateRange(string rangeType)
        {
            DateFilterResult result = DateUtils.ApplyDateRange(rangeType);

            SelectedStartDate = result.SelectedStartDate;
            SelectedEndDate = result.SelectedEndDate;
        }

        private void ApplyCustomDateRange()
        {
            SelectedStartDate = DateTime.Now;
            SelectedEndDate = DateTime.Now;
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync(Models.Notification notification)
        {
            try
            {
                var notificationId = notification.Id;
                var originalState = notification.IsFavorite;

                ApiResponse? response;
                if (!originalState)
                {
                    AddFavorite request = new AddFavorite()
                    {
                        NotificationId = notificationId
                    };
                    response = await _favoritesApiService.AddFavoriteAsync(request);
                }
                else
                {
                    response = await _favoritesApiService.RemoveFavoriteAsync(notificationId);
                }
                if (response == null || !response.Success)
                {
                    await Shell.Current
                        .DisplayAlert("Ошибка", $"Не удалось {(!originalState == true ? "добавить в" : "удалить из")} избранное: {response?.Message ?? "Проверьте интернет соединение"}", "OK");
                    return;
                }
                notification.IsFavorite = !originalState;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось {(!notification?.IsFavorite == true ? "добавить в" : "удалить из")} избранное: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task MarkAsReadAsync(Models.Notification notification)
        {
            if (notification.IsRead) return;

            try
            {
                var success = await _notificationsApiService.MarkAsReadAsync(notification.Id);
                if (success)
                {
                    await _notificationService.MarkAsReadAsync(notification.Id);
                    notification.IsRead = true;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
            OnPropertyChanged(nameof(Notifications));
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                IsStartLoadNotificationsFailed = false;
                await StartAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task GoToRepliesPageAsync(Guid notificationId)
        {
            await Shell.Current.GoToAsync($"RepliesPage?id={notificationId}");
        }

        private void OnNotificationReceived(Models.Notification notification)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!Notifications.Any(n => n.Id == notification.Id))
                    Notifications.Insert(0, notification);
                OnPropertyChanged(nameof(Notifications));

                // Показать локальное уведомление
                if (DeviceInfo.Platform == DevicePlatform.Android ||
                DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    string localImagePath = null;

                    // Если у уведомления есть URL изображения, загружаем его
                    if (!string.IsNullOrEmpty(notification.ImageUrl))
                    {
                        localImagePath = await _fileService.DownloadImageToLocalFile(notification.ImageUrl);
                    }

                    var request = new NotificationRequest
                    {
                        Title = "Новое уведомление",
                        Description = $"От {notification.SenderName}: {notification.Message}",
                        Schedule = new NotificationRequestSchedule
                        {
                            NotifyTime = DateTime.Now
                        }
                    };

                    if (!string.IsNullOrEmpty(localImagePath) && File.Exists(localImagePath))
                    {
                        request.Image = new NotificationImage
                        {
                            FilePath = localImagePath
                        };
                    }
                    await LocalNotificationCenter.Current.Show(request);
                }
            });
        }

        private void OnNotificationRemove(Guid notificationId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null) Notifications.Remove(notification);
            });
        }

        private void OnNotificationRead(Guid notificationId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    OnPropertyChanged(nameof(Notifications));
                }
            });
        }

        private void OnNotificationUpdate(Models.Notification notificationUpdated)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notification = Notifications.FirstOrDefault(n => n.Id == notificationUpdated.Id);
                if (notification != null)
                {
                    var index = Notifications.IndexOf(notification);

                    Notifications.RemoveAt(index);
                    Notifications.Insert(index, notificationUpdated);

                    OnPropertyChanged(nameof(Notifications));
                }
            });
        }

        partial void OnSelectedDateRangeChanged(PickerItem<string> value)
        {
            if (value == null) return;

            ShowCustomDateInput = value.Value == "custom";

            if (!ShowCustomDateInput)
            {
                ApplyDateRange(value.Value);
                return;
            }
            ApplyCustomDateRange();
        }

        private async Task InitializeFields()
        {
            SortOrderItems = new()
            {
                new()
                {
                    Value = SortOrder.Ascending,
                    DisplayName = "Сначала старые"
                },
                new()
                {
                    Value = SortOrder.Descending,
                    DisplayName = "Сначала новые"
                }
            };

            SortByItems = new()
            {
                new()
                {
                    Value = SortBy.CreatedAt,
                    DisplayName = "Дата публикации"
                },
                new()
                {
                    Value = SortBy.Title,
                    DisplayName = "Заголовку"
                }
            };
            DateRangeOptions = new List<PickerItem<string>>
            {
                new() { DisplayName = "За все время", Value = "all" },
                new() { DisplayName = "Сегодня", Value = "today" },
                new() { DisplayName = "Вчера", Value = "yesterday" },
                new() { DisplayName = "Эта неделя", Value = "this_week" },
                new() { DisplayName = "Прошлая неделя", Value = "last_week" },
                new() { DisplayName = "Этот месяц", Value = "this_month" },
                new() { DisplayName = "Прошлый месяц", Value = "last_month" },
                new() { DisplayName = "Этот год", Value = "this_year"},
                new() { DisplayName = "Прошлый год", Value = "last_year"},
                new() { DisplayName = "Произвольный период", Value = "custom" }
            };
            SelectedSortBy = SortByItems[0];
            SelectedSortOrder = SortOrderItems[1];
            SelectedDateRange = DateRangeOptions[0];

            var auth = _loginService.GetCurrentAuth();
            var result = Guid.TryParse(auth?.UserId, out Guid currentUserId);

            CurrentUserId = result ? currentUserId : Guid.Empty;
        }
    }

    public class PickerItem<T>
    {
        public T Value { get; set; }
        public string DisplayName { get; set; }
    }
}

