using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models.Channel;

namespace Scfet.Notification.Converters
{
    public class ChannelRoleToRussianConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                ChannelRole.Owner => "Владелец",
                ChannelRole.Admin => "Администратор",
                ChannelRole.Moderator => "Модератор",
                ChannelRole.Member => "Участник",
                _ => "Неизвестно"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
