using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Converters
{
    public class InvitationStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                InvitationStatus.Pending => "#FFA500",
                InvitationStatus.Accepted => "#4CAF50",
                InvitationStatus.Declined => "#F44336",
                InvitationStatus.Expired => "#9E9E9E",
                _ => "#CCCCCC"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
