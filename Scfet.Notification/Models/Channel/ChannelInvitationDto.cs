using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models.Channel
{
    public class ChannelInvitationDto: INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public string? ChannelDescription { get; set; }
        public Guid InviterId { get; set; }
        public string InviterName { get; set; } = string.Empty;
        public string? InviterAvatar { get; set; }
        public Guid InviteeId { get; set; }
        public string InviteeName { get; set; } = string.Empty;
        public string? InviteeAvatar { get; set; }
        public string? Message { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsExpired { get; set; }
        public string? TimeAgo { get; set; }
        public bool IsIncomingTab { get; set; }

        private InvitationStatus _status;
        public InvitationStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public enum InvitationStatus
    {
        Pending = 1,
        Accepted = 2,
        Declined = 3,
        Expired = 4
    }
}
