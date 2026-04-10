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
        protected const string BaseUrl = "http://81.94.159.27:5050/api";

        protected BaseApiService(LoginService loginService, ITokenService tokenService)
        {
            LoginService = loginService;

            var handler = new AuthHandler(tokenService, loginService)
            {
                InnerHandler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                }
            };

            HttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl)
            };
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
    }
}
