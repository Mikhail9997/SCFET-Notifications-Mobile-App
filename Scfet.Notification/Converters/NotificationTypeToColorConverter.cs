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

            AppTheme currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;

            return GetBackgroundColor(type, currentTheme);
        }

        private Color GetBackgroundColor(string type, AppTheme theme)
        {
            if (theme == AppTheme.Dark)
            {
                return type switch
                {
                    "Urgent" => Color.FromArgb("#4A1E1E"), 
                    "Warning" => Color.FromArgb("#4A401E"),
                    "Event" => Color.FromArgb("#1E3A4A"),
                    _ => Color.FromArgb("#2A2A2A") 
                };
            }
            else // Light theme
            {
                return type switch
                {
                    "Urgent" => Color.FromArgb("#FFE0E0"), 
                    "Warning" => Color.FromArgb("#FFF9E0"), 
                    "Event" => Color.FromArgb("#E0F7FF"), 
                    _ => Color.FromArgb("#F0F0F0")
                };
            }
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
