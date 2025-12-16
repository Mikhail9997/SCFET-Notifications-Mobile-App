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
using Scfet.Notification.Utils;

namespace Scfet.Notification.ViewModels
{
    public partial class NotificationsViewModel: BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly NotificationService _notificationService;
        private readonly FileService _fileService;

        public NotificationsViewModel(IApiService apiService, NotificationService notificationService, FileService fileService)
        {
            _apiService = apiService;
            _notificationService = notificationService;
            _fileService = fileService;

            _notificationService.OnNotificationReceived += OnNotificationReceived;
            _notificationService.OnNotificationRemove += OnNotificationRemove;
            _notificationService.OnNotificationRead += OnNotificationRead;

            Title = "Уведомления";
            _ = InitializeFields();
        }

        [ObservableProperty]
        public ObservableCollection<Models.Notification> notifications = [];

        [ObservableProperty]
        public GetNotification<Models.Notification> pageResult = new();

        [ObservableProperty]
        public NotificationFilter filter = new();

        [ObservableProperty]
        public List<int> pageSizes = new List<int> { 5, 10, 20 };

        [ObservableProperty]
        public List<PickerItem<NotificationSortOrder>> sortOrderItems = new();

        [ObservableProperty]
        public List<PickerItem<NotificationSortBy>> sortByItems = new();

        [ObservableProperty]
        public List<PickerItem<string>> dateRangeOptions = new();

        [ObservableProperty]
        private PickerItem<string> selectedDateRange;

        [ObservableProperty]
        public PickerItem<NotificationSortOrder>? selectedSortOrder;

        [ObservableProperty]
        public PickerItem<NotificationSortBy>? selectedSortBy;

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


        public async Task InitializeAsync()
        {
            await StartAsync();
            await _notificationService.ConnectAsync();
        }

        [RelayCommand]
        private async Task StartAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                await LoadNotificationsAsync();

                Notifications.Clear();
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
            }
        }

        [RelayCommand]
        private async Task LoadNotificationsAsync()
        {
            try
            {
                var pageResult = await _apiService.GetNotificationsAsync(Filter);

                if (pageResult == null) return;

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
                //if (!ValidatePagination()) return;
                var nextPage = (Notifications.Count / Filter.PageSize) + 1;
                Filter.Page = nextPage;
                //Filter.Page += 1;

                await LoadNotificationsAsync();

                if (PageResult == null) return;

                if (PageResult?.Items != null && PageResult.Items.Any())
                {
                    foreach (var notification in PageResult.Items)
                    {
                        // Проверяем, нет ли уже такого уведомления
                        if (!Notifications.Any(n => n.Id == notification.Id))
                        {
                            Notifications.Add(notification);
                        }
                    }
                }
            }
            catch(Exception ex)
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

        public bool ValidatePagination()
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

        [RelayCommand]
        public async Task ApplyFiltersAsync()
        {
            Filter.Page = 1;
            Filter.SortBy = SelectedSortBy?.Value ?? NotificationSortBy.CreatedAt;
            Filter.SortOrder = SelectedSortOrder?.Value ?? NotificationSortOrder.Descending;
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
            var today = DateTime.Today;

            switch (rangeType)
            {
                case "today":
                    SelectedStartDate = today;
                    SelectedEndDate = today;
                    break;
                case "yesterday":
                    var yesterday = today.AddDays(-1);
                    SelectedStartDate = yesterday;
                    SelectedEndDate = yesterday;
                    break;
                case "this_week":
                    var dayOffset = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
                    SelectedStartDate = today.AddDays(-dayOffset);
                    SelectedEndDate = today;
                    break;
                case "last_week":
                    var dayOffset2 = today.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)today.DayOfWeek;
                    var lastWeekStart = today.AddDays(-dayOffset2 - 6);
                    var lastWeekEnd = lastWeekStart.AddDays(6);
                    SelectedStartDate = lastWeekStart;
                    SelectedEndDate = lastWeekEnd;
                    break;
                case "this_month":
                    var monthStart = new DateTime(today.Year, today.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    SelectedStartDate = monthStart;
                    SelectedEndDate = monthEnd;
                    break;
                case "last_month":
                    var lastMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    var lastMonthEndStart = lastMonthStart.AddMonths(1).AddDays(-1);
                    SelectedStartDate = lastMonthStart;
                    SelectedEndDate = lastMonthEndStart;
                    break;
                case "this_year":
                    var yearStart = new DateTime(today.Year, 1, 1);
                    var yearEnd = today;
                    SelectedStartDate = yearStart;
                    SelectedEndDate = yearEnd;
                    break;
                case "last_year":
                    var lastYearStart = new DateTime(today.Year, 1, 1).AddYears(-1);
                    var lastYearEnd = lastYearStart.AddMonths(11);
                    SelectedStartDate = lastYearStart;
                    SelectedEndDate = lastYearEnd;
                    break;
                case "all":
                default:
                    SelectedStartDate = null;
                    SelectedEndDate = null;
                    break;
            }
        }

        private void ApplyCustomDateRange()
        {
            SelectedStartDate = DateTime.Now;
            SelectedEndDate = DateTime.Now;
        }

        [RelayCommand]
        private async Task MarkAsReadAsync(Models.Notification notification)
        {
            if (notification.IsRead) return;

            try
            {
                var success = await _apiService.MarkAsReadAsync(notification.Id);
                if (success)
                {
                    notification.IsRead = true;
                    await _notificationService.MarkAsReadAsync(notification.Id);

                    var index = Notifications.IndexOf(notification);

                    if(index >= 0)
                    {
                        Notifications[index] = notification;
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadNotificationsAsync();
        }

        private void OnNotificationReceived(Models.Notification notification)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                Notifications.Insert(0, notification);
                OnPropertyChanged(nameof(Notifications));

                // Показать локальное уведомление
                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
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
                if(notification != null) Notifications.Remove(notification);
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
                    Value = NotificationSortOrder.Ascending,
                    DisplayName = "Сначала старые"
                },
                new()
                {
                    Value = NotificationSortOrder.Descending,
                    DisplayName = "Сначала новые"
                }
            };

            SortByItems = new()
            {
                new()
                {
                    Value = NotificationSortBy.CreatedAt,
                    DisplayName = "Дата публикации"
                },
                new()
                {
                    Value = NotificationSortBy.Title,
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
        }
    }

    public class PickerItem<T>
    {
        public T Value { get; set; }
        public string DisplayName { get; set; }
    }
}

