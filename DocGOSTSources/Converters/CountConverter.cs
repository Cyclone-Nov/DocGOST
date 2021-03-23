using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GostDOC.Converters
{
    public class CountConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";

            float val = 0;
            try
            {
                val = Convert.ToSingle(value);
            }
            catch
            {
                val = 0;
            }

            if (val == 0)
            {
                return "";
            }
            return val;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            if (float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var count))
                return count;

            return 0;
        }
    }
}
