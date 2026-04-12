using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class ChannelsViewModel: BaseViewModel
    {
        private readonly IChannelApiService _channelApiService;
        private readonly SignalRService _signalRService;

        public ChannelsViewModel(
            IChannelApiService channelApiService,
            SignalRService signalRService)
        {
            _channelApiService = channelApiService;
            _signalRService = signalRService;

            _signalRService.OnChannelInvitation += OnChannelInvitationReceived;
            _signalRService.OnInvitationAccepted += OnInvitationAccepted;
            _signalRService.OnInvitationDeclined += OnInvitationDeclined;

            Title = "Каналы";
            _ = InitializeFields();
        }

        [ObservableProperty]
        private ObservableCollection<ChannelDto> channels = new();

        [ObservableProperty]
        private ChannelDto? selectedChannel;

        [ObservableProperty]
        private ChannelFilter filter = new();

        [ObservableProperty]
        private List<int> pageSizes = new() { 5, 10, 20 };

        [ObservableProperty]
        private List<PickerItem<ChannelSortBy>> sortByItems = new();

        [ObservableProperty]
        private List<PickerItem<SortOrder>> sortOrderItems = new();

        [ObservableProperty]
        private PickerItem<ChannelSortBy>? selectedSortBy;

        [ObservableProperty]
        private PickerItem<SortOrder>? selectedSortOrder;

        [ObservableProperty]
        private bool isPagination;

        [ObservableProperty]
        private bool isPaginationEnable;

        [ObservableProperty]
        private bool isStartLoadFailed;

        [ObservableProperty]
        private int pendingInvitationsCount;

        [ObservableProperty]
        private bool showInvitationsBanner = true;

        public bool HasPendingInvitations => ShowInvitationsBanner && PendingInvitationsCount > 0 && !IsBusy;

        public bool IsShowScrollButtons => IsBusy != true && IsStartLoadFailed != true && Channels.Any();

        public async Task InitializeAsync()
        {
            await LoadChannelsAsync();
            await CheckPendingInvitationsAsync();
        }


        [RelayCommand]
        private async Task LoadChannelsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            IsStartLoadFailed = false;
            OnPropertyChanged(nameof(HasPendingInvitations));
            OnPropertyChanged(nameof(IsShowScrollButtons));
            try
            {
                Filter.Page = 1;
                var response = await _channelApiService.GetMyChannelsAsync(Filter);

                if (response?.Success == true && response.Data != null)
                {
                    Channels.Clear();
                    foreach (var channel in response.Data)
                    {
                        Channels.Add(channel);
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
                OnPropertyChanged(nameof(HasPendingInvitations));
            }
        }

        [RelayCommand]
        private async Task LoadMoreChannelsAsync()
        {
            if (IsPagination || !IsPaginationEnable) return;

            IsPagination = true;

            try
            {
                Filter.Page++;
                var response = await _channelApiService.GetMyChannelsAsync(Filter);

                if (response?.Success == true && response.Data != null)
                {
                    foreach (var channel in response.Data)
                    {
                        if (!Channels.Any(c => c.Id == channel.Id))
                        {
                            Channels.Add(channel);
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
        private async Task ApplyFiltersAsync()
        {
            Filter.Page = 1;
            Filter.SortBy = SelectedSortBy?.Value ?? ChannelSortBy.CreatedAt;
            Filter.SortOrder = SelectedSortOrder?.Value ?? SortOrder.Descending;
            await LoadChannelsAsync();
        }

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            Filter = new ChannelFilter();
            SelectedSortBy = SortByItems.First();
            SelectedSortOrder = SortOrderItems.First(s => s.Value == SortOrder.Descending);
            await LoadChannelsAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadChannelsAsync();
            await CheckPendingInvitationsAsync();
        }

        [RelayCommand]
        private async Task CreateChannelAsync()
        {
            await Shell.Current.GoToAsync("CreateChannelPage");
        }

        [RelayCommand]
        private async Task ChannelSelectedAsync()
        {
            if (SelectedChannel == null) return;

            var channelId = SelectedChannel.Id;
            SelectedChannel = null;
            await Shell.Current.GoToAsync($"ChannelMessagesPage?channelId={channelId}");
        }

        [RelayCommand]
        private async Task ShowChannelMenuAsync(ChannelDto channel)
        {
            var action = await Shell.Current.DisplayActionSheet(
                channel.Name,
                "Отмена",
                null,
                "Открыть",
                "Участники",
                channel.IsOwner || channel.UserRole == ChannelRole.Admin ? "Пригласить" : null,
                "Покинуть канал");

            switch (action)
            {
                case "Открыть":
                    await Shell.Current.GoToAsync($"ChannelMessagesPage?channelId={channel.Id}");
                    break;
                case "Участники":
                    await Shell.Current.GoToAsync($"ChannelMembersPage?channelId={channel.Id}");
                    break;
                case "Пригласить":
                    await Shell.Current.GoToAsync($"InviteUsersPage?channelId={channel.Id}");
                    break;
                case "Покинуть канал":
                    await LeaveChannelAsync(channel);
                    break;
            }
        }

        private async Task LeaveChannelAsync(ChannelDto channel)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Покинуть канал",
                $"Вы уверены, что хотите покинуть канал \"{channel.Name}\"?",
                "Да", "Нет");

            if (!confirm) return;

            try
            {
                var response = await _channelApiService.LeaveChannelAsync(channel.Id);
                if (response?.Success == true)
                {
                    Channels.Remove(channel);
                    await Shell.Current.DisplayAlert("Успех", "Вы покинули канал", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось покинуть канал", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task ViewInvitationsAsync()
        {
            await Shell.Current.GoToAsync("ChannelInvitationsPage");
        }

        [RelayCommand]
        private void DismissInvitationsBanner()
        {
            ShowInvitationsBanner = false;
            OnPropertyChanged("HasPendingInvitations");
        }

        private async Task CheckPendingInvitationsAsync()
        {
            try
            {
                var filter = new ChannelFilter { Page = 1, PageSize = 5 };
                var response = await _channelApiService.GetMyInvitationsAsync(filter);

                if (response?.Success == true && response.Data != null)
                {
                    PendingInvitationsCount = response.Data.Count(i => i.Status == InvitationStatus.Pending);
                    OnPropertyChanged("HasPendingInvitations");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Check invitations error: {ex.Message}");
            }
        }

        private void OnChannelInvitationReceived(ChannelInvitationDto invitation)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CheckPendingInvitationsAsync();
            });
        }

        private void OnInvitationAccepted(ChannelInvitationDto invitation)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadChannelsAsync();
                await CheckPendingInvitationsAsync();
            });
        }

        private void OnInvitationDeclined(ChannelInvitationDto invitation)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CheckPendingInvitationsAsync();
            });
        }

        private async Task InitializeFields()
        {
            SortByItems = new()
        {
            new() { Value = ChannelSortBy.CreatedAt, DisplayName = "Дата создания" },
            new() { Value = ChannelSortBy.Name, DisplayName = "Название" },
            new() { Value = ChannelSortBy.MembersCount, DisplayName = "Количество участников" }
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
            _signalRService.OnChannelInvitation -= OnChannelInvitationReceived;
            _signalRService.OnInvitationAccepted -= OnInvitationAccepted;
            _signalRService.OnInvitationDeclined -= OnInvitationDeclined;
        }
    }
}
