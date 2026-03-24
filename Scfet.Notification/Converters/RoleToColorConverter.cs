using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value as string) switch
            {
                "Administrator" => "#E74C3C",  // Красный
                "Teacher" => "#3498DB",     // Синий
                "Student" => "#2ECC71",    // Зеленый
                "Parent" => "#F39C12",     // Оранжевый                    
                _ => "#95A5A6"             // Серый
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
