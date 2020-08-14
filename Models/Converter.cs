using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    class Converter
    {
        Dictionary<string, Tuple<string, string>> _groupNames = new Dictionary<string, Tuple<string, string>>();

        public Converter()
        {
            FillGroupNames();
        }

        public string GetGroupName(string aSymbol, bool isOneElement = false)
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

        private void FillGroupNames()
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