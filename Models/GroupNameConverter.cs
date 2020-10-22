using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    static class GroupNameConverter
    {
        static Dictionary<string, Tuple<string, string>> _groupNames = new Dictionary<string, Tuple<string, string>>();

        static GroupNameConverter()
        {
            FillGroupNames();
        }

        public static string GetGroupName(string aSymbol, bool isOneElement = false)
        {
            if (!string.IsNullOrEmpty(aSymbol))
            {
                string symbol = string.Concat(aSymbol.Where(char.IsLetter));

                Tuple<string, string> names;
                if (_groupNames.TryGetValue(symbol, out names))
                {
                    return isOneElement ? names.Item1 : names.Item2;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Получить соответствующий символ по имени группы
        /// </summary>
        /// <param name="aGroupName">Name of a group.</param>        
        /// <returns></returns>
        public static string GetSymbol(string aGroupName)
        {
            if (!string.IsNullOrEmpty(aGroupName))
            {                
                foreach(var group in _groupNames)
                {
                    if (string.Equals(group.Value.Item1, aGroupName, StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(group.Value.Item2, aGroupName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return group.Key;
                    }
                }
            }
            return string.Empty;
        }

        private static void FillGroupNames()
        {
            foreach (var line in Utils.ReadCfgFileLines("PhysicalDesignators"))
            {
                string[] split = line.Split(new char[] { '\t' });
                if (split.Length == 3)
                {
                    _groupNames.Add(split[0], new Tuple<string, string>(split[1], split[2]));
                }
            }
        }
    }
}