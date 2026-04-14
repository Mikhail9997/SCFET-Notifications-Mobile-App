using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class DateHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);

                if (dateTime.Date == today)
                    return "Сегодня";
                if (dateTime.Date == yesterday)
                    return "Вчера";

                return dateTime.ToString("dd MMMM yyyy");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
