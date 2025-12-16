using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Utils
{
    public static class ResponseUtils
    {
        public static string GenerateQuery(Dictionary<string, string?> query)
        {
            var parameters = query
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")
                .ToArray();

            return string.Join("&", parameters); 
        }
    }
}
