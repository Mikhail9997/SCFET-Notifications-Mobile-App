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
    public class ApiResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
    public class ApiResponse<T>: Response<T> where T : class
    {
        public PaginationInfo? Pagination { get; set; }
    }
    public class PaginationInfo
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
