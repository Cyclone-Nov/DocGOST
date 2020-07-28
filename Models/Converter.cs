using System;
using System.Collections.Generic;
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
            return null;
        }

        private void FillGroupNames()
        {
            _groupNames.Add("A", new Tuple<string, string>("Устройство", "Устройства"));
            _groupNames.Add("BA", new Tuple<string, string>("Громкоговоритель", "Громкоговорители"));
            _groupNames.Add("BB", new Tuple<string, string>("Магнитострикционный элемент", "Магнитострикционные элементы"));
            _groupNames.Add("BD", new Tuple<string, string>("Детектор ионизирующих излучений", "Детекторы ионизирующих излучений"));

            // TODO: Fill values
        }
    }
}