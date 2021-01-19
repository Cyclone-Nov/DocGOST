using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using GostDOC.Context;
using GostDOC.Models;

namespace GostDOC.Converters
{
    public class DeliveryIntervalConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            var deliveryInterval = (DeliveryInterval)value;
            if (deliveryInterval.DeliveryTimeMax == deliveryInterval.DeliveryTimeMin)
                return deliveryInterval.DeliveryTimeMin.ToString();
            return $"{deliveryInterval.DeliveryTimeMin} - {deliveryInterval.DeliveryTimeMax}";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {         
            var deliveryInterval = ((string)value).Split(new char[] {'-'});
            float min = 0, max = 0;
            
            if (deliveryInterval != null && deliveryInterval.Length > 0)
            {
                Single.TryParse(deliveryInterval[0].Trim(), out min);
                if (deliveryInterval.Length == 2)                
                    Single.TryParse(deliveryInterval[1].Trim(), out max);                
                else                
                    max = min;                
            }

            return new DeliveryInterval() { DeliveryTimeMin = min, DeliveryTimeMax = max};
        }
    }
}
