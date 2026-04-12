using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class UnreadCountResponse
    {
        public bool Success { get; set; }
        public int UnreadCount { get; set; }
    }
}
