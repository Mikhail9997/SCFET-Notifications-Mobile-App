using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Scfet.Notification.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;

        public string FormattedDate => CreatedAt.AddHours(3).ToString("dd.MM.yyyy HH:mm");
        public string ShortDate => CreatedAt.AddHours(3).ToString("dd.MM.yyyy");
    }

    public class CreateNotification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public List<Guid>? TargetUserIds { get; set; }
        public Guid? TargetGroupId { get; set; }
        public FileResult? Image { get; set; }
    }
    public class SentNotification
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalReceivers { get; set; }
        public int ReadReceivers { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;

        public string FormattedDate => CreatedAt.AddHours(3).ToString("dd.MM.yyyy HH:mm");
        public double ReadPercentage => TotalReceivers > 0 ? (double)ReadReceivers / TotalReceivers * 100 : 0;
    }

    public class GetNotification<T>
    {
        public IReadOnlyList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public enum NotificationType
    {
        Info = 1,
        Warning = 2,
        Urgent = 3,
        Event = 4
    }
}
