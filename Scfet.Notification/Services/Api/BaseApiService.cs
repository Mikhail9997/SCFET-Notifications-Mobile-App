using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scfet.Notification.Handlers;
using Scfet.Notification.Utils;

namespace Scfet.Notification.Services.Api
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient HttpClient;
        protected readonly LoginService LoginService;

        protected BaseApiService(HttpClient httpClient, LoginService loginService)
        {
            HttpClient = httpClient;
            LoginService = loginService;
        }

        protected async Task AddAuthHeader()
        {
            if (await SecureStorage.GetAsync("access_token") != null)
            {
                var token = await SecureStorage.GetAsync("access_token");
                HttpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        protected string BuildQueryString(Dictionary<string, string?> parameters)
        {
            return ResponseUtils.GenerateQuery(parameters);
        }

        protected T? DeserializeResponse<T>(string content)
        {
            if (string.IsNullOrEmpty(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        protected async Task<T?> GetAsync<T>(string url)
        {
            var response = await HttpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return DeserializeResponse<T>(content);
        }

        protected async Task<T?> PostAsync<T>(string url, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return DeserializeResponse<T>(responseContent);
        }

        protected async Task<T?> PutAsync<T>(string url, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await HttpClient.PutAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return DeserializeResponse<T>(responseContent);
        }

        protected async Task<bool> DeleteAsync(string url)
        {
            var response = await HttpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}
