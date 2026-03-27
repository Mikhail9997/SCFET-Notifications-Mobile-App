using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Scfet.Notification.Services
{
    interface IJsonSerializeService
    {
        Task<T?> DeserializeResponse<T>(HttpResponseMessage response) where T : class;
    }
    public class JsonSerializeService: IJsonSerializeService
    {
        public async Task<T?> DeserializeResponse<T>(HttpResponseMessage response) where T : class
        {
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(content))
                return null;

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
