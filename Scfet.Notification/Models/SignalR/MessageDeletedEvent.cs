using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.SignalR
{
    public class MessageDeletedEvent
    {
        public Guid MessageId { get; set; }
        public Guid ChannelId { get; set; }
    }
}
