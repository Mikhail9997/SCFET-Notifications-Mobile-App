using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class ChannelInvitationsViewModel : BaseViewModel
    {
        private readonly IChannelApiService _channelApiService;
        private readonly SignalRService _signalRService;

        public ChannelInvitationsViewModel(
            IChannelApiService channelApiService,
            SignalRService signalRService)
        {
            _channelApiService = channelApiService;
            _signalRService = signalRService;

            Title = "Приглашения";
            _ = InitializeFields();
        }

        [ObservableProperty]
        private ObservableCollection<ChannelInvitationDto> invitations = new();

        [ObservableProperty]
        private ChannelFilter filter = new();

        [ObservableProperty]
        private List<int> pageSizes = new() { 10, 20, 30 };

        [ObservableProperty]
        private List<PickerItem<ChannelSortBy>> sortByItems = new();

        [ObservableProperty]
        private List<PickerItem<SortOrder>> sortOrderItems = new();

        [ObservableProperty]
        private PickerItem<ChannelSortBy>? selectedSortBy;

        [ObservableProperty]
        private PickerItem<SortOrder>? selectedSortOrder;

        [ObservableProperty]
        private bool isIncomingTab = true;

        [ObservableProperty]
        private bool isPagination;

        [ObservableProperty]
        private bool isPaginationEnable;

        [ObservableProperty]
        private bool isStartLoadFailed;

        public bool IsShowScrollButtons => IsBusy != true && IsStartLoadFailed != true && Invitations.Any();

        public string EmptyStateText => IsIncomingTab
            ? "Нет входящих приглашений"
            : "Нет исходящих приглашений";

        public string EmptyStateDescription => IsIncomingTab
            ? "Здесь будут появляться приглашения в каналы от других пользователей"
            : "Вы еще никого не пригласили в каналы";

        public async Task InitializeAsync()
        {
            await LoadInvitationsAsync();
            SubscribeToSignalREvents();
        }

        [RelayCommand]
        private async Task LoadInvitationsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            IsStartLoadFailed = false;
            OnPropertyChanged(nameof(IsShowScrollButtons));
            try
            {
                Filter.Page = 1;

                var response = IsIncomingTab
                    ? await _channelApiService.GetMyInvitationsAsync(Filter)
                    : await _channelApiService.GetSentInvitationsAsync(Filter);

                if (response?.Success == true && response.Data != null)
                {
                    Invitations.Clear();
                    foreach (var invitation in response.Data)
                    {
                        invitation.IsIncomingTab = IsIncomingTab;
                        Invitations.Add(invitation);
                    }

                    IsPaginationEnable = response.Pagination.Page < response.Pagination.TotalPages;
                }
                else
                {
                    IsStartLoadFailed = true;
                }
            }
            catch (Exception ex)
            {
                IsStartLoadFailed = true;
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsShowScrollButtons));
                OnPropertyChanged(nameof(EmptyStateText));
                OnPropertyChanged(nameof(EmptyStateDescription));
            }
        }

        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsPagination || !IsPaginationEnable) return;

            IsPagination = true;

            try
            {
                Filter.Page++;

                var response = IsIncomingTab
                    ? await _channelApiService.GetMyInvitationsAsync(Filter)
                    : await _channelApiService.GetSentInvitationsAsync(Filter);

                if (response?.Success == true && response.Data != null)
                {
                    foreach (var invitation in response.Data)
                    {
                        if (!Invitations.Any(i => i.Id == invitation.Id))
                        {
                            invitation.IsIncomingTab = IsIncomingTab;
                            Invitations.Add(invitation);
                        }
                    }

                    IsPaginationEnable = response.Pagination.Page < response.Pagination.TotalPages;
                }
            }
            catch (Exception ex)
            {
                Filter.Page--;
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsPagination = false;
            }
        }

        [RelayCommand]
        private async Task ShowIncomingAsync()
        {
            if (IsIncomingTab) return;
            IsIncomingTab = true;
            await LoadInvitationsAsync();
        }

        [RelayCommand]
        private async Task ShowOutgoingAsync()
        {
            if (!IsIncomingTab) return;
            IsIncomingTab = false;
            await LoadInvitationsAsync();
        }

        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            Filter.Page = 1;
            Filter.SortBy = SelectedSortBy?.Value ?? ChannelSortBy.CreatedAt;
            Filter.SortOrder = SelectedSortOrder?.Value ?? SortOrder.Descending;
            await LoadInvitationsAsync();
        }

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            Filter = new ChannelFilter();
            SelectedSortBy = SortByItems.First();
            SelectedSortOrder = SortOrderItems.First(s => s.Value == SortOrder.Descending);
            await LoadInvitationsAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadInvitationsAsync();
        }

        [RelayCommand]
        private async Task AcceptInvitationAsync(ChannelInvitationDto invitation)
        {
            if (invitation.Status != InvitationStatus.Pending) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Принять приглашение",
                $"Вы хотите присоединиться к каналу \"{invitation.ChannelName}\"?",
                "Да", "Нет");

            if (!confirm) return;

            IsBusy = true;

            try
            {
                var response = await _channelApiService.AcceptInvitationAsync(invitation.Id);

                if (response?.Success == true)
                {
                    invitation.Status = InvitationStatus.Accepted;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось принять приглашение", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeclineInvitationAsync(ChannelInvitationDto invitation)
        {
            if (invitation.Status != InvitationStatus.Pending) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Отклонить приглашение",
                $"Вы уверены, что хотите отклонить приглашение в канал \"{invitation.ChannelName}\"?",
                "Да", "Нет");

            if (!confirm) return;

            IsBusy = true;

            try
            {
                var response = await _channelApiService.DeclineInvitationAsync(invitation.Id);

                if (response?.Success == true)
                {
                    invitation.Status = InvitationStatus.Declined;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось отклонить приглашение", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelInvitationAsync(ChannelInvitationDto invitation)
        {
            if (invitation.Status != InvitationStatus.Pending) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Отменить приглашение",
                $"Вы уверены, что хотите отменить приглашение для {invitation.InviteeName}?",
                "Да", "Нет");

            if (!confirm) return;

            IsBusy = true;

            try
            {
                var response = await _channelApiService.CancelInvitationAsync(invitation.Id);

                if (response?.Success == true)
                {
                    Invitations.Remove(invitation);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось отменить приглашение", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteInvitationAsync(ChannelInvitationDto invitation)
        {
            if (invitation.Status == InvitationStatus.Pending) return;

            var confirm = await Shell.Current.DisplayAlert(
                "Удалить приглашение",
                "Вы уверены, что хотите удалить это приглашение?",
                "Да", "Нет");

            if (!confirm) return;

            IsBusy = true;

            try
            {
                var response = await _channelApiService.DeleteInvitationAsync(invitation.Id);

                if (response?.Success == true)
                {
                    Invitations.Remove(invitation);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось удалить приглашение", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnInvitationReceived(ChannelInvitationDto invitation)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (IsIncomingTab && !Invitations.Any(i => i.Id == invitation.Id))
                {
                    invitation.IsIncomingTab = true;
                    Invitations.Insert(0, invitation);
                }
            });
        }

        private void OnInvitationUpdated(ChannelInvitationDto invitation)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Invitations.FirstOrDefault(i => i.Id == invitation.Id);
                if (existing != null)
                {
                    var index = Invitations.IndexOf(existing);
                    Invitations[index] = invitation;
                }
            });
        }

        private void OnInvitationCancelled(ChannelInvitationDto invitation)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Invitations.FirstOrDefault(i => i.Id == invitation.Id);
                if (existing != null)
                {
                    Invitations.Remove(existing);
                }
            });
        }

        private void SubscribeToSignalREvents()
        {
            _signalRService.OnChannelInvitation += OnInvitationReceived;
            _signalRService.OnInvitationAccepted += OnInvitationUpdated;
            _signalRService.OnInvitationDeclined += OnInvitationUpdated;
            _signalRService.OnInvitationCancelled += OnInvitationCancelled;
        }

        private async Task InitializeFields()
        {
            SortByItems = new()
            {
                new() { Value = ChannelSortBy.CreatedAt, DisplayName = "Дата" },
                new() { Value = ChannelSortBy.Name, DisplayName = "Название канала" }
            };

            SortOrderItems = new()
            {
                new() { Value = SortOrder.Ascending, DisplayName = "По возрастанию" },
                new() { Value = SortOrder.Descending, DisplayName = "По убыванию" }
            };

            SelectedSortBy = SortByItems.First();
            SelectedSortOrder = SortOrderItems.First(s => s.Value == SortOrder.Descending);
        }

        public void Cleanup()
        {
            _signalRService.OnChannelInvitation -= OnInvitationReceived;
            _signalRService.OnInvitationAccepted -= OnInvitationUpdated;
            _signalRService.OnInvitationDeclined -= OnInvitationUpdated;
            _signalRService.OnInvitationCancelled -= OnInvitationCancelled;
        }
    }
}
