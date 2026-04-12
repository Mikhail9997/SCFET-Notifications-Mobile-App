using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services.Api
{
    public interface IFavoritesApiService
    {
        Task<Response<PagedResult<Favorite>>?> GetMyFavoritesAsync(Filter filter);
        Task<ApiResponse?> AddFavoriteAsync(AddFavorite request);
        Task<ApiResponse?> RemoveFavoriteAsync(Guid notificationId);
    }

    public class FavoritesApiService : BaseApiService, IFavoritesApiService
    {
        public FavoritesApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
        }

        public async Task<Response<PagedResult<Favorite>>?> GetMyFavoritesAsync(Filter filter)
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
                var response = await HttpClient.GetAsync($"favorites/my?{queryString}");
                var content = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<Response<PagedResult<Favorite>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get favorites error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> AddFavoriteAsync(AddFavorite request)
        {
            return await PostAsync<ApiResponse>("favorites/add", request);
        }

        public async Task<ApiResponse?> RemoveFavoriteAsync(Guid notificationId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"favorites/{notificationId}/remove");
                var responseContent = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Remove favorite error: {ex.Message}");
                return null;
            }
        }
    }
}
