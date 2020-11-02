using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Font;
using GostDOC.Common;

namespace GostDOC.PDF
{

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class PdfUtils
    {
        /// <summary>
        /// Gets the count page.
        /// </summary>
        /// <param name="aType">a type.</param>
        /// <param name="">The .</param>
        /// <returns></returns>
        public static int GetCountPage(DocType aType, int aTotalRows)
        {
            int RowsOnFirstPage;
            int RowsOnNextPage;
            switch (aType)
            {
                case DocType.Bill:
                    {                        
                        RowsOnFirstPage = Constants.BillRowsOnFirstPage;
                        RowsOnNextPage = Constants.BillRowsOnNextPage;
                    }
                    break;
                case DocType.D27:
                    {                     
                        RowsOnFirstPage = Constants.DefaultRowsOnFirstPage;
                        RowsOnNextPage = Constants.DefaultRowsOnNextPage;
                    }
                    break;
                case DocType.Specification: 
                    {                    
                        RowsOnFirstPage = Constants.SpecificationRowsOnFirstPage;
                        RowsOnNextPage = Constants.SpecificationRowsOnNextPage;
                    }
                    break;
                case DocType.ItemsList:
                    {                     
                        RowsOnFirstPage = Constants.ItemListRowsOnFirstPage;
                        RowsOnNextPage = Constants.ItemListRowsOnNextPage;
                    }
                    break;                
                default:
                    {                     
                        RowsOnFirstPage = Constants.DefaultRowsOnFirstPage;
                        RowsOnNextPage = Constants.DefaultRowsOnNextPage;
                    }
                    break;
            }

            int countPages = 1;
            int notFirstPageRows = aTotalRows - RowsOnFirstPage;
            if (notFirstPageRows > 0)
            {
                countPages += (int)(notFirstPageRows / RowsOnNextPage) + (notFirstPageRows % RowsOnNextPage > 0 ? 1: 0);
            }
            if (countPages > PdfDefines.MAX_PAGES_WITHOUT_CHANGELIST)
            {
                countPages++;
            }
            return countPages;
        }

        /// <summary>
        /// Разбить строку на несколько строк исходя из длины текста
        /// </summary>
        /// <param name="aLength">максимальная длина в мм</param>
        /// <param name="aString">строка для разбивки</param>
        /// <param name="aFontSize">размер шрифта</param>
        /// <returns></returns>
        public static List<string> SplitStringByWidth(float aLength, string aString, char[] aDelimiters, float aFontSize = PdfDefines.DefaultFontSize, bool aUseGOST = false)
        {
            if (string.IsNullOrEmpty(aString) || aDelimiters == null || aDelimiters.Length == 0)
                return new List<string>() { string.Empty};                          

            List<string> name_strings = new List<string>();
            int default_padding = 2;
            float maxLength = aLength * PdfDefines.mmAXw - default_padding;
            var font = PdfDefines.MainFont;
            float currLength = font.GetWidth(aString, aFontSize);

            List<string> PREF_ARR = new List<string>() { @" ГОСТ ", @" ОСТ ", @" ТУ ", @" ANSI ", @" ISO ", @" DIN " };

            if (currLength < maxLength)
            {
                name_strings.Add(aString);
            } else
            {
                string fullName = aString;
                do
                {
                    // извлекаем из строки то число символов, которое может поместиться в указанную длину maxLength
                    int symbOnMaxLength = (int)((fullName.Length / currLength) * maxLength);
                    string partName = fullName.Substring(0, symbOnMaxLength);

                    // пробуем найти ближайший символ, по которому можно переносить фразу и извлечем часть для первой строки
                    int index = (aUseGOST && PREF_ARR.Any(s=> partName.Contains(s))) ? partName.IndexOf(PREF_ARR.Find(b => partName.Contains(b))) : partName.LastIndexOfAny(aDelimiters);
                    if (index < 0)
                    {
                        name_strings.Add(partName);
                        fullName = fullName.Substring(symbOnMaxLength + 1);
                    } else
                    {
                        name_strings.Add(fullName.Substring(0, index).TrimEnd());
                        fullName = fullName.Substring(index).TrimStart();
                    }
                    currLength = font.GetWidth(fullName, aFontSize);
                }
                while (currLength > maxLength);
                name_strings.Add(fullName);
            }

            return name_strings;
        }

        
    }
}
