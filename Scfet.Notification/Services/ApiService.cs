using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Handlers;
using Scfet.Notification.Models;
using Scfet.Notification.Utils;

namespace Scfet.Notification.Services
{
    public interface IApiService
    {
        Task<AuthResponse<User>> LoginAsync(string email, string password);
        Task Logout();
        Task<Profile> GetCurrentUserAsync();
        Task<GetNotification<Models.Notification>?> GetNotificationsAsync(NotificationFilter filter);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task<List<Group>> GetGroupsAsync(GroupFilter filter);
        Task<List<User>> GetStudentsAsync(UserFilter filter);
        Task<List<User>> GetTeachersAsync(UserFilter filter);
        Task<List<User>> GetAdministratorsAsync(UserFilter filter);
        Task<GetNotification<SentNotification>?> GetSentNotificationsAsync(NotificationFilter filter);
        Task<bool> SendNotificationAsync(CreateNotification notification);
        Task<bool> RemoveNotificationAsync(Guid id);
        Task<bool> UpdateProfileAsync(string firstName, string lastName, string email);
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    }
    //http://localhost:5050/api
    //https://amorously-preeminent-godwit.cloudpub.ru/api
    public class ApiService:IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly LoginService _loginService;
        private const string BaseUrl = "https://amorously-preeminent-godwit.cloudpub.ru/api";

        public ApiService(LoginService loginService)
        {
            var handler = new AuthHandler
            {
                InnerHandler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                }
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl)
            };

            _loginService = loginService;
        }

        private void AddAuthHeader()
        {
            if (Preferences.ContainsKey("auth_token"))
            {
                var token = Preferences.Get("auth_token", string.Empty);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<AuthResponse<User>> LoginAsync(string email, string password)
        {
            var authError = new AuthResponse<User>()
            {
                Message = "Произошла неизвестная ошибка",
                Success = false
            };
            try
            {
                var loginData = new { email, password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponse<User>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authResponse != null && authResponse.Data != null)
                    {
                        var data = authResponse.Data;
                        var auth = new Auth
                        {
                            Token = data.Token,
                            UserId = data.UserId.ToString(),
                            Email = data.Email,
                            FullName = data.FullName,
                            Role = data.Role
                        };
                        _loginService.Login(auth);

                        return authResponse;
                    }
                    return JsonSerializer.Deserialize<AuthResponse<User>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? authError;
                }
                return authError;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return authError;
            }
        }

        public async Task Logout()
        {
            await _loginService.Logout();

            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<Profile> GetCurrentUserAsync()
        {
            var profileError = new Profile
            {
                Message = "Произошла неизвестная ошибка",
                Success = false,
                Code = 503
            };
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/users/profile");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    User? user = JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? null;
                    if (user == null)
                    {
                        profileError.Code = 500;
                        return profileError;
                    }
                    return new Profile
                    {
                        Message = "Успешное получение пользователя",
                        Success = true,
                        Code = 200,
                        User = user 
                    };
                }
                else if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new Profile
                    {
                        Message = "Не удалось найти пользователя",
                        Success = false,
                        Code = 404
                    };
                }
                profileError.Code = (int)response.StatusCode;
                return profileError;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Get user error: {ex.Message}");
            }

            return profileError;
        }

        public async Task<GetNotification<Models.Notification>?> GetNotificationsAsync(NotificationFilter filter)
        {
            try
            {
                AddAuthHeader();
                var query = new Dictionary<string, string?>(){
                    { "page", filter.Page.ToString() },
                    { "PageSize", filter.PageSize.ToString() },
                    { "SortOrder", filter.SortOrder.ToString() },
                    { "SortBy", filter.SortBy.ToString() }
                };

                if (filter.StartDate.HasValue)
                {
                    query.Add("startDate", filter.StartDate.Value.ToString("yyyy-MM-dd"));
                }

                if (filter.EndDate.HasValue)
                {
                    query.Add("endDate", filter.EndDate.Value.ToString("yyyy-MM-dd"));
                }

                var queryString = ResponseUtils.GenerateQuery(query);

                var response = await _httpClient.GetAsync($"{BaseUrl}/notifications/my?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<GetNotification<Models.Notification>?>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get notifications error: {ex.Message}");
            }

            return null;
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient
                    .PutAsync($"{BaseUrl}/notifications/{notificationId}/mark-as-read", null);


                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Mark as read error: {ex.Message}");
                return false;

            }
        }

        public async Task<List<Group>> GetGroupsAsync(GroupFilter filter)
        {
            try
            {
                AddAuthHeader();
                var query = new Dictionary<string, string?>
                {
                    { "name", filter?.Name }
                };

                var queryString = ResponseUtils.GenerateQuery(query);

                var response = await _httpClient.GetAsync($"{BaseUrl}/groups?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Group>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Group>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get groups error: {ex.Message}");
            }

            return new List<Group>();
        }

        public async Task<List<User>> GetStudentsAsync(UserFilter filter)
        {
            try
            {
                AddAuthHeader();
                var query = new Dictionary<string, string?>
                {
                    { "firstName", filter?.FirstName },
                    { "lastName", filter?.LastName },
                    { "email", filter?.Email },
                    { "groupId", filter?.GroupId?.ToString() },
                    { "isActive", true.ToString() }
                };

                var queryString = ResponseUtils.GenerateQuery(query);

                var response = await _httpClient.GetAsync($"{BaseUrl}/users/students?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<User>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get students error: {ex.Message}");
            }

            return new List<User>();
        }

        public async Task<List<User>> GetTeachersAsync(UserFilter filter)
        {
            try
            {
                AddAuthHeader();

                var query = new Dictionary<string, string?>
                {
                    { "firstName", filter?.FirstName },
                    { "lastName", filter?.LastName },
                    { "email", filter?.Email },
                    { "groupId", filter?.GroupId?.ToString() },
                    { "isActive", true.ToString() }
                };

                var queryString = ResponseUtils.GenerateQuery(query);

                var response = await _httpClient.GetAsync($"{BaseUrl}/users/teachers?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?? new List<User>();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Get teachers error: {ex.Message}");
            }
            return new List<User>();
        }

        public async Task<List<User>> GetAdministratorsAsync(UserFilter filter)
        {
            try
            {
                AddAuthHeader();

                var query = new Dictionary<string, string?>
                {
                    { "firstName", filter?.FirstName },
                    { "lastName", filter?.LastName },
                    { "email", filter?.Email },
                    { "groupId", filter?.GroupId?.ToString() },
                    { "isActive", true.ToString() }
                };

                var queryString = ResponseUtils.GenerateQuery(query);

                var response = await _httpClient.GetAsync($"{BaseUrl}/users/administrators?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?? new List<User>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get administrators error: {ex.Message}");
            }
            return new List<User>();
        }

        public async Task<bool> SendNotificationAsync(CreateNotification request)
        {
            try
            {
                AddAuthHeader();
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(request.Title), "Title");
                content.Add(new StringContent(request.Message), "Message");
                content.Add(new StringContent(request.Type.ToString()), "Type");
                
                if(request.TargetUserIds != null && request.TargetUserIds.Any())
                {
                    foreach (var userId in request.TargetUserIds)
                    {
                        content.Add(new StringContent(userId.ToString()), "TargetUserIds");
                    }
                }

                if (request.TargetGroupId.HasValue)
                {
                    content.Add(new StringContent(request.TargetGroupId.Value.ToString()), "TargetGroupId");
                }

                if (request.Image != null)
                {
                    var imageContent = new StreamContent(await request.Image.OpenReadAsync());
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.Image.ContentType);
                    content.Add(imageContent, "Image", request.Image.FileName);
                }
                var response = await _httpClient.PostAsync($"{BaseUrl}/notifications", content);
                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Send notification error: {ex.Message}");
                return false;
            }
        }

        public async Task<GetNotification<SentNotification>?> GetSentNotificationsAsync(NotificationFilter filter)
        {
            try
            {
                AddAuthHeader();

                var query = new Dictionary<string, string?>(){
                    { "page", filter.Page.ToString() },
                    { "PageSize", filter.PageSize.ToString() },
                    { "SortOrder", filter.SortOrder.ToString() },
                    { "SortBy", filter.SortBy.ToString() }
                };

                if (filter.StartDate.HasValue)
                {
                    query.Add("startDate", filter.StartDate.Value.ToString("yyyy-MM-dd"));
                }

                if (filter.EndDate.HasValue)
                {
                    query.Add("endDate", filter.EndDate.Value.ToString("yyyy-MM-dd"));
                }

                var queryString = ResponseUtils.GenerateQuery(query);

                var response = await _httpClient.GetAsync($"{BaseUrl}/notifications/sent?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonSerializer.Deserialize<GetNotification<SentNotification>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get sent notifications error: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> RemoveNotificationAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/notifications/{id}/remove");

                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Remove notification error: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> UpdateProfileAsync(string firstName, string lastName, string email)
        {
            try
            {
                AddAuthHeader();
                var updateData = new { firstName, lastName, email };
                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/users/profile", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update profile error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                AddAuthHeader();
                var passwordData = new
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    ConfirmNewPassword = newPassword
                };

                var json = JsonSerializer.Serialize(passwordData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient
                    .PostAsync($"{BaseUrl}/auth/change-password", content);

                return response.IsSuccessStatusCode;

            }
            catch(Exception ex)
            {
                Console.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }
    }
}
