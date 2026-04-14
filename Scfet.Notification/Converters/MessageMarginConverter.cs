using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class MessageMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOwnMessage)
            {
                // Для своих сообщений - отступ слева (чтобы прижалось к правому краю)
                // Для чужих - отступ справа
                return isOwnMessage
                    ? new Thickness(50, 2, 0, 2)   // Свои: большой отступ слева
                    : new Thickness(0, 2, 50, 2);  // Чужие: большой отступ справа
            }

            return new Thickness(0, 2, 50, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
