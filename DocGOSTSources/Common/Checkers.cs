using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Common
{
    public static class Checkers
    {

        /// <summary>
        /// Конвертирование строки с позиционным обозначением компонента в формате 
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="Result">The result.</param>
        /// <returns></returns>
        public static bool CheckDesignatorFormat(string aDesigantor)
        {
            // не может быть пустым и быть меньше 2 символов
            if (string.IsNullOrEmpty(aDesigantor) || aDesigantor.Length < 2)
                return false;

            for (int i = 0; i < aDesigantor.Length; i++)
            {
                if (Char.IsDigit(aDesigantor[i]))
                {                    
                    string symb = aDesigantor.Substring(0, i);
                    // символы должны быть
                    if (string.IsNullOrEmpty(symb))
                        return false;

                    string digs = aDesigantor.Substring(i);
                    // цифры должны быть
                    if (string.IsNullOrEmpty(digs))
                        return false;
                    // 
                    return Int32.TryParse(digs, out var dig);
                }
            }

            // не нашли цифр
            return false;
        }

    }
}
