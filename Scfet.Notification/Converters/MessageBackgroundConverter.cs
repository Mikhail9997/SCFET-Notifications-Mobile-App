using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isOwnMessage = (bool)value;

            if (isOwnMessage)
            {
                return Application.Current.RequestedTheme == AppTheme.Light
                    ? Color.FromArgb("#DCF8C6")  // WhatsApp зеленый для своих
                    : Color.FromArgb("#056162"); // Темная тема
            }
            else
            {
                return Application.Current.RequestedTheme == AppTheme.Light
                    ? Colors.White
                    : Color.FromArgb("#1E1E1E");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
