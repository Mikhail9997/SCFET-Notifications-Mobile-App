using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scfet.Notification.Models.Channel;
using Scfet.Notification.Services;
using Scfet.Notification.Services.Api;

namespace Scfet.Notification.ViewModels
{
    public partial class ChannelMembersViewModel : BaseViewModel
    {
        private readonly IChannelApiService _channelApiService;
        private readonly LoginService _loginService;

        public ChannelMembersViewModel(
            IChannelApiService channelApiService,
            LoginService loginService)
        {
            _channelApiService = channelApiService;
            _loginService = loginService;
        }

        [ObservableProperty]
        private string channelId = string.Empty;

        [ObservableProperty]
        private Guid currentUserId;

        [ObservableProperty]
        private ChannelDto? channel;

        [ObservableProperty]
        private ObservableCollection<ChannelMemberDto> members = new();

        [ObservableProperty]
        private ObservableCollection<ChannelMemberDto> filteredMembers = new();

        [ObservableProperty]
        private string searchTerm = string.Empty;

        [ObservableProperty]
        private bool isChannelLoading = true;

        [ObservableProperty]
        private bool isMembersLoading = true;

        [ObservableProperty]
        private bool isChannelLoadFailed;

        [ObservableProperty]
        private bool isMembersLoadFailed;

        [ObservableProperty]
        private string channelError = string.Empty;

        [ObservableProperty]
        private string membersError = string.Empty;

        [ObservableProperty]
        private int membersCount;

        public bool CanManageMembers
        {
            get
            {
                if (Channel == null) return false;

                var currentMember = Members.FirstOrDefault(m => m.UserId == CurrentUserId);
                if (currentMember == null) return false;

                return currentMember.ChannelRole == ChannelRole.Owner ||
                       currentMember.ChannelRole == ChannelRole.Admin;
            }
        }

        public bool IsShowScrollButtons => !IsMembersLoading && !IsMembersLoadFailed && FilteredMembers.Any();

        partial void OnSearchTermChanged(string value)
        {
            FilterMembers();
        }

        public async Task InitializeAsync()
        {
            var auth = _loginService.GetCurrentAuth();
            CurrentUserId = Guid.TryParse(auth?.UserId, out var id) ? id : Guid.Empty;

            List<Task> tasks = new()
            {
                LoadChannelInfoAsync(),
                LoadMembersAsync()
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
                    Title = $"Участники: {Channel.Name}";
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

        [RelayCommand]
        private async Task LoadMembersAsync()
        {
            if (string.IsNullOrEmpty(ChannelId)) return;

            IsMembersLoading = true;
            IsMembersLoadFailed = false;
            MembersError = string.Empty;

            try
            {
                var response = await _channelApiService.GetChannelMembersAsync(Guid.Parse(ChannelId));

                if (response?.Success == true && response.Data != null)
                {
                    Members.Clear();
                    foreach (var member in response.Data)
                    {
                        member.IsCurrentUser = member.UserId == CurrentUserId;
                        Members.Add(member);
                    }

                    MembersCount = Members.Count;
                    FilterMembers();

                    OnPropertyChanged(nameof(CanManageMembers));
                }
                else
                {
                    IsMembersLoadFailed = true;
                    MembersError = response?.Message ?? "Не удалось загрузить участников";
                }
            }
            catch (Exception ex)
            {
                IsMembersLoadFailed = true;
                MembersError = ex.Message;
                await Shell.Current.DisplayAlert("Ошибка", $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsMembersLoading = false;
                OnPropertyChanged(nameof(IsShowScrollButtons));
            }
        }

        private void FilterMembers()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                FilteredMembers = new ObservableCollection<ChannelMemberDto>(Members);
            }
            else
            {
                var filtered = Members.Where(m =>
                    m.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    m.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                FilteredMembers = new ObservableCollection<ChannelMemberDto>(filtered);
            }

            OnPropertyChanged(nameof(FilteredMembers));
            OnPropertyChanged(nameof(IsShowScrollButtons));
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            FilterMembers();
        }

        [RelayCommand]
        private async Task RetryLoadChannelAsync()
        {
            await LoadChannelInfoAsync();
        }

        [RelayCommand]
        private async Task RetryLoadMembersAsync()
        {
            await LoadMembersAsync();
        }

        [RelayCommand]
        private async Task InviteUsersAsync()
        {
            await Shell.Current.GoToAsync($"InviteUsersPage?channelId={ChannelId}");
        }

        [RelayCommand]
        private async Task ShowMemberMenuAsync(ChannelMemberDto member)
        {
            if (!CanManageMembers) return;

            // Нельзя управлять владельцем (если ты не владелец)
            var currentMember = Members.FirstOrDefault(m => m.UserId == CurrentUserId);
            if (member.ChannelRole == ChannelRole.Owner && currentMember?.ChannelRole != ChannelRole.Owner)
            {
                await Shell.Current.DisplayAlert("Информация", "Вы не можете управлять владельцем канала", "OK");
                return;
            }

            // Нельзя управлять самим собой (кроме выхода, но это отдельно)
            if (member.UserId == CurrentUserId)
            {
                await Shell.Current.DisplayAlert("Информация", "Используйте кнопку \"Покинуть канал\" в списке каналов", "OK");
                return;
            }

            // Нельзя управлять админами (если ты не владелец)
            if (member.ChannelRole == ChannelRole.Admin && currentMember?.ChannelRole != ChannelRole.Owner)
            {
                await Shell.Current.DisplayAlert("Информация", "Вы не можете управлять другими администраторами", "OK");
                return;
            }

            var actions = new List<string>();

            // Доступные действия в зависимости от роли
            if (currentMember?.ChannelRole == ChannelRole.Owner)
            {
                // Владелец может всё
                if (member.ChannelRole != ChannelRole.Owner)
                {
                    actions.Add("Назначить владельцем");
                }
                if (member.ChannelRole != ChannelRole.Admin)
                {
                    actions.Add("Назначить администратором");
                }
                if (member.ChannelRole != ChannelRole.Moderator && member.ChannelRole != ChannelRole.Admin)
                {
                    actions.Add("Назначить модератором");
                }
                if (member.ChannelRole != ChannelRole.Member && member.ChannelRole != ChannelRole.Owner)
                {
                    actions.Add("Понизить до участника");
                }
                actions.Add("Удалить из канала");
            }
            else if (currentMember?.ChannelRole == ChannelRole.Admin)
            {
                // Админ не может управлять владельцем и другими админами
                if (member.ChannelRole != ChannelRole.Owner && member.ChannelRole != ChannelRole.Admin)
                {
                    if (member.ChannelRole != ChannelRole.Moderator)
                    {
                        actions.Add("Назначить модератором");
                    }
                    if (member.ChannelRole != ChannelRole.Member)
                    {
                        actions.Add("Понизить до участника");
                    }
                    actions.Add("Удалить из канала");
                }
            }

            if (!actions.Any())
            {
                await Shell.Current.DisplayAlert("Информация", "Нет доступных действий", "OK");
                return;
            }

            var action = await Shell.Current.DisplayActionSheet(
                $"Управление: {member.FullName}",
                "Отмена",
                null,
                actions.ToArray());

            if (string.IsNullOrEmpty(action) || action == "Отмена")
                return;

            await ProcessMemberActionAsync(member, action);
        }

        private async Task ProcessMemberActionAsync(ChannelMemberDto member, string action)
        {
            try
            {
                ChannelRole? newRole = null;

                switch (action)
                {
                    case "Назначить владельцем":
                        newRole = ChannelRole.Owner;
                        break;
                    case "Назначить администратором":
                        newRole = ChannelRole.Admin;
                        break;
                    case "Назначить модератором":
                        newRole = ChannelRole.Moderator;
                        break;
                    case "Понизить до участника":
                        newRole = ChannelRole.Member;
                        break;
                    case "Удалить из канала":
                        await RemoveMemberAsync(member);
                        return;
                }

                if (newRole.HasValue)
                {
                    await UpdateMemberRoleAsync(member, newRole.Value);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task UpdateMemberRoleAsync(ChannelMemberDto member, ChannelRole newRole)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Изменение роли",
                $"Вы уверены, что хотите изменить роль пользователя {member.FullName} на \"{GetRoleName(newRole)}\"?",
                "Да", "Нет");

            if (!confirm) return;

            IsMembersLoading = true;

            try
            {
                var response = await _channelApiService.UpdateMemberRoleAsync(
                    Guid.Parse(ChannelId),
                    member.UserId,
                    newRole);

                if (response?.Success == true)
                {
                    member.ChannelRole = newRole;
                    member.ChannelRoleText = GetRoleName(newRole);

                    await Shell.Current.DisplayAlert("Успех", "Роль участника обновлена", "OK");
                    await LoadMembersAsync(); // Перезагружаем список
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось обновить роль", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsMembersLoading = false;
            }
        }

        private async Task RemoveMemberAsync(ChannelMemberDto member)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Удаление участника",
                $"Вы уверены, что хотите удалить пользователя {member.FullName} из канала?",
                "Да", "Нет");

            if (!confirm) return;

            IsMembersLoading = true;

            try
            {
                var response = await _channelApiService.RemoveMemberAsync(
                    Guid.Parse(ChannelId),
                    member.UserId);

                if (response?.Success == true)
                {
                    Members.Remove(member);
                    FilterMembers();
                    MembersCount = Members.Count;

                    await Shell.Current.DisplayAlert("Успех", "Участник удален из канала", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", response?.Message ?? "Не удалось удалить участника", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                IsMembersLoading = false;
            }
        }

        private string GetRoleName(ChannelRole role)
        {
            return role switch
            {
                ChannelRole.Owner => "Владелец",
                ChannelRole.Admin => "Администратор",
                ChannelRole.Moderator => "Модератор",
                ChannelRole.Member => "Участник",
                _ => "Неизвестно"
            };
        }
    }
}
