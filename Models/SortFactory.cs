using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    interface ISort<T>
    {
        List<T> Sort(List<T> aItems); 
    }

    class NameSort : ISort<Component>
    {
        public List<Component> Sort(List<Component> aItems)
        {
            return aItems.OrderBy(x => x.Properties[Constants.ComponentName]).ToList();
        }
    }

    class SignSort : ISort<Component>
    {
        public List<Component> Sort(List<Component> aItems)
        {
            return aItems.OrderBy(x => x.Properties[Constants.ComponentSign]).ToList();
        }
    }

    class NameSortRegex : ISort<Component>
    {
        private string _regex = string.Empty;

        #region Singleton
        private static readonly Lazy<NameSortRegex> _instance = new Lazy<NameSortRegex>(() => new NameSortRegex(), true);
        public static NameSortRegex Instance => _instance.Value;
        NameSortRegex()
        {
            StringBuilder unitsGroup = new StringBuilder(@"\w*?(\d*)\s*(");

            bool first = true;
            using (var reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Units.txt")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    foreach (var str in line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (first)
                            first = false;
                        else
                            unitsGroup.Append('|');

                        unitsGroup.Append(str);
                    }                    
                }
            }            
            unitsGroup.Append(")");

            _regex = unitsGroup.ToString();
        }
        #endregion
        private static double ParseMultiplier(string aSuffix)
        {
            if (aSuffix.StartsWith("н", StringComparison.InvariantCulture))
                return Math.Pow(10, -9);
            if (aSuffix.StartsWith("п", StringComparison.InvariantCulture))
                return Math.Pow(10, -6);
            if (aSuffix.StartsWith("мк", StringComparison.InvariantCulture))
                return Math.Pow(10, -3);
            if (aSuffix.StartsWith("к", StringComparison.InvariantCulture))
                return Math.Pow(10, 3);
            if (aSuffix.StartsWith("М", StringComparison.InvariantCulture))
                return Math.Pow(10, 6);
            if (aSuffix.StartsWith("Г", StringComparison.InvariantCulture))
                return Math.Pow(10, 9);
            return 1;
        }

        private double ParseValue(string aInput)
        {
            Regex regex = new Regex(_regex);
            Match match = regex.Match(aInput);
            if (match.Success)
            {
                var num = Convert.ToUInt32(match.Groups[1].Value);
                var multiplier = ParseMultiplier(match.Groups[2].Value);
                return num * multiplier;
            }
            return 0.0;
        }

        public List<Component> Sort(List<Component> aItems)
        {
            aItems.Sort((x, y) =>
            {
                string nameX = x.Properties[Constants.ComponentName];
                string nameY = y.Properties[Constants.ComponentName];

                if (string.IsNullOrEmpty(nameX))
                    return -1;

                if (string.IsNullOrEmpty(nameY))
                    return 1;

                // Compare 1st letter
                var result = nameX[0].CompareTo(nameY[0]);

                if (result == 0)
                {
                    // Parse value and compare it
                    var num1 = ParseValue(nameX);
                    var num2 = ParseValue(nameY);
                    result = num1.CompareTo(num2);
                }
                return result;
            });

            return aItems;
        }
    }

    static class SortFactory
    {        
        public static ISort<Component> GetSort(SortType aSortType)
        {
            switch (aSortType)
            {
                case SortType.Bill:
                    return new NameSort();
                case SortType.Specification:
                    return new SignSort();
                case SortType.SpecificationOthers:
                    return NameSortRegex.Instance;
            }
            return null;
        }
    }
}
