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
    /// Расшириение для класса String
    /// </summary>
    public static class StringExt
    {
        /// <summary>
        /// обрезать строку value 
        /// </summary>
        /// <param name="value">исходная строка</param>
        /// <param name="maxLength">максимальное количество символов в строке</param>
        /// <returns>результат</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// класс с вспомогательными функциями для создания PDF
    /// </summary>
    public static class PdfUtils
    {   

        /// <summary>
        /// Разбить строку на несколько строк исходя из длины текста
        /// Если задано несколько разделителей, то приоритетным считается пробел
        /// </summary>
        /// <param name="aLength">максимальная длина в мм</param>
        /// <param name="aString">строка для разбивки</param>
        /// <param name="aDelimiters">массив символов-разделителей, по которым можно разбивать строку</param>
        /// <param name="aFontSize">размер шрифта, по умолчанию PdfDefines.DefaultFontSize</param>
        /// <param name="aUseGOST">признак, что надо разбивать относительно массива специальных подстрок</param>
        /// <returns>список строк, являющихся разбивкой исходной строки aString</returns>
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
                    int nameLength = fullName.Length;
                    int symbOnMaxLength = (int)(nameLength * (maxLength / currLength));
                    string partName = fullName.Substring(0, symbOnMaxLength);

                    // проверим что получившася подстрока вмещается, иначе уменьшим ее
                    float newLength = font.GetWidth(partName, aFontSize);
                    while(newLength > maxLength)
                    {
                        symbOnMaxLength--;
                        partName = fullName.Substring(0, symbOnMaxLength);
                        newLength = font.GetWidth(partName, aFontSize);
                    }

                    // пробуем найти ближайший символ, по которому можно переносить фразу и извлечем часть для первой строки
                    int index = -1;
                    if (aUseGOST && PREF_ARR.Any(s => partName.Contains(s)))
                    {
                        index = partName.IndexOf(PREF_ARR.Find(b => partName.Contains(b)));
                        aUseGOST = false;
                    }
                    else
                    {
                        if(aDelimiters.Contains(' '))
                            index = partName.LastIndexOfAny(new char[] { ' ' });
                        if (index < 0)
                            index = partName.LastIndexOfAny(aDelimiters);
                    }

                    if (index < 0)
                    {
                        name_strings.Add(partName);
                        fullName = fullName.Substring(symbOnMaxLength);
                    } else
                    {
                        name_strings.Add(fullName.Substring(0, index+1).TrimEnd());
                        fullName = fullName.Substring(index+1).TrimStart();
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
