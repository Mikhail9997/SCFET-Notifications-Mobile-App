using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class AvailableUsersFilter : Filter
    {
        public UserRole? Role { get; set; }
        public Guid? GroupId { get; set; }
        public string? SearchTerm { get; set; }
    }
}
