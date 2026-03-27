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
using Scfet.Notification.Utils;

namespace Scfet.Notification.ViewModels
{
    public partial class SentNotificationsViewModel:BaseViewModel
    {
        private readonly IApiService _apiService;

        public SentNotificationsViewModel(IApiService apiService)
        {
            _apiService = apiService;

            _ = InitializeFields();
        }

        [ObservableProperty]
        private ObservableCollection<SentNotification> notifications = new();

        [ObservableProperty]
        private SentNotification selectedNotification;

        [ObservableProperty]
        private NotificationFilter filter = new();

        [ObservableProperty]
        public List<int> pageSizes = new List<int> { 5, 10, 20 };

        [ObservableProperty]
        private GetItems<SentNotification> pagedResult = new();

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

                if (PagedResult?.Items != null)
                {
                    foreach (var notification in PagedResult.Items)
                    {
                        Notifications.Add(notification);
                    }
                }
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
                var pageResult = await _apiService.GetSentNotificationsAsync(Filter);

                if (pageResult == null)
                {
                    IsLoadNotificationsFailed = true;
                    return;
                }

                IsLoadNotificationsFailed = false;
                PagedResult = pageResult;

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

                if (PagedResult == null || IsLoadNotificationsFailed)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "не удалось загрузить уведомления.\nПроверьте подключение к интернету", "ОК");
                    return;
                }

                if (PagedResult?.Items != null && PagedResult.Items.Any())
                {
                    foreach (var notification in PagedResult.Items)
                    {
                        // Проверяем, нет ли уже такого уведомления
                        if (!Notifications.Any(n => n.Id == notification.Id))
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
            int page = PagedResult.Page;
            int totalPages = PagedResult.TotalPages;

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
            if (Filter != null)
            {
                Filter.Page = 1;
            }
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
            DateFilterResult result = DateUtils.ApplyDateRange(rangeType);

            SelectedStartDate = result.SelectedStartDate;
            SelectedEndDate = result.SelectedEndDate;
        }

        private void ApplyCustomDateRange()
        {
            SelectedStartDate = DateTime.Now;
            SelectedEndDate = DateTime.Now;
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

        [RelayCommand]
        private async Task EditNotificationAsync(SentNotification notification)
        {
            var id = notification.Id;
            await Shell.Current.GoToAsync($"EditNotificationPage?id={id}");
        }

        [RelayCommand]
        private async Task DeleteNotificationAsync(SentNotification notification)
        {
            var confirm = await Shell.Current.DisplayAlert("Удаление",
                $"Удалить уведомление \"{notification.Title}\"?", "Да", "Нет");

            if (confirm)
            {
                try
                {
                    var success = await _apiService.RemoveNotificationAsync(notification.Id);
                    if (!success)
                    {
                        await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить уведомление", "OK");
                        return;
                    }
                    Notifications.Remove(notification);

                    OnPropertyChanged(nameof(Notifications));

                    await Shell.Current.DisplayAlert("Успех", "Уведомление удалено", "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"Ошибка удаления: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task ShowDetailsAsync(SentNotification notification)
        {
            SelectedNotification = notification;

            await Shell.Current.DisplayAlert(
                $"Уведомление: {notification.Title}",
                $"Сообщение: {notification.Message}\n\n" +
                $"Тип: {notification.Type}\n" +
                $"Отправлено: {notification.FormattedDate}\n" +
                $"Получатели: {notification.ReadReceivers}/{notification.TotalReceivers} ({notification.ReadPercentage:F1}% прочитано)",
                "OK");
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
}

