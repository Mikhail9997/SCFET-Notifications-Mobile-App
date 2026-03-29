using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models
{
    public class Reply
    {
        public Guid Id { get; set; }
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReply
    {
        public Guid NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateReply
    {
        public string Message { get; set; } = string.Empty;
    }
}
