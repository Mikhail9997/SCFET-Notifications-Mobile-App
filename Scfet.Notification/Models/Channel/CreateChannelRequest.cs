using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class CreateChannelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
