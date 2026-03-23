using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class PercentageToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;

                if (currentTheme == AppTheme.Light)
                {
                    if (percentage < 30)
                        return Color.FromArgb("#FF4444"); // Ярко-красный
                    else if (percentage < 70)
                        return Color.FromArgb("#FFA500"); // Оранжевый
                    else
                        return Color.FromArgb("#4CAF50"); // Зеленый
                }
                else
                {
                    if (percentage < 30)
                        return Color.FromArgb("#FF6B6B"); // Светло-красный
                    else if (percentage < 70)
                        return Color.FromArgb("#FFB347"); // Мягкий оранжевый
                    else
                        return Color.FromArgb("#81C784"); // Мягкий зеленый
                }
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
