using System;
using System.Windows.Data;
using System.Globalization;

namespace GostDOC.Converters
{
    public class FloatToStringConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";
            return value.ToString();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            if (Single.TryParse(value.ToString(), out var count))
                return count;

            return 0;
        }
    }
}
