using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class CounterColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int length)
            {
                int maxLength = parameter != null ? int.Parse(parameter.ToString()) : 100;

                if (length >= maxLength)
                    return Colors.Red;
                else if (length > maxLength * 0.8)
                    return Colors.Orange;
                else
                    return (Color)Application.Current.Resources["CounterColor"] ?? Colors.Gray;
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
