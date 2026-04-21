using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Scfet.Notification.Models.Channel
{
    public class ChannelMemberDto: ObservableObject
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public UserRole UserRole { get; set; }
        public string UserRoleText { get; set; } = string.Empty;
        public bool IsCurrentUser { get; set; }

        private ChannelRole _channelRole;
        public ChannelRole ChannelRole
        {
            get => _channelRole;
            set => SetProperty(ref _channelRole, value);
        }

        private string _channelRoleText;
        public string ChannelRoleText
        {
            get => _channelRoleText;
            set => SetProperty(ref _channelRoleText, value);
        }
    }
    public enum ChannelRole
    {
        Member = 1,
        Moderator = 2,
        Admin = 3,
        Owner = 4
    }
}
