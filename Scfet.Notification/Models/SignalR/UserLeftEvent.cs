using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.SignalR
{
    public class UserLeftEvent
    {
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public Guid ChannelId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
