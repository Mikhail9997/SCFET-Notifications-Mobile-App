using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models
{
    public class UserFilter
    {
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public Guid? GroupId { get; set; }
    }

    public class GroupFilter
    {
        public string? Name { get; set; } = string.Empty;
    }

    public class Filter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SortOrder SortOrder { get; set; } = SortOrder.Descending;
        public SortBy SortBy { get; set; } = SortBy.CreatedAt;
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }
    public enum SortBy
    {
        CreatedAt,
        Title
    }
}
