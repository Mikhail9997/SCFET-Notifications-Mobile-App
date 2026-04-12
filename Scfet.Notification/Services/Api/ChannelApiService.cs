using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Services.Api
{
    public interface IChannelApiService
    {
        // Каналы
        Task<ApiResponse<ChannelDto>?> CreateChannelAsync(CreateChannelRequest request);
        Task<ApiResponse<List<ChannelDto>>?> GetMyChannelsAsync(ChannelFilter filter);
        Task<ApiResponse<List<ChannelDto>>?> GetAllChannelsAsync(ChannelFilter filter);
        Task<ApiResponse<ChannelDto>?> GetChannelByIdAsync(Guid channelId);
        Task<ApiResponse<List<ChannelMemberDto>>?> GetChannelMembersAsync(Guid channelId);
        Task<ApiResponse?> UpdateMemberRoleAsync(Guid channelId, Guid userId, ChannelRole newRole);
        Task<ApiResponse?> RemoveMemberAsync(Guid channelId, Guid userId);
        Task<ApiResponse?> LeaveChannelAsync(Guid channelId);

        // Приглашения
        Task<ApiResponse<List<ChannelInvitationDto>>?> GetMyInvitationsAsync(ChannelFilter filter);
        Task<ApiResponse<List<ChannelInvitationDto>>?> GetSentInvitationsAsync(ChannelFilter filter);
        Task<ApiResponse?> InviteUsersAsync(Guid channelId, InviteUsersRequest request);
        Task<ApiResponse?> AcceptInvitationAsync(Guid invitationId);
        Task<ApiResponse?> DeclineInvitationAsync(Guid invitationId);
        Task<ApiResponse?> CancelInvitationAsync(Guid invitationId);
        Task<ApiResponse?> DeleteInvitationAsync(Guid invitationId);

        // Доступные пользователи
        Task<ApiResponse<List<AvailableUserDto>>?> GetAvailableUsersAsync(Guid channelId, AvailableUsersFilter filter);
    }

    public class ChannelApiService : BaseApiService, IChannelApiService
    {
        public ChannelApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
        }

        #region Каналы

        public async Task<ApiResponse<ChannelDto>?> CreateChannelAsync(CreateChannelRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync("channels/create", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<ChannelDto>>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create channel error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<List<ChannelDto>>?> GetMyChannelsAsync(ChannelFilter filter)
        {
            try
            {
                var query = BuildChannelFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"channels/my-channels?{queryString}");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<List<ChannelDto>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get my channels error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<List<ChannelDto>>?> GetAllChannelsAsync(ChannelFilter filter)
        {
            try
            {
                var query = BuildChannelFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"channels/all?{queryString}");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<List<ChannelDto>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get all channels error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<ChannelDto>?> GetChannelByIdAsync(Guid channelId)
        {
            try
            {
                var response = await HttpClient.GetAsync($"channels/{channelId}");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<ChannelDto>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get channel by id error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<List<ChannelMemberDto>>?> GetChannelMembersAsync(Guid channelId)
        {
            try
            {
                var response = await HttpClient.GetAsync($"channels/{channelId}/members");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<List<ChannelMemberDto>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get channel members error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> UpdateMemberRoleAsync(Guid channelId, Guid userId, ChannelRole newRole)
        {
            try
            {
                var request = new { NewRole = newRole };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PutAsync($"channels/{channelId}/members/{userId}/role", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update member role error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> RemoveMemberAsync(Guid channelId, Guid userId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"channels/{channelId}/members/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Remove member error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> LeaveChannelAsync(Guid channelId)
        {
            try
            {
                var response = await HttpClient.PostAsync($"channels/{channelId}/leave", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Leave channel error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Приглашения

        public async Task<ApiResponse<List<ChannelInvitationDto>>?> GetMyInvitationsAsync(ChannelFilter filter)
        {
            try
            {
                var query = BuildChannelFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"channels/invitations?{queryString}");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<List<ChannelInvitationDto>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get my invitations error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<List<ChannelInvitationDto>>?> GetSentInvitationsAsync(ChannelFilter filter)
        {
            try
            {
                var query = BuildChannelFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"channels/sent-invitations?{queryString}");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<List<ChannelInvitationDto>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get sent invitations error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> InviteUsersAsync(Guid channelId, InviteUsersRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync($"channels/{channelId}/invite", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invite users error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> AcceptInvitationAsync(Guid invitationId)
        {
            try
            {
                var response = await HttpClient.PostAsync($"channels/invitations/{invitationId}/accept", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Accept invitation error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> DeclineInvitationAsync(Guid invitationId)
        {
            try
            {
                var response = await HttpClient.PostAsync($"channels/invitations/{invitationId}/decline", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decline invitation error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> CancelInvitationAsync(Guid invitationId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"channels/invitations/{invitationId}/cancel");
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel invitation error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> DeleteInvitationAsync(Guid invitationId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"channels/invitations/{invitationId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete invitation error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Доступные пользователи

        public async Task<ApiResponse<List<AvailableUserDto>>?> GetAvailableUsersAsync(Guid channelId, AvailableUsersFilter filter)
        {
            try
            {
                var query = new Dictionary<string, string?>
            {
                { "page", filter.Page.ToString() },
                { "pageSize", filter.PageSize.ToString() },
                { "searchTerm", filter.SearchTerm }
            };

                if (filter.Role.HasValue)
                    query.Add("role", filter.Role.Value.ToString());

                if (filter.GroupId.HasValue)
                    query.Add("groupId", filter.GroupId.Value.ToString());

                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"channels/{channelId}/available-users?{queryString}");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse<List<AvailableUserDto>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get available users error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Вспомогательные методы

        private Dictionary<string, string?> BuildChannelFilterQuery(ChannelFilter filter)
        {
            var query = new Dictionary<string, string?>
        {
            { "page", filter.Page.ToString() },
            { "pageSize", filter.PageSize.ToString() },
            { "sortBy", filter.SortBy.ToString() },
            { "sortOrder", filter.SortOrder.ToString() },
            { "searchTerm", filter.SearchTerm }
        };

            return query;
        }

        #endregion
    }
}
