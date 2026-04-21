using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Services.Api
{
    public interface IProfileApiService
    {
        Task<Profile> GetCurrentUserAsync();
        Task<ProfileUpdateResponse> UpdateProfileAsync(string firstName, string lastName, string email, string phoneNumber);
        Task<Response<List<AvatarPreset>>?> GetAllAvatarsAsync();
        Task<ApiResponse?> UploadAvatarAsync(string presetKey);
        Task<ApiResponse?> UploadCustomAvatarAsync(FileResult Image);
    }

    public class ProfileApiService : BaseApiService, IProfileApiService
    {
        public ProfileApiService(HttpClient httpClient, LoginService loginService)
            : base(httpClient, loginService)
        {
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
                var response = await HttpClient.GetAsync("users/profile");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = DeserializeResponse<User>(content);

                    return user == null
                        ? new Profile { Message = "Ошибка десериализации", Success = false, Code = 500 }
                        : new Profile { Message = "Успешное получение пользователя", Success = true, Code = 200, User = user };
                }

                return response.StatusCode switch
                {
                    HttpStatusCode.NotFound => new Profile { Message = "Не удалось найти пользователя", Success = false, Code = 404 },
                    _ => new Profile { Message = $"Ошибка сервера: {response.StatusCode}", Success = false, Code = (int)response.StatusCode }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get user error: {ex.Message}");
                return profileError;
            }
        }

        public async Task<ProfileUpdateResponse> UpdateProfileAsync(string firstName, string lastName, string email, string phoneNumber)
        {
            try
            {
                var updateData = new { firstName, lastName, email, phoneNumber };
                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PutAsync("users/profile", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return DeserializeResponse<ProfileUpdateResponse>(responseContent)
                       ?? new ProfileUpdateResponse { Success = false, Message = "Что-то пошло не так" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update profile error: {ex.Message}");
                return new ProfileUpdateResponse { Success = false, Message = "Что-то пошло не так" };
            }
        }

        public async Task<Response<List<AvatarPreset>>?> GetAllAvatarsAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync("profile/avatars");
                var content = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<Response<List<AvatarPreset>>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get all avatars error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> UploadAvatarAsync(string presetKey)
        {
            try
            {
                var request = new { avatarPresetKey = presetKey };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PutAsync("profile/uploadAvatar", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload avatar error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResponse?> UploadCustomAvatarAsync(FileResult image)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var imageContent = new StreamContent(await image.OpenReadAsync());
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                content.Add(imageContent, "Image", image.FileName);

                var response = await HttpClient.PutAsync("profile/uploadCustomAvatar", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return DeserializeResponse<ApiResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload custom avatar error: {ex.Message}");
                return null;
            }
        }
    }
}
