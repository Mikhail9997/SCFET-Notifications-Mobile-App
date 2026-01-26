using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Scfet.Notification.Models;
using Scfet.Notification.Services;

namespace Scfet.Notification.Handlers
{
    public class AuthHandler: DelegatingHandler
    {
        private readonly ITokenService _tokenService;
        private const string BaseUrl = "https://amorously-preeminent-godwit.cloudpub.ru/api";

        private readonly HashSet<string> _excludedPaths = new()
        {
            "/api/auth/login",
            "/api/auth/refresh-token",
            "/api/auth/register",
            "/api/auth/register-employee",
            "/api/auth/check-email-exist"
        };

        public AuthHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Проверяем, не исключен ли путь из проверки авторизации
            var requestUrl = request.RequestUri?.AbsolutePath ?? "";
            if (IsExcludedPath(requestUrl))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Получаем валидный токен
            var token = await _tokenService.GetValidAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Если получили 401, пробуем обновить токен
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedResponseAsync(request, response, cancellationToken);
            }

            return response;
        }

        private async Task<HttpResponseMessage> HandleUnauthorizedResponseAsync(
            HttpRequestMessage request,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            // Пытаемся обновить токен
            var refreshed = await _tokenService.RefreshTokenAsync();

            if (refreshed)
            {
                // Получаем новый токен
                var newToken = await SecureStorage.GetAsync("access_token");
                if (!string.IsNullOrEmpty(newToken))
                {
                    // Повторяем оригинальный запрос с новым токеном
                    request.Headers.Authorization = null;
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);

                    // Клонируем запрос
                    var newRequest = await CloneRequest(request);

                    response.Dispose();
                    return await base.SendAsync(newRequest, cancellationToken);
                }
            }

            // Если не удалось обновить, возвращаем оригинальный ответ
            return response;
        }

        private async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage originalRequest)
        {
            var newRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);

            // Копируем заголовки
            foreach (var header in originalRequest.Headers)
            {
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Копируем контент
            if (originalRequest.Content != null)
            {
                var contentBytes = await originalRequest.Content.ReadAsByteArrayAsync();
                var stream = new MemoryStream(contentBytes);

                // Восстанавливаем позицию
                stream.Position = 0;

                // Копируем заголовки контента
                var content = new StreamContent(stream);
                foreach (var header in originalRequest.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                newRequest.Content = content;
            }

            return newRequest;
        }

        private bool IsExcludedPath(string path)
        {
            return _excludedPaths.Any(excluded =>
                path.Equals(excluded, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(excluded + "/", StringComparison.OrdinalIgnoreCase));
        }
    }
}
