using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Font;
using GostDOC.Common;

namespace GostDOC.PDF
{
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
                        RowsOnFirstPage = 24;
                        RowsOnNextPage = 29;
                    }
                    break;
                case DocType.D27:
                    {                     
                        RowsOnFirstPage = 24;
                        RowsOnNextPage = 29;
                    }
                    break;
                case DocType.Specification: 
                    {                    
                        RowsOnFirstPage = 24;
                        RowsOnNextPage = 29;
                    }
                    break;
                case DocType.ItemsList:
                    {                     
                        RowsOnFirstPage = 24;
                        RowsOnNextPage = 31;
                    }
                    break;                
                default:
                    {                     
                        RowsOnFirstPage = 26;
                        RowsOnNextPage = 33;
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
        public static List<string> SplitStringByWidth(float aLength, string aString, float aFontSize = PdfDefines.DefaultFontSize)
        {
            List<string> name_strings = new List<string>();
            int default_padding = 4;
            float maxLength = aLength * PdfDefines.mmAXw - default_padding;
            var font = PdfDefines.MainFont;
            float currLength = font.GetWidth(aString, aFontSize);

            GetLimitSubstring(name_strings, maxLength, currLength, aString);

            return name_strings;
        }

        /// <summary>
        /// разделить строку на писок строк по максимально допустимой ширине строки
        /// </summary>
        /// <param name="name_strings">результат в виде списка строк</param>
        /// <param name="maxLength">ограничение на ширину строки</param>
        /// <param name="currLength">текущая ширина фразы</param>
        /// <param name="aFullName">фраза которую надо разделить</param>
        public static void GetLimitSubstring(List<string> name_strings, float maxLength, float currLength, string aFullName)
        {
            if (currLength < maxLength)
            {
                name_strings.Add(aFullName);
            } else
            {
                string fullName = aFullName;
                // извлекаем из строки то число символов, которое может поместиться в указанную длину maxLength
                int symbOnMaxLength = (int)((fullName.Length / currLength) * maxLength);
                string partName = fullName.Substring(0, symbOnMaxLength);
                
                // пробуем найти ближайший символ, по которому можно переносить фразу и извлечем часть для первой строки
                int index = partName.LastIndexOfAny(new char[] { ' ', '-', '.' });
                if (index < 0)
                {
                    name_strings.Add(partName);
                    fullName = fullName.Substring(symbOnMaxLength + 1);
                }
                else
                {
                    name_strings.Add(fullName.Substring(0, index));
                    fullName = fullName.Substring(index + 1);
                }
                currLength = fullName.Length;
                GetLimitSubstring(name_strings, maxLength, currLength, fullName);
            }
        }
    }
}
