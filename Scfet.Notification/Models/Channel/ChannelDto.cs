using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class ChannelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string? OwnerAvatar { get; set; }
        public int MembersCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ChannelRole? UserRole { get; set; }
        public bool IsOwner { get; set; }
        public bool IsMember { get; set; }
    }
}
