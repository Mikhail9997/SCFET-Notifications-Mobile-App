using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Converters
{
    public class ChannelRoleToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                ChannelRole.Owner => "#FF6B6B",
                ChannelRole.Admin => "#4ECDC4",
                ChannelRole.Moderator => "#45B7D1",
                ChannelRole.Member => "#96CEB4",
                _ => "#CCCCCC"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
