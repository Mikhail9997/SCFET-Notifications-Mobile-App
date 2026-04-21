using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services.Api
{
    public interface IUsersApiService
    {
        Task<List<Group>> GetGroupsAsync(GroupFilter filter);
        Task<List<User>> GetStudentsAsync(UserFilter filter);
        Task<List<User>> GetParentsAsync(UserFilter filter);
        Task<List<User>> GetTeachersAsync(UserFilter filter);
        Task<List<User>> GetAdministratorsAsync(UserFilter filter);
    }

    public class UsersApiService : BaseApiService, IUsersApiService
    {
        public UsersApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
        }

        private Dictionary<string, string?> BuildUserFilterQuery(UserFilter filter)
        {
            return new Dictionary<string, string?>
        {
            { "firstName", filter?.FirstName },
            { "lastName", filter?.LastName },
            { "email", filter?.Email },
            { "phoneNumber", filter?.PhoneNumber },
            { "groupId", filter?.GroupId?.ToString() },
            { "isActive", "true" }
        };
        }

        private async Task<List<T>> GetUsersAsync<T>(string endpoint, UserFilter filter) where T : class
        {
            try
            {
                var query = BuildUserFilterQuery(filter);
                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"{endpoint}?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return DeserializeResponse<List<T>>(content) ?? new List<T>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get {typeof(T).Name} error: {ex.Message}");
            }

            return new List<T>();
        }

        public async Task<List<Group>> GetGroupsAsync(GroupFilter filter)
        {
            try
            {
                var query = new Dictionary<string, string?>
            {
                { "name", filter?.Name }
            };

                var queryString = BuildQueryString(query);
                var response = await HttpClient.GetAsync($"groups?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return DeserializeResponse<List<Group>>(content) ?? new List<Group>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get groups error: {ex.Message}");
            }

            return new List<Group>();
        }

        public async Task<List<User>> GetStudentsAsync(UserFilter filter)
            => await GetUsersAsync<User>("users/students", filter);

        public async Task<List<User>> GetParentsAsync(UserFilter filter)
            => await GetUsersAsync<User>("users/parents", filter);

        public async Task<List<User>> GetTeachersAsync(UserFilter filter)
            => await GetUsersAsync<User>("users/teachers", filter);

        public async Task<List<User>> GetAdministratorsAsync(UserFilter filter)
            => await GetUsersAsync<User>("users/administrators", filter);
    }

}
