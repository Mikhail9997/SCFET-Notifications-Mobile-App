using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Converters
{
    public class ShowDeleteInvitationButtonMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is InvitationStatus status && values[1] is bool isIncomingTab)
            {
                return status != InvitationStatus.Pending && isIncomingTab;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
