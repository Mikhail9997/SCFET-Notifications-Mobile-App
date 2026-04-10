using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models
{
    public class Favorite:INotifyPropertyChanged
    {
        public Guid NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public bool IsPersonal { get; set; }
        public bool AllowReplies { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
        public bool IsEnable { get; set; }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
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
    public class AddFavorite
    {
        public Guid NotificationId { get; set; }
    }
}
