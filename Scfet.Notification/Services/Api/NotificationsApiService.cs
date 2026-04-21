using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services.Api
{
    public interface INotificationsApiService
    {
        Task<PagedResult<Models.Notification>?> GetNotificationsAsync(Filter filter);
        Task<NotificationDetail?> GetNotificationById(Guid id);
        Task<NotificationDetail?> GetNotificationDetail(Guid id);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task<bool> SendNotificationAsync(CreateNotification request);
        Task<bool> UpdateNotificationAsync(UpdateNotification request);
        Task<bool> RemoveNotificationAsync(Guid id);
        Task<PagedResult<SentNotification>?> GetSentNotificationsAsync(Filter filter);
    }

    public class NotificationsApiService : BaseApiService, INotificationsApiService
    {
        public NotificationsApiService(HttpClient httpClient, LoginService loginService) 
            : base(httpClient, loginService)
        {
        }

        private Dictionary<string, string?> BuildFilterQuery(Filter filter)
        {
            var query = new Dictionary<string, string?>
        {
            { "page", filter.Page.ToString() },
            { "PageSize", filter.PageSize.ToString() },
            { "SortOrder", filter.SortOrder.ToString() },
            { "SortBy", filter.SortBy.ToString() }
        };

            if (filter.StartDate.HasValue)
                query.Add("startDate", filter.StartDate.Value.ToString("yyyy-MM-dd"));

            if (filter.EndDate.HasValue)
                query.Add("endDate", filter.EndDate.Value.ToString("yyyy-MM-dd"));

            return query;
        }

        public async Task<PagedResult<Models.Notification>?> GetNotificationsAsync(Filter filter)
        {
            try
            {
                var query = BuildFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"notifications/my?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return DeserializeResponse<PagedResult<Models.Notification>>(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get notifications error: {ex.Message}");
            }

            return null;
        }

        public async Task<NotificationDetail?> GetNotificationById(Guid id)
        {
            try
            {
                var response = await HttpClient.GetAsync($"notifications/{id}");
                var content = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<NotificationDetail>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get notification error: {ex.Message}");
                return null;
            }
        }

        public async Task<NotificationDetail?> GetNotificationDetail(Guid id)
        {
            return await GetNotificationById(id);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId)
        {
            try
            {
                var response = await HttpClient.PutAsync($"notifications/{notificationId}/mark-as-read", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mark as read error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationAsync(CreateNotification request)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(request.Title), "Title");
                content.Add(new StringContent(request.Message), "Message");
                content.Add(new StringContent(request.AllowReplies.ToString()), "AllowReplies");
                content.Add(new StringContent(request.Type.ToString()), "Type");

                if (request.TargetUserIds?.Any() == true)
                {
                    foreach (var userId in request.TargetUserIds)
                        content.Add(new StringContent(userId.ToString()), "TargetUserIds");
                }

                if (request.TargetGroupId.HasValue)
                    content.Add(new StringContent(request.TargetGroupId.Value.ToString()), "TargetGroupId");

                if (request.Image != null)
                {
                    var imageContent = new StreamContent(await request.Image.OpenReadAsync());
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(request.Image.ContentType);
                    content.Add(imageContent, "Image", request.Image.FileName);
                }

                var response = await HttpClient.PostAsync("notifications", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send notification error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateNotificationAsync(UpdateNotification request)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(request.Title), "Title");
                content.Add(new StringContent(request.Message), "Message");
                content.Add(new StringContent(request.AllowReplies.ToString()), "AllowReplies");
                content.Add(new StringContent(request.Type.ToString()), "Type");

                if (request.TargetUserIds?.Any() == true)
                {
                    foreach (var userId in request.TargetUserIds)
                        content.Add(new StringContent(userId.ToString()), "TargetUserIds");
                }

                if (request.TargetGroupId.HasValue)
                    content.Add(new StringContent(request.TargetGroupId.Value.ToString()), "TargetGroupId");

                if (request.Image != null)
                {
                    var imageContent = new StreamContent(await request.Image.OpenReadAsync());
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(request.Image.ContentType);
                    content.Add(imageContent, "Image", request.Image.FileName);
                }

                var response = await HttpClient.PutAsync($"notifications/{request.NotificationId}/update", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update notification error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveNotificationAsync(Guid id)
        {
            return await DeleteAsync($"notifications/{id}/remove");
        }

        public async Task<PagedResult<SentNotification>?> GetSentNotificationsAsync(Filter filter)
        {
            try
            {
                var query = BuildFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"notifications/sent?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return DeserializeResponse<PagedResult<SentNotification>>(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get sent notifications error: {ex.Message}");
            }

            return null;
        }
    }
}
