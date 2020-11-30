using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace GostDOC.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        private string GetEnumDescription(Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());
            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
                return enumObj.ToString();
            else
            {
                DescriptionAttribute attrib = null;

                foreach (var att in attribArray)
                {
                    if (att is DescriptionAttribute)
                        attrib = att as DescriptionAttribute;
                }

                if (attrib != null)
                    return attrib.Description;

                return enumObj.ToString();
            }
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            Enum myEnum = (Enum)value;
            return GetEnumDescription(myEnum);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            foreach (var one in Enum.GetValues(targetType))
            {
                Enum myEnum = (Enum)one;
                if (value.ToString() == GetEnumDescription(myEnum))
                    return one;
            }
            return null;
        }
    }
}
