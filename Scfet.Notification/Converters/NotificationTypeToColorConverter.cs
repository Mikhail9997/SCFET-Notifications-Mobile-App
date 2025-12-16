using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class NotificationTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value?.ToString() ?? "Info";

            return type switch
            {
                "Urgent" => Color.FromArgb("#FFE0E0"),
                "Warning" => Color.FromArgb("#FFF9E0"),
                "Event" => Color.FromArgb("#E0F7FF"),
                _ => Color.FromArgb("#F0F0F0") // Info
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
