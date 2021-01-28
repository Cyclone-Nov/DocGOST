using System;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Text;

namespace GostDOC.Common
{
    public static class Converters
    {
        /// <summary>
        /// получить значение тега DescriptionAttribute для значения enumObj перечисление типа Enum 
        /// </summary>
        /// <param name="enumObj">значение перечисления типа Enum</param>
        /// <returns>строка со значением атрибута DescriptionAttribute для данного значения перечисления Enum иначе само значение</returns>
        public static string GetEnumDescription(Enum enumObj)
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

        /// <summary>
        /// преобразование строки с описаниеи для типа перечисления в соответсвующее значение перечисления
        /// </summary>
        /// <typeparam name="T">должен быть типа перечисления Enum с установленными атрибутами Description</typeparam>
        /// <param name="aDescription">строка с описанием</param>
        /// <param name="bIsValid">признак успешного преобразования -  <c>true</c>.</param>
        /// <returns></returns>
        public static T DescriptionToEnum<T>(string aDescription, out bool bIsValid) where T : Enum // where T : struct, IConvertible
        {
            bIsValid = false;
            T errVal = (T)Enum.GetValues(typeof(T)).GetValue(0);
            if (!typeof(T).IsEnum)
            {                
                return errVal;
                //throw new ArgumentException("T must be an enumerated type");
            }

            if (UnicodeCyrillicToASCIIUtils.IsUnicode(aDescription))
            {
                aDescription = UnicodeCyrillicToASCIIUtils.UnicodeRusStringToASCII(aDescription);
            }

            foreach (T item in Enum.GetValues(typeof(T)))
            {
                FieldInfo fieldInfo = item.GetType().GetField(item.ToString());
                object[] attribArray = fieldInfo.GetCustomAttributes(false);
                if (attribArray.Length == 0)
                    continue; // 
                else
                {
                    var attrib = attribArray.Where( attr => attr is DescriptionAttribute).First() as DescriptionAttribute;
                    if (attrib != null && string.Equals(attrib.Description, aDescription, StringComparison.InvariantCultureIgnoreCase))
                    {
                        bIsValid = true;
                        return item;
                    }
                }
            }
            return errVal;
        }

        /// <summary>
        /// Получить строку с символьным кодом документа по типу документа
        /// </summary>
        /// <param name="aDocType">Тип документа типа <paramref name="DocType"/></param>
        /// <returns></returns>
        public static string GetDocumentCode(DocType aDocType)
        {
            switch (aDocType)
            {
                case DocType.Bill:
                    return @"ВП";
                case DocType.ItemsList:
                    return @"ПЭ3";
                case DocType.Specification:
                case DocType.D27:
                case DocType.None:
                    return string.Empty;
            }
            return string.Empty;
        }

        public static string DocTypeToString(DocType aDocType)
        {
            switch (aDocType)
            {
                case DocType.Bill:
                    return @"ВП";
                case DocType.ItemsList:
                    return @"ПЭ3";
                case DocType.Specification:
                case DocType.D27:
                case DocType.None:
                    return string.Empty;
            }
            return string.Empty;
        }


        /// <summary>
        /// Конвертирование строки с позиционным обозначением компонента в пару значений <paramref name="Result"/>, 
        /// соответствующую символам позиционного обозначения и числу позиционного обозначения отдельно
        /// </summary>
        /// <param name="s">входная строка с позиционным обозначенияем</param>
        /// <param name="Result">результат разделения позуионного обозначения на строку сисмволов и число</param>
        /// <returns>true - конвертиация прошла успешно, иначе false</returns>
        public static bool SplitDesignatorToStringAndNumber(string s, out Tuple<string, int> Result)
        {            
            Result = null;
            for (int i = 0; i < s.Length; i++)
            {
                if (Char.IsDigit(s[i]))
                {
                    try
                    {
                        string val = s.Substring(0, i);
                        int dig = Int32.Parse(s.Substring(i));

                        Result = new Tuple<string, int>(val, dig);
                        return true;
                    }
                    catch
                    { 
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// получить стоимость с учетом налога
        /// </summary>
        /// <param name="aPurePrice">стоимость до учета налога</param>
        /// <param name="aTax">тип налоговой ставки</param>
        /// <returns></returns>
        public static float GetPriceWithTax(float aPurePrice, TaxTypes aTax)
        {
            if (aTax == TaxTypes.Tax10)
                return aPurePrice * 1.1f;

            if (aTax == TaxTypes.Tax20)
                return aPurePrice * 1.2f;

            return aPurePrice;
        }

    }
}
