using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class InverseBoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                var colors = paramString.Split(';');
                if (colors.Length == 2)
                {
                    //если true - используем второй цвет, если false - первый
                    var colorString = boolValue ? colors[1] : colors[0];
                    return Color.FromArgb(colorString);
                }
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
