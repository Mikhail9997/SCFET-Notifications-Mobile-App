using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Converters
{
    public class InvitationStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                InvitationStatus.Pending => "Ожидает",
                InvitationStatus.Accepted => "Принято",
                InvitationStatus.Declined => "Отклонено",
                InvitationStatus.Expired => "Истекло",
                _ => "Неизвестно"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
