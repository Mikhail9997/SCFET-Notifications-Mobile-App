using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Converters
{
    public class BorderStrokeToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is Guid cardSenderId && values[1] is Guid currentUserId)
            {
                if(cardSenderId == currentUserId)
                {
                    if (Application.Current.Resources.ContainsKey("PrimaryColor"))
                    {
                        return (Color)Application.Current.Resources["PrimaryColor"];
                    }
                }
            }
            return Colors.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
