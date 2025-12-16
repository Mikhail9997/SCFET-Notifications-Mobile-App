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
        public Guid? GroupId { get; set; }
    }

    public class GroupFilter
    {
        public string? Name { get; set; } = string.Empty;
    }

    public class NotificationFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public NotificationSortOrder SortOrder { get; set; } = NotificationSortOrder.Descending;
        public NotificationSortBy SortBy { get; set; } = NotificationSortBy.CreatedAt;
    }

    public enum NotificationSortOrder
    {
        Ascending,
        Descending
    }
    public enum NotificationSortBy
    {
        CreatedAt,
        Title
    }
}
