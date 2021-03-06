﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Common
{
    public static class Checkers
    {

        /// <summary>
        /// Конвертирование строки с позиционным обозначением компонента. 
        /// Формат позиционного обозначения: SSDD, где SS - набор символов английского алфавита от 1 и более, DD - набор цифр от одной и более
        /// </summary>        
        /// <param name="aDesigantor">строка форматов X или X,Y,Z (и т.д.) или X-Y, где символы - позиционны обозначения</param>
        /// <returns></returns>
        public static bool CheckDesignatorFormat(string aDesigantor)
        {
            // не может быть пустым и быть меньше 2 символов
            if (string.IsNullOrEmpty(aDesigantor) || aDesigantor.Length < 2)
                return false;

            // позиционное обозначение может быть объединным, тогда проверим каждый символ
            var designators = aDesigantor.Split(new char[] { '-',','}, StringSplitOptions.RemoveEmptyEntries);

            bool res = true;
            foreach (var item in designators)
            {
                string desigantor = item.Trim();
                for (int i = 0; i < desigantor.Length; i++)
                {
                    if (Char.IsDigit(desigantor[i]))
                    {
                        string symb = desigantor.Substring(0, i);
                        // символы должны быть
                        if (string.IsNullOrEmpty(symb))
                            return false;

                        string digs = desigantor.Substring(i);
                        // цифры должны быть
                        if (string.IsNullOrEmpty(digs))
                            return false;
                        // 
                        res &= Int32.TryParse(digs, out var dig);
                    } else if (!Char.IsLetter(desigantor[i]))
                    {
                        return false;
                    }
                }
            }
                        
            return res;
        }

    }
}
