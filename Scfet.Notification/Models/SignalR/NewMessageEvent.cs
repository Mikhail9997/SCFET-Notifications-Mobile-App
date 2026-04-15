using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Models.SignalR
{
    public class NewMessageEvent
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
        public DateTime CreatedAt { get; set; }
    }
}
