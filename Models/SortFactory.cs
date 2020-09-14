﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
            return aItems.OrderBy(x => x.GetProperty(Constants.ComponentName)).ToList();
        }
    }

    class SignSort : ISort<Component>
    {
        public List<Component> Sort(List<Component> aItems)
        {
            return aItems.OrderBy(x => x.GetProperty(Constants.ComponentSign)).ToList();
        }
    }

    class NameSignSort : ISort<Component>
    {
        public List<Component> Sort(List<Component> aItems)
        {
            return aItems.OrderBy(x => x.GetProperty(Constants.ComponentName)).ThenBy(x => x.GetProperty(Constants.ComponentSign)).ToList();
        }
    }

    class DesignatorIDSort : ISort<Component>
    {
        public List<Component> Sort(List<Component> aItems)
        {
            return aItems.OrderBy(x => x.Count).ThenBy(x => x.GetProperty(Constants.ComponentDesignatiorID)).ToList();
        }
    }    

    class NameSortRegex : ISort<Component>
    {
        private string _regex = string.Empty;

        #region Singleton
        private static readonly Lazy<NameSortRegex> _instance = new Lazy<NameSortRegex>(() => new NameSortRegex(), true);
        public static NameSortRegex Instance => _instance.Value;
        private NameSortRegex()
        {
            StringBuilder unitsGroup = new StringBuilder(@"\w*?(\d+(?:\.\d+)?)\w*\s*(");

            bool first = true;
            foreach (var line in Utils.ReadCfgFileLines("Units"))
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
                var num = double.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                var multiplier = ParseMultiplier(match.Groups[2].Value);
                return num * multiplier;
            }
            return 0.0;
        }

        public List<Component> Sort(List<Component> aItems)
        {            
            aItems.Sort((x, y) =>
            {
                string nameX = x.GetProperty(Constants.ComponentName);
                string nameY = y.GetProperty(Constants.ComponentName);

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

    class StandardSort : ISort<Component>
    {
        List<string> _standards = new List<string>();

        #region Singleton
        private static readonly Lazy<StandardSort> _instance = new Lazy<StandardSort>(() => new StandardSort(), true);
        public static StandardSort Instance => _instance.Value;
        private StandardSort()
        {
            _standards.AddRange(Utils.ReadCfgFileLines("Standard"));
        }
        #endregion
        
        private string ParseStandard(string aName)
        {
            foreach (var str in _standards)
            {
                if (aName.Contains(str))
                {
                    return str;
                }
            }
            return string.Empty;
        }

        public List<Component> Sort(List<Component> aItems)
        {
            aItems.Sort((x, y) =>
            {
                string docX = x.GetProperty(Constants.ComponentDoc);
                string docY = y.GetProperty(Constants.ComponentDoc);

                if (string.IsNullOrEmpty(docX))
                {
                    return 1;
                }
                if (string.IsNullOrEmpty(docY))
                {
                    return -1;
                }             

                var result = ParseStandard(docX).CompareTo(ParseStandard(docY));
                if (result == 0)
                {
                    result = x.CompareTo(y, Constants.ComponentName);
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
                case SortType.SpComplex:
                    return new SignSort();
                case SortType.SpStandard:
                    return StandardSort.Instance;
                case SortType.SpOthers:
                    return NameSortRegex.Instance;
                case SortType.SpKits:
                    return new NameSignSort();
                case SortType.Name:
                    return new NameSort();
                case SortType.DesignatorID:
                    return new DesignatorIDSort();
            }
            return null;
        }
    }
}
