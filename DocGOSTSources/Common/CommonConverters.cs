using System;

namespace GostDOC.Common
{
    public static class Converters
    {
        /// <summary>
        /// Получить строку с кодом документа по типу документа
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
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
        /// Конвертирование строки с позиционным обозначением компонента в формате 
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="Result">The result.</param>
        /// <returns></returns>
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
        /// Gets the price with tax.
        /// </summary>
        /// <param name="aPurePrice">a pure price.</param>
        /// <param name="aTax">a tax.</param>
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
