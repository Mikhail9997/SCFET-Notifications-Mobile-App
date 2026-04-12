using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class ChannelMessageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid ChannelId { get; set; }

        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public UserRole SenderRole { get; set; }
        public ChannelRole? SenderChannelRole { get; set; }

        public Guid? ReplyToMessageId { get; set; }
        public ReplyMessageDto? ReplyToMessage { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        // Вспомогательные свойства для UI
        public string TimeAgo => GetTimeAgo();
        public bool IsOwnMessage { get; set; }
        public string SenderInitials => GetInitials();

        private string GetTimeAgo()
        {
            var timeSpan = DateTime.UtcNow - CreatedAt;

            if (timeSpan.TotalDays > 365)
                return $"{(int)(timeSpan.TotalDays / 365)} г. назад";
            if (timeSpan.TotalDays > 30)
                return $"{(int)(timeSpan.TotalDays / 30)} мес. назад";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} д. назад";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} ч. назад";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} мин. назад";

            return "Только что";
        }

        private string GetInitials()
        {
            if (string.IsNullOrEmpty(SenderName))
                return "?";

            var parts = SenderName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length > 0)
                return parts[0][0].ToString().ToUpper();

            return "?";
        }
    }

}
