using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.LocalNotification;
using Scfet.Notification.Models;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;
using Scfet.Notification.Utils;

namespace Scfet.Notification.ViewModels
{
    public partial class FavoritesViewModel:ObservableObject
    {
        private readonly IFavoritesApiService _favoritesApiService;
        private readonly INotificationsApiService _notificationsApiService;
        private readonly LoginService _loginService;

        public FavoritesViewModel(IFavoritesApiService apiService, 
            LoginService loginService,
            INotificationsApiService notificationsApiService)
        {
            _favoritesApiService = apiService;
            _loginService = loginService;
            _notificationsApiService = notificationsApiService;

            _ = InitializeFields();
        }

        // User
        [ObservableProperty]
        private Guid currentUserId;

        //Favorites
        [ObservableProperty]
        private ObservableCollection<Favorite> favorites = new();

        [ObservableProperty]
        private PagedResult<Favorite>? pageResult;

        // Фильтры
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
        public DateTime? selectedStartDate;

        [ObservableProperty]
        public DateTime? selectedEndDate;

        //UI
        [ObservableProperty]
        private bool showCustomDateInput;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isPagination;

        [ObservableProperty]
        private bool isPaginationEnable;

        [ObservableProperty]
        private bool isLoadFailed;

        [ObservableProperty]
        private bool isStartLoadFailed;

        public bool IsShowScrollButtons => IsBusy != true && IsStartLoadFailed != true && Favorites.Any();

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

            try
            {
                await LoadFavoritesAsync();

                Favorites.Clear();
                if (IsLoadFailed)
                {
                    IsStartLoadFailed = true;
                    return;
                }

                IsStartLoadFailed = false;

                if (PageResult?.Items != null)
                {
                    foreach (var item in PageResult.Items)
                    {
                        Favorites.Add(item);
                    }
                }

                OnPropertyChanged(nameof(Favorites));
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

        private async Task LoadFavoritesAsync()
        {
            try
            {
                var response = await _favoritesApiService.GetMyFavoritesAsync((Filter)Filter);

                if (response == null || response?.Data == null || !response.Success)
                {
                    IsLoadFailed = true;
                    return;
                }

                IsLoadFailed = false;
                PageResult = response.Data;

                if (!PageResult.Items.Any()) return;

                ValidatePagination();

            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task LoadFavoritesPaginationAsync()
        {
            if (IsPagination) return;

            IsPagination = true;

            try
            {
                var nextPage = (Favorites.Count / Filter.PageSize) + 1;
                Filter.Page = nextPage;

                await LoadFavoritesAsync();

                if (PageResult == null || IsLoadFailed)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "не удалось загрузить уведомления.\nПроверьте подключение к интернету", "ОК");
                    return;
                }

                if (PageResult?.Items != null && PageResult.Items.Any())
                {
                    foreach (var item in PageResult.Items)
                    {
                        // Проверяем, нет ли уже такого элемента
                        if (!Favorites.Any(n => n.NotificationId == item.NotificationId))
                        {
                            Favorites.Add(item);
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



        [RelayCommand]
        public async Task ApplyFiltersAsync()
        {
            Filter.Page = 1;
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
            SelectedSortOrder = SortOrderItems[1];
            SelectedDateRange = DateRangeOptions[0];
            Filter.StartDate = null;
            Filter.EndDate = null;
            await StartAsync();
        }

        private void ApplyCustomDateRange()
        {
            SelectedStartDate = DateTime.Now;
            SelectedEndDate = DateTime.Now;
        }

        private void ApplyDateRange(string rangeType)
        {
            DateFilterResult result = DateUtils.ApplyDateRange(rangeType);

            SelectedStartDate = result.SelectedStartDate;
            SelectedEndDate = result.SelectedEndDate;
        }


        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                IsStartLoadFailed = false;
                await StartAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task MarkAsReadAsync(Favorite favorite)
        {
            if (!favorite.IsEnable)
            {
                await Shell.Current.DisplayAlert("Доступ запрещен",
                    "Вы не можете отметить это уведомление как прочитанное, так как не являетесь его получателем.",
                    "OK");
                return;
            }

            if (favorite.IsRead) return;

            try
            {
                var success = await _notificationsApiService.MarkAsReadAsync(favorite.NotificationId);
                if (success)
                {
                    favorite.IsRead = true;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RemoveFavoriteAsync(Favorite favorite)
        {
            try
            {
                var notificationId = favorite.NotificationId;

                var result = await _favoritesApiService.RemoveFavoriteAsync(notificationId);

                if (result == null || !result.Success)
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"Не удалось обновить ответ: {result?.Message ?? "проверьте подключение к интернету"}", "OK");
                }
                Favorites.Remove(favorite);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить из избранного: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task GoToRepliesPageAsync(Guid notificationId)
        {
            var favorite = Favorites.FirstOrDefault(f => f.NotificationId == notificationId);

            if (favorite != null && !favorite.IsEnable)
            {
                await Shell.Current.DisplayAlert("Доступ запрещен",
                    "Вы не можете обсуждать это уведомление, так как не являетесь его получателем.",
                    "OK");
                return;
            }

            await Shell.Current.GoToAsync($"RepliesPage?id={notificationId}");
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
                    DisplayName = "Дата добавления"
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
}
