using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class IsReplyActionsEnableConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values[0] is Guid currentUserId &&
                values[1] is Guid replySenderId &&
                values[2] is Guid notificationSenderId)
            {
                bool isReplyAuthor = currentUserId == replySenderId;
                bool isNotificationAuthor = currentUserId == notificationSenderId;

                return isReplyAuthor || isNotificationAuthor;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
