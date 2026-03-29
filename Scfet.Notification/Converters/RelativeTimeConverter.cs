using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class RelativeTimeConverter : IValueConverter
    {
        private readonly int _timezoneOffset = 3;
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTime dateTime) return string.Empty;

            var localDateTime = dateTime.AddHours(_timezoneOffset);
            var now = DateTime.Now;
            var today = now.Date;
            var messageDate = localDateTime.Date;
            var timeSpan = now - localDateTime;

            // Сегодня 
            if (messageDate == today)
                return localDateTime.ToString("HH:mm");

            // Вчера
            if (messageDate == today.AddDays(-1))
                return "Вчера";

            // Последние 7 дней
            if (messageDate > today.AddDays(-7))
                return GetDayOfWeek(localDateTime.DayOfWeek);

            // В этом году 
            if (messageDate.Year == now.Year)
                return localDateTime.ToString("d MMMM");

            // Старше года
            return localDateTime.ToString("d MMMM yyyy");
        }

        private string GetDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => string.Empty
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
