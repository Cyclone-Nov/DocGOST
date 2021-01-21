using System;

namespace GostDOC.Common
{
    public static class Converters
    {
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
