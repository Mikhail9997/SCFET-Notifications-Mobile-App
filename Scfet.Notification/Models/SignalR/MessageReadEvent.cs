using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.SignalR
{
    public class MessageReadEvent
    {
        public Guid MessageId { get; set; }
        public Guid ChannelId { get; set; }
        public Guid ReadByUserId { get; set; }
        public string? ReadByUserName { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
