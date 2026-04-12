using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Protocol;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class InviteUsersViewModel : BaseViewModel
    {
        private readonly IChannelApiService _channelApiService;
        private readonly IUsersApiService _usersApiService;
        private readonly LoginService _loginService;

        public InviteUsersViewModel(
            IChannelApiService channelApiService,
            IUsersApiService usersApiService,
            LoginService loginService)
        {
            _channelApiService = channelApiService;
            _usersApiService = usersApiService;
            _loginService = loginService;

            _ = InitializeFields();
        }

        [ObservableProperty]
        private string channelId = string.Empty;

        [ObservableProperty]
        private UserRole? currentUserRole;

        [ObservableProperty]
        private ChannelDto? channel;

        [ObservableProperty]
        private ObservableCollection<AvailableUserDto> users = new();

        [ObservableProperty]
        private AvailableUsersFilter filter = new();

        [ObservableProperty]
        private List<int> pageSizes = new() { 10, 20, 30 };

        [ObservableProperty]
        private List<PickerItem<UserRole?>> roleOptions = new();

        [ObservableProperty]
        private PickerItem<UserRole?>? selectedRole;

        [ObservableProperty]
        private List<Group> groups = new();

        [ObservableProperty]
        private Group? selectedGroup;

        [ObservableProperty]
        private string invitationMessage = string.Empty;

        [ObservableProperty]
        private bool isPagination;

        [ObservableProperty]
        private bool isPaginationEnable;

        [ObservableProperty]
        private int selectedCount;

        // Отдельные состояния загрузки
        [ObservableProperty]
        private bool isChannelLoading = true;

        [ObservableProperty]
        private bool isGroupsLoading = true;

        [ObservableProperty]
        private bool isUsersLoading = true;

        // Отдельные состояния ошибок
        [ObservableProperty]
        private bool isChannelLoadFailed;

        [ObservableProperty]
        private bool isGroupsLoadFailed;

        [ObservableProperty]
        private bool isUsersLoadFailed;

        [ObservableProperty]
        private string channelError = string.Empty;

        [ObservableProperty]
        private string groupsError = string.Empty;

        [ObservableProperty]
        private string usersError = string.Empty;

        public bool ShowGroupSelector => SelectedRole?.Value == UserRole.Student && !IsGroupsLoading && !IsGroupsLoadFailed;
        public bool HasUsers => Users.Any() && !IsUsersLoading && !IsUsersLoadFailed;
        public bool HasSelectedUsers => SelectedCount > 0;
        public bool CanInvite => SelectedCount > 0 && !IsUsersLoading;
        public bool IsShowScrollButtons => !IsUsersLoading && !IsUsersLoadFailed && Users.Any();

        partial void OnSelectedRoleChanged(PickerItem<UserRole?>? value)
        {
            OnPropertyChanged(nameof(ShowGroupSelector));
            if (!ShowGroupSelector)
            {
                SelectedGroup = null;
            }
        }

        partial void OnUsersChanged(ObservableCollection<AvailableUserDto> value)
        {
            UpdateSelectedCount();
            OnPropertyChanged(nameof(HasUsers));
            OnPropertyChanged(nameof(IsShowScrollButtons));
        }

        public async Task InitializeAsync()
        {
            var tasks = new List<Task>
            {
                LoadChannelInfoAsync(),
                LoadGroupsAsync(),
                LoadUsersAsync()
            };

            await Task.WhenAll(tasks);
        }

        private async Task LoadChannelInfoAsync()
        {
            if (string.IsNullOrEmpty(ChannelId))
            {
                IsChannelLoading = false;
                return;
            }

            IsChannelLoading = true;
            IsChannelLoadFailed = false;
            ChannelError = string.Empty;

            try
            {
                var response = await _channelApiService.GetChannelByIdAsync(Guid.Parse(ChannelId));
                if (response?.Success == true && response.Data != null)
                {
                    Channel = response.Data;
                    Title = $"Пригласить в {Channel.Name}";
                }
                else
                {
                    IsChannelLoadFailed = true;
                    ChannelError = response?.Message ?? "Не удалось загрузить информацию о канале";
                }
            }
            catch (Exception ex)
            {
                IsChannelLoadFailed = true;
                ChannelError = ex.Message;
                Console.WriteLine($"Load channel info error: {ex.Message}");
            }
            finally
            {
                IsChannelLoading = false;
            }
        }

        private async Task LoadGroupsAsync()
        {
            IsGroupsLoading = true;
            IsGroupsLoadFailed = false;
            GroupsError = string.Empty;
            OnPropertyChanged(nameof(ShowGroupSelector));
            try
            {
                var groupFilter = new GroupFilter();
                var response = await _usersApiService.GetGroupsAsync(groupFilter);
                Groups = response ?? new List<Group>();

                if (Groups.Count == 0)
                {
                    // Это не ошибка, просто нет групп
                    GroupsError = "Группы не найдены";
                }
            }
            catch (Exception ex)
            {
                IsGroupsLoadFailed = true;
                GroupsError = ex.Message;
                Console.WriteLine($"Load groups error: {ex.Message}");
            }
            finally
            {
                IsGroupsLoading = false;
                OnPropertyChanged(nameof(ShowGroupSelector));
            }
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            if (string.IsNullOrEmpty(ChannelId)) return;

            IsUsersLoading = true;
            IsUsersLoadFailed = false;
            UsersError = string.Empty;
            IsPaginationEnable = false;

            try
            {
                Filter.Page = 1;
                Filter.Role = SelectedRole?.Value;
                Filter.GroupId = SelectedGroup?.Id;

                var response = await _channelApiService.GetAvailableUsersAsync(Guid.Parse(ChannelId), Filter);

                if (response?.Success == true && response.Data != null)
                {
                    Users.Clear();
                    foreach (var user in response.Data)
                    {
                        user.IsSelected = false;
                        Users.Add(user);
                    }

                    IsPaginationEnable = response.Pagination.Page < response.Pagination.TotalPages;
                }
                else
                {
                    IsUsersLoadFailed = true;
                    UsersError = response?.Message ?? "Не удалось загрузить пользователей";
                }
            }
            catch (Exception ex)
            {
                IsUsersLoadFailed = true;
                UsersError = ex.Message;
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsUsersLoading = false;
                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(IsShowScrollButtons));
            }
        }

        [RelayCommand]
        private async Task RetryLoadChannelAsync()
        {
            await LoadChannelInfoAsync();
        }

        [RelayCommand]
        private async Task RetryLoadGroupsAsync()
        {
            await LoadGroupsAsync();
        }

        [RelayCommand]
        private async Task RetryLoadUsersAsync()
        {
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsPagination || !IsPaginationEnable || string.IsNullOrEmpty(ChannelId)) return;

            IsPagination = true;

            try
            {
                Filter.Page++;

                var response = await _channelApiService.GetAvailableUsersAsync(Guid.Parse(ChannelId), Filter);

                if (response?.Success == true && response.Data != null)
                {
                    foreach (var user in response.Data)
                    {
                        if (!Users.Any(u => u.Id == user.Id))
                        {
                            user.IsSelected = false;
                            Users.Add(user);
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
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            Filter = new AvailableUsersFilter();
            SelectedRole = null;
            SelectedGroup = null;
            await LoadUsersAsync();
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var user in Users)
            {
                user.IsSelected = true;
            }
            UpdateSelectedCount();
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var user in Users)
            {
                user.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        [RelayCommand]
        private void ToggleUserSelection(AvailableUserDto user)
        {
            if (user != null)
            {
                user.IsSelected = !user.IsSelected;
                UpdateSelectedCount();
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            DeselectAll();
        }

        [RelayCommand]
        private async Task InviteAsync()
        {
            var selectedUsers = Users.Where(u => u.IsSelected).ToList();
            if (!selectedUsers.Any())
            {
                await Shell.Current.DisplayAlert("Ошибка", "Выберите хотя бы одного пользователя", "OK");
                return;
            }

            if (string.IsNullOrEmpty(ChannelId))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Канал не выбран", "OK");
                return;
            }

            IsUsersLoading = true;

            try
            {
                var request = new InviteUsersRequest
                {
                    UserIds = selectedUsers.Select(u => u.Id).ToList(),
                    Message = string.IsNullOrWhiteSpace(InvitationMessage) ? null : InvitationMessage.Trim()
                };

                var response = await _channelApiService.InviteUsersAsync(Guid.Parse(ChannelId), request);

                if (response?.Success == true)
                {
                    await Shell.Current.DisplayAlert("Успех", $"Приглашения отправлены ({selectedUsers.Count} чел.)", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось отправить приглашения", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsUsersLoading = false;
            }
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = Users.Count(u => u.IsSelected);
            OnPropertyChanged(nameof(HasSelectedUsers));
            OnPropertyChanged(nameof(CanInvite));
        }

        private async Task InitializeFields()
        {
            var auth = _loginService.GetCurrentAuth();
            CurrentUserRole = Enum.TryParse(auth?.Role, out UserRole role) ? role : null;

            // Создаем все возможные опции
            var allRoles = new List<PickerItem<UserRole?>>
                {
                    new() { Value = null, DisplayName = "Все роли" },
                    new() { Value = UserRole.Student, DisplayName = "Студенты" },
                    new() { Value = UserRole.Teacher, DisplayName = "Учителя" },
                    new() { Value = UserRole.Parent, DisplayName = "Родители" },
                    new() { Value = UserRole.Administrator, DisplayName = "Администраторы" }
                };

            // Определяем доступные роли для фильтрации
            var availableRoles = GetAvailableRolesForCurrentUser();
            RoleOptions = allRoles.Where(r => availableRoles.Contains(r.Value)).ToList();
        }

        private List<UserRole?> GetAvailableRolesForCurrentUser()
        {
            return CurrentUserRole switch
            {
                UserRole.Student => new List<UserRole?> { null, UserRole.Parent, UserRole.Student },
                UserRole.Parent => new List<UserRole?> { null, UserRole.Parent, UserRole.Student },
                UserRole.Teacher => new List<UserRole?> { null, UserRole.Student, UserRole.Teacher, UserRole.Parent, UserRole.Administrator },
                UserRole.Administrator => new List<UserRole?> { null, UserRole.Student, UserRole.Teacher, UserRole.Parent, UserRole.Administrator },
                _ => new List<UserRole?> { null, UserRole.Student, UserRole.Teacher, UserRole.Parent, UserRole.Administrator }
            };
        }
    }
}
