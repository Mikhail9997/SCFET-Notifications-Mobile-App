using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models
{
    public class Profile
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int Code { get; set; }
        public User? User { get; set; }
    }
}
