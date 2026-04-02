using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scfet.Notification.Models;

namespace Scfet.Notification.Converters
{
    public class IsSelectedPresetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is AvatarPreset selectedPreset &&
                values[1] is AvatarPreset currentPreset)
            {
                return selectedPreset != null && currentPreset != null &&
                       selectedPreset.Key == currentPreset.Key;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
