using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Converters
{
    public class BoolToLabelMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not bool isIncomingTab || values[1] is not ChannelInvitationDto)
                return string.Empty;

            return isIncomingTab ? "От: " : "Кому: ";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
