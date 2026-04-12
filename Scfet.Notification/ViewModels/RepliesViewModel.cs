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
using Scfet.Notification.Utils;

namespace Scfet.Notification.ViewModels
{
    public partial class RepliesViewModel : ObservableObject
    {
        private readonly IRepliesApiService _repliesApiService;
        private readonly INotificationsApiService _notificationsApiService;
        private readonly IFavoritesApiService _favoritesApiService;
        private readonly SignalRService _notificationService;
        private readonly LoginService _loginService;

        public RepliesViewModel(IRepliesApiService repliesApiService,
            SignalRService notificationService, LoginService loginService,
            INotificationsApiService notificationsApiService,
            IFavoritesApiService favoritesApiService)
        {
            _repliesApiService = repliesApiService;
            _notificationsApiService = notificationsApiService;
            _notificationService = notificationService;
            _favoritesApiService = favoritesApiService;
            _loginService = loginService;

            _notificationService.OnReplyRead += OnReplyReceived;
            _notificationService.OnReplyUpdate += OnReplyUpdate;
            _notificationService.OnReplyRemove += OnReplyRemove;

            _ = InitializeFields();
        }

        // Пользователь
        [ObservableProperty]
        private Guid currentUserId;

        // Уведомление
        public Guid NotificationId { get; set; }

        [ObservableProperty]
        private Guid notificationSenderId;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;

        [ObservableProperty]
        private string? imageUrl = string.Empty;

        [ObservableProperty]
        private string? senderName = string.Empty;

        [ObservableProperty]
        private string? senderRole = string.Empty;

        [ObservableProperty]
        private string? senderAvatarUrl = string.Empty;

        [ObservableProperty]
        private bool isFavorite;

        [ObservableProperty]
        private DateTime? createdAt;

        [ObservableProperty]
        private bool isPersonal;

        // Ответы
        [ObservableProperty]
        private ObservableCollection<Reply> replies = new();

        [ObservableProperty]
        private PagedResult<Reply>? pageResult;

        [ObservableProperty]
        private string replyMessage = string.Empty;

        [ObservableProperty]
        private string editingMessage = string.Empty;

        [ObservableProperty]
        private Guid editingReplyId;

        // Фильтры
        [ObservableProperty]
        public Filter filter = new();

        [ObservableProperty]
        public List<int> pageSizes = new List<int> { 5, 10, 20 };

        [ObservableProperty]
        public List<PickerItem<SortOrder>> sortOrderItems = new();

        [ObservableProperty]
        public List<PickerItem<string>> dateRangeOptions = new();

        [ObservableProperty]
        private PickerItem<string> selectedDateRange;

        [ObservableProperty]
        public PickerItem<SortOrder>? selectedSortOrder;

        [ObservableProperty]
        public DateTime? selectedStartDate;

        [ObservableProperty]
        public DateTime? selectedEndDate;

        // UI
        [ObservableProperty]
        private bool showReplyUpdateForm;

        [ObservableProperty]
        private bool showCustomDateInput;

        [ObservableProperty]
        private bool isReplyCreation;

        [ObservableProperty]
        private bool isReplyUpdating;

        [ObservableProperty]
        private bool isReplyRemoving;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isPagination;

        [ObservableProperty]
        private bool isPaginationEnable;

        [ObservableProperty]
        private bool isLoadRepliesFailed;

        [ObservableProperty]
        private bool isStartLoadRepliesFailed;

        public bool IsShowScrollButtons => IsBusy != true && IsStartLoadRepliesFailed != true && Replies.Any();

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

        private async Task StartAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            await _notificationService.ConnectAsync();

            OnPropertyChanged(nameof(IsShowScrollButtons));

            try
            {
                await LoadNotificationAsync();

                await LoadRepliesAsync();

                Replies.Clear();

                if (IsLoadRepliesFailed)
                {
                    IsStartLoadRepliesFailed = true;
                    return;
                }

                IsStartLoadRepliesFailed = false;

                if (PageResult?.Items != null)
                {
                    foreach (var reply in PageResult.Items)
                    {
                        Replies.Add(reply);
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
                OnPropertyChanged(nameof(IsShowScrollButtons));
            }
        }

        private async Task LoadNotificationAsync()
        {
            try
            {
                var notification = await _notificationsApiService.GetNotificationById(NotificationId);
                if (notification == null)
                {
                    IsStartLoadRepliesFailed = true;
                    return;
                }

                Title = notification.Title;
                Message = notification.Message;
                ImageUrl = notification.ImageUrl;
                CreatedAt = notification.CreatedAt;
                SenderName = notification.SenderName;
                SenderRole = notification.SenderRole;
                SenderAvatarUrl = notification.SenderAvatarUrl;
                IsPersonal = notification.IsPersonal;
                IsFavorite = notification.IsFavorite;
                NotificationSenderId = notification.SenderId;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task LoadRepliesAsync()
        {
            try
            {
                var response = await _repliesApiService.GetNotificationRepliesAsync(NotificationId, (Filter)Filter);

                if (response == null || response?.Data == null || !response.Success)
                {
                    IsLoadRepliesFailed = true;
                    return;
                }

                IsLoadRepliesFailed = false;
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
        public async Task LoadRepliesPaginationAsync()
        {
            if (IsPagination) return;

            IsPagination = true;

            try
            {
                var nextPage = (Replies.Count / Filter.PageSize) + 1;
                Filter.Page = nextPage;

                await LoadRepliesAsync();

                if (PageResult == null || IsLoadRepliesFailed)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "не удалось загрузить уведомления.\nПроверьте подключение к интернету", "ОК");
                    return;
                }

                if (PageResult?.Items != null && PageResult.Items.Any())
                {
                    foreach (var reply in PageResult.Items)
                    {
                        // Проверяем, нет ли уже такого ответа
                        if (!Replies.Any(n => n.Id == reply.Id))
                        {
                            Replies.Add(reply);
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

            ClearReplyEditFields();

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

        private void ClearReplyEditFields()
        {
            EditingReplyId = Guid.Empty;
            EditingMessage = string.Empty;
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                IsStartLoadRepliesFailed = false;
                await StartAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
        }

        private async Task<bool> IsReplyValid()
        {
            if (string.IsNullOrWhiteSpace(ReplyMessage))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Введите текст ответа", "OK");
                return false;
            }
            else if (ReplyMessage.Length > 500)
            {
                await Shell.Current.DisplayAlert("Ошибка", "ответ не может быть больше 500 символов", "OK");
                return false;
            }
            return true;
        }

        [RelayCommand]
        private async Task CreateReplyAsync()
        {
            if (IsReplyCreation) return;

            if (!await IsReplyValid()) return;

            IsReplyCreation = true;

            try
            {
                var createReply = new CreateReply
                {
                    NotificationId = NotificationId,
                    Message = ReplyMessage
                };

                var result = await _repliesApiService.CreateReplyAsync(createReply);

                if (result != null && result.Success)
                {
                    ReplyMessage = string.Empty;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"Не удалось отправить ответ: {result?.Message ?? "проверьте подключение к интернету"}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось отправить ответ: {ex.Message}", "OK");
            }
            finally
            {
                IsReplyCreation = false;
            }
        }

        [RelayCommand]
        private void StartReplyUpdateProcess(Reply reply)
        {
            if (reply == null) return;

            // Закрываем форму редактирования, если она уже открыта для этого ответа
            if (EditingReplyId == reply.Id)
            {
                EditingReplyId = Guid.Empty;
                EditingMessage = string.Empty;
            }
            else
            {
                // Открываем форму редактирования для выбранного ответа
                EditingReplyId = reply.Id;
                EditingMessage = reply.Message; 
            }

            OnPropertyChanged(nameof(EditingReplyId));
            OnPropertyChanged(nameof(EditingMessage));
        }

        [RelayCommand]
        private async Task UpdateReplyAsync()
        {
            if (IsReplyUpdating) return;

            if (string.IsNullOrWhiteSpace(EditingMessage))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Введите текст ответа", "OK");
                return;
            }

            IsReplyUpdating = true;

            try
            {
                var updateReply = new UpdateReply
                {
                    Message = EditingMessage
                };

                var result = await _repliesApiService.UpdateReplyAsync(EditingReplyId, updateReply);

                if (result != null && result.Success)
                {
                    EditingMessage = string.Empty;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"Не удалось обновить ответ: {result?.Message ?? "проверьте подключение к интернету"}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось обновить ответ: {ex.Message}", "OK");
            }
            finally
            {
                IsReplyUpdating = false;
                ClearReplyEditFields();
            }
        }

        [RelayCommand]
        private async Task RemoveReplyAsync(Guid replyId)
        {
            if (IsReplyRemoving) return;

            IsReplyRemoving = true;

            try
            {

                var result = await _repliesApiService.RemoveReplyAsync(replyId);

                if (result == null || !result.Success)
                {
                    await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить ответ: {result?.Message ?? "проверьте подключение к интернету"}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить ответ: {ex.Message}", "OK");
            }
            finally
            {
                IsReplyRemoving = false;
            }
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync()
        {
            try
            {
                var originalState = IsFavorite;
                ApiResponse? response;
                if (!originalState)
                {
                    AddFavorite request = new AddFavorite()
                    {
                        NotificationId = NotificationId
                    };
                    response = await _favoritesApiService.AddFavoriteAsync(request);
                }
                else
                {
                    response = await _favoritesApiService.RemoveFavoriteAsync(NotificationId);
                }
                if (response == null || !response.Success)
                {
                    await Shell.Current
                        .DisplayAlert("Ошибка", $"Не удалось {(!IsFavorite == true ? "добавить в" : "удалить из")} избранное: {response?.Message ?? "Проверьте интернет соединение"}", "OK");
                    return;
                }
                IsFavorite = !originalState;

            }
            catch (Exception ex)
            {
                // Откатываем изменение при ошибке
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось {(!IsFavorite == true ? "добавить в" : "удалить из")} избранное: {ex.Message}", "OK");
                IsFavorite = !IsFavorite;
            }     
        }

        private void OnReplyReceived(Reply reply)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!Replies.Any(r => r.Id == reply.Id))
                {
                    Replies.Insert(0, reply);
                    OnPropertyChanged(nameof(Replies));
                }
            });
        }

        private void OnReplyUpdate(Reply replyUpdated)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var reply = Replies.FirstOrDefault(r => r.Id == replyUpdated.Id);

                if (reply != null)
                {
                    var index = Replies.IndexOf(reply);

                    Replies.RemoveAt(index);
                    Replies.Insert(index, replyUpdated);

                    OnPropertyChanged(nameof(Replies));
                }
            });
        }

        private void OnReplyRemove(Guid replyId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var reply = Replies.FirstOrDefault(r => r.Id == replyId);
                if (reply != null) Replies.Remove(reply);
            });
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
            SelectedSortOrder = SortOrderItems[1];
            SelectedDateRange = DateRangeOptions[0];

            var auth = _loginService.GetCurrentAuth();
            var result = Guid.TryParse(auth?.UserId, out Guid currentUserId);

            CurrentUserId = result ? currentUserId : Guid.Empty;
        }

    }
}
