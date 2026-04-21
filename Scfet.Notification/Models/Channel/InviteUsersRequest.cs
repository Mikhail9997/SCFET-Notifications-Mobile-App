using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class InviteUsersRequest
    {
        public List<Guid> UserIds { get; set; } = new();
        public string? Message { get; set; }
    }
}
