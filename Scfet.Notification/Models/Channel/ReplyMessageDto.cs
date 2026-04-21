using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class ReplyMessageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
