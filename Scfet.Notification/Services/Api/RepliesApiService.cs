using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services.Api
{
    public interface IRepliesApiService
    {
        Task<Response<PagedResult<Reply>>?> GetNotificationRepliesAsync(Guid notificationId, Filter filter);
        Task<ApiResponse?> CreateReplyAsync(CreateReply request);
        Task<ApiResponse?> UpdateReplyAsync(Guid id, UpdateReply request);
        Task<ApiResponse?> RemoveReplyAsync(Guid id);
    }

    public class RepliesApiService : BaseApiService, IRepliesApiService
    {
        public RepliesApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
        }

        public async Task<Response<PagedResult<Reply>>?> GetNotificationRepliesAsync(Guid notificationId, Filter filter)
        {
            try
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

                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"notificationreplies/notification/{notificationId}/replies?{queryString}");
                var content = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<Response<PagedResult<Reply>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get notification replies error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> CreateReplyAsync(CreateReply request)
        {
            return await PostAsync<ApiResponse>("notificationreplies", request);
        }

        public async Task<ApiResponse?> UpdateReplyAsync(Guid id, UpdateReply request)
        {
            return await PutAsync<ApiResponse>($"notificationreplies/{id}/update", request);
        }

        public async Task<ApiResponse?> RemoveReplyAsync(Guid id)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"notificationreplies/{id}/remove");
                var responseContent = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Remove reply error: {ex.Message}");
                return null;
            }
        }
    }
}
