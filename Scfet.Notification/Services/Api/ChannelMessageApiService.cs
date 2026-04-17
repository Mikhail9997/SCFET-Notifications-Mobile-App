using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Models;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Services.Api
{
    public interface IChannelMessageApiService
    {
        // Получение сообщений
        Task<ApiResponse<List<ChannelMessageDto>>?> GetMessagesAsync(Guid channelId, MessageFilter filter);

        // Отправка сообщения
        Task<ApiResponse<ChannelMessageDto>?> SendMessageAsync(Guid channelId, SendMessageRequest request);

        // Обновление сообщения
        Task<ApiResponse<ChannelMessageDto>?> UpdateMessageAsync(Guid channelId, Guid messageId, UpdateMessageRequest request);

        // Удаление сообщения
        Task<ApiResponse?> DeleteMessageAsync(Guid channelId, Guid messageId);

        // Отметка о прочтении
        Task<ApiResponse?> MarkAsReadAsync(Guid channelId, Guid messageId);
        Task<ApiResponse?> MarkAllAsReadAsync(Guid channelId);

        // Получение количества непрочитанных
        Task<UnreadCountResponse?> GetUnreadCountAsync(Guid channelId);
    }
    public class ChannelMessageApiService : BaseApiService, IChannelMessageApiService
    {
        private readonly LoginService _loginService;
        public ChannelMessageApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
            _loginService = loginService;
        }

        public async Task<ApiResponse<List<ChannelMessageDto>>?> GetMessagesAsync(Guid channelId, MessageFilter filter)
        {
            try
            {
                var query = new Dictionary<string, string?>
                {
                    { "page", filter.Page.ToString() },
                    { "pageSize", filter.PageSize.ToString() },
                    { "sortOrder", filter.SortOrder.ToString() },
                    { "searchTerm", filter.SearchTerm }
                };

                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"channels/{channelId}/messages?{queryString}");
                var content = await response.Content.ReadAsStringAsync();

                var result = DeserializeResponse<ApiResponse<List<ChannelMessageDto>>>(content);

                // Устанавливаем флаг IsOwnMessage для текущего пользователя
                if (result?.Data != null)
                {
                    var currentUserId = GetCurrentUserIdAsync();
                    foreach (var message in result.Data)
                    {
                        message.IsOwnMessage = message.SenderId == currentUserId;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get messages error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<ChannelMessageDto>?> SendMessageAsync(Guid channelId, SendMessageRequest request)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(request.Content), "Content");

                if (request.ReplyToMessageId.HasValue)
                {
                    content.Add(new StringContent(request.ReplyToMessageId.Value.ToString()), "ReplyToMessageId");
                }

                if (request.Image != null)
                {
                    var imageContent = new StreamContent(await request.Image.OpenReadAsync());
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(request.Image.ContentType);
                    content.Add(imageContent, "Image", request.Image.FileName);
                }

                var response = await HttpClient.PostAsync($"channels/{channelId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = DeserializeResponse<ApiResponse<ChannelMessageDto>>(responseContent);

                // Устанавливаем флаг для собственного сообщения
                if (result?.Data != null)
                {
                    var currentUserId = GetCurrentUserIdAsync();
                    result.Data.IsOwnMessage = result.Data.SenderId == currentUserId;
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send message error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse<ChannelMessageDto>?> UpdateMessageAsync(Guid channelId, Guid messageId, UpdateMessageRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PutAsync($"channels/{channelId}/messages/{messageId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = DeserializeResponse<ApiResponse<ChannelMessageDto>>(responseContent);

                // Устанавливаем флаг для собственного сообщения
                if (result?.Data != null)
                {
                    var currentUserId = GetCurrentUserIdAsync();
                    result.Data.IsOwnMessage = result.Data.SenderId == currentUserId;
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update message error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> DeleteMessageAsync(Guid channelId, Guid messageId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"channels/{channelId}/messages/{messageId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete message error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> MarkAsReadAsync(Guid channelId, Guid messageId)
        {
            try
            {
                var response = await HttpClient.PostAsync($"channels/{channelId}/messages/{messageId}/read", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mark as read error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> MarkAllAsReadAsync(Guid channelId)
        {
            try
            {
                var response = await HttpClient.PostAsync($"channels/{channelId}/messages/read-all", null);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mark all as read error: {ex.Message}");
                return null;
            }
        }

        public async Task<UnreadCountResponse?> GetUnreadCountAsync(Guid channelId)
        {
            try
            {
                var response = await HttpClient.GetAsync($"channels/{channelId}/messages/unread-count");
                var content = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<UnreadCountResponse>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get unread count error: {ex.Message}");
                return null;
            }
        }

        private Guid? GetCurrentUserIdAsync()
        {
            try
            {
                var auth = _loginService.GetCurrentAuth();
                if (Guid.TryParse(auth?.UserId, out Guid userId))
                    return userId;
            }
            catch
            {
            }
            return null;
        }
    }
}
