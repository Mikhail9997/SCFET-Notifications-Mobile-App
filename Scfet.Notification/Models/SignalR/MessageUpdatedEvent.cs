using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.SignalR
{
    public class MessageUpdatedEvent
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid ChannelId { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }
}
