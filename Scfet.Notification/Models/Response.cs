using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models
{
    public class Response<T> where T : class
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
    public class Response
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
