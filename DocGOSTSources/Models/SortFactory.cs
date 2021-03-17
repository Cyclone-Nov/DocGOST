using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GostDOC.Common;
using SoftCircuits.Collections;

namespace GostDOC.Models
{
    interface ISort<T>
    {
        List<T> Sort(List<T> aItems); 
    }

    class NoSort : ISort<Component>
    {
        public List<Component> Sort(List<Component> aItems) {
            return aItems.ToList();
        }
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
            aItems.Sort((x, y) =>
            {
                string dX = x.GetProperty(Constants.ComponentDesignatorID);
                string dY = y.GetProperty(Constants.ComponentDesignatorID);

                if (string.IsNullOrEmpty(dX))
                    return -1;

                if (string.IsNullOrEmpty(dY))
                    return 1;

                int result = dX.Length.CompareTo(dY.Length);

                // Compare length
                if (result == 0)
                {
                    // Compare strings
                    result = string.Compare(dX, dY);
                }
                return result;
            });
            return aItems;
        }
    }


    class DesignatorIDComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (string.IsNullOrEmpty(x))
                return -1;

            if (string.IsNullOrEmpty(y))
                return 1;

            if (!Common.Converters.SplitDesignatorToStringAndNumber(x, out var dX))
                return -1;

            if (!Common.Converters.SplitDesignatorToStringAndNumber(y, out var dY))
                return 1;

            int result = string.Compare(dX.Item1, dY.Item1);

            if (result == 0)
            {
                return dX.Item2.CompareTo(dY.Item2);
            }

            return result;
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
            StringBuilder unitsGroup = new StringBuilder(@"\w*?(\d+(?:\.\d+)?)\s*(");

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
            if (aSuffix.StartsWith("п", StringComparison.InvariantCulture))
                return Math.Pow(10, -12);
            if (aSuffix.StartsWith("н", StringComparison.InvariantCulture))
                return Math.Pow(10, -9);            
            if (aSuffix.StartsWith("мк", StringComparison.InvariantCulture))
                return Math.Pow(10, -6);
            if (aSuffix.StartsWith("мл", StringComparison.InvariantCulture))
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
            return double.NaN;
        }

        private int CompareFirstLetter(string aFirst, string aSecond)
        {
            if (string.IsNullOrEmpty(aFirst) && string.IsNullOrEmpty(aSecond))
                return 0;

            if (string.IsNullOrEmpty(aFirst))
                return -1;

            if (string.IsNullOrEmpty(aSecond))
                return 1;

            return aFirst[0].CompareTo(aSecond[0]);
        }

        public List<Component> Sort(List<Component> aItems)
        {            
            aItems.Sort((x, y) =>
            {
                string nameX = x.GetProperty(Constants.ComponentName);
                string nameY = y.GetProperty(Constants.ComponentName);

                var num1 = ParseValue(nameX);
                var num2 = ParseValue(nameY);

                if (!double.IsNaN(num1) && !double.IsNaN(num2))
                {
                    var result = num1.CompareTo(num2);
                    if (result != 0)
                        return result;
                } else if(double.IsNaN(num1) && !double.IsNaN(num2))
                {
                    return -1;
                }else if (!double.IsNaN(num1) && double.IsNaN(num2))
                {
                    return 1;
                }

                // не удалось распарсить основные парметры либо они одинаковы - сортируем по алфавиту
                return string.Compare(nameX, nameY, true);
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
            _standards.AddRange(Utils.ReadCfgFileLines("Standards"));
        }
        #endregion

        /// <summary>
        /// парсит тип и параметры стандарта из полного наименования стандарта
        /// </summary>
        /// <param name="aStandardName">полное наименование стандарта</param>
        /// <param name="StandardParams">параметры стандарта: порядковый индекс стандарта из приоритетного списка (0 - самый высокий приоритет), список с частями номера стандарта</param>
        /// <returns><param>true</param> - удалось найти стандарт></returns>
        private bool ParseStandard(string aStandardName, out Tuple<int, List<int>> StandardParams)
        {
            string standardType = string.Empty;
            int standardPriorIndex = -1;
            List<int> standard_numbers = new List<int>();
            bool result = false;

            if (!string.IsNullOrEmpty(aStandardName))
            {
                // выделим тип стандарта
                var arr_standards = _standards.ToArray();
                for (int i = 0; i < arr_standards.Length; i++)
                {
                    // типы стандартов в своем наименовании могут содержать наименования других типов стандартов, 
                    // потому выбор типа будет осуществлять так же по длине строки - чем длинее строка с названием типа, тем более он является подходящим
                    if (aStandardName.Contains(arr_standards[i]))
                    {
                        if (arr_standards[i].Length > standardType.Length)
                        {
                            standardType = arr_standards[i];
                            standardPriorIndex = i;
                        }
                    }
                }

                // если удалось выделить тип стандарта, то выделим его парметры
                if (standardPriorIndex > -1)
                {
                    // примем что имя любого стандарта начинается с типа, потому вырезаем от начала и до длины типа стандарта
                    var std_params = aStandardName.Substring(standardType.Length).Split(new char[] {' ', '-'}, StringSplitOptions.RemoveEmptyEntries);
                    if (std_params != null)
                    {
                        foreach (var part in std_params)
                        {
                            try
                            {
                                int val = Convert.ToInt32(part);
                                standard_numbers.Add(val);
                            } catch { };                            
                        }
                    }
                    result = true;
                }
            }

            StandardParams = new Tuple<int, List<int>>(standardPriorIndex, standard_numbers);
            return result;
        }

        /// <summary>
        /// Gets the name without standard.
        /// </summary>
        /// <param name="aName">a name.</param>
        /// <param name="aStandard">a standard.</param>
        /// <returns></returns>
        private string GetNameWithoutStandardAndType(string aName, string aStandard, string aType)
        {
            string GetName(string aDoc)
            {
                int index = aName.IndexOf(aDoc);
                string firstPart = aName.Substring(0, index);
                string secondPart = aName.Substring(index + aDoc.Length);
                return $"{firstPart}{secondPart}";
            }

            if (aName.IndexOf(aType) == 0)
            {
                aName = aName.Remove(0, aType.Length).TrimStart();
            }

            // проверим не содежится ли все наименование стандарта в имени
            if (aName.Contains(aStandard))
            {
                return GetName(aStandard);
            }
            else
            {
                // иначе разобьем наименование стандарта на части и будем отбрасывать по одной части с конца
                int index = 0;
                string less_standard = aStandard;
                do
                {
                    index = less_standard.LastIndexOf('-');
                    if (index > -1)
                    {
                        less_standard = less_standard.Substring(0, index + 1); // +1 чтобы исключить знак - из названия
                        if (aName.Contains(less_standard))
                        {
                            return GetName(less_standard);
                        }
                    }
                }
                while (index > 0);
            }
            
            return aName;
        }

        private int CompareNames(string aNameX, string aNameY)
        {        
            var separators = new char[] { ' ', '-' };
            var nameArrX = aNameX.Split(separators);
            var nameArrY = aNameY.Split(separators);

            int length = Math.Min(nameArrX.Length, nameArrY.Length);
            if (length > 0)
            {                
                float fvalX, fvalY;                
                for (int i = 0; i < length; i++)
                {
                    fvalX = 0f; fvalY = 0f;
                    try
                    {
                        fvalX = Convert.ToSingle(nameArrX[i]);             
                    }
                    catch {}

                    try
                    {
                        fvalY = Convert.ToSingle(nameArrY[i]);                        
                    } catch {}

                    if (fvalX > fvalY)
                    {
                        return 1;
                    }
                    else if (fvalX < fvalY)
                    {
                        return -1;
                    }
                }
            }

            return String.CompareOrdinal(aNameX, aNameY);
        }


        private Tuple<float, float> ParseParams(string aName)
        {
            float param1 = 0.0f;
            float param2 = 0.0f;
            Regex regex = new Regex(@"\d+([\,]\d+)*([\.]\d+)?x\d+([\,]\d+)*([\.]\d+)?");
            var match = regex.Match(aName);
            if (match.Success)
            {
                var values = match.Value.Split('x');
                param1 = Convert.ToSingle(values[0]);
                param2 = Convert.ToSingle(values[1]);
            }

            return new Tuple<float, float>(param1, param2);
        }

        public List<Component> Sort(List<Component> aItems)
        {
            aItems.Sort((x, y) =>
            {
                // сначала сортируем по типу
                string typeX = x.GetProperty(Constants.ComponentType);
                string typeY = y.GetProperty(Constants.ComponentType);

                if (string.IsNullOrEmpty(typeX))
                {
                    return 1;
                }
                if (string.IsNullOrEmpty(typeY))
                {
                    return -1;
                }
                var result = typeX.CompareTo(typeY);
                if (result == 0)
                {
                    // сортируем по наличию стандартного документа
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

                    // сортируем по типу стандартного документа (относительно индекса типа из файла)
                    bool parseStdX = ParseStandard(docX, out var stdParamsX);
                    bool parseStdY = ParseStandard(docY, out var stdParamsY);
                    
                    if (!parseStdX)
                        return 1;

                    if (!parseStdY)
                        return -1;                    

                    if (stdParamsX.Item1 < stdParamsY.Item1)
                    {
                        return -1;
                    }
                    else if (stdParamsX.Item1 > stdParamsY.Item1)
                    {
                        return 1;
                    }
                    else
                    {
                        // сортируем по номеру стандартного документа:
                        // нмоер может состоять из нескольких частей
                        int length = Math.Min(stdParamsX.Item2.Count, stdParamsY.Item2.Count);
                        for (int i = 0; i < length ;i++)
                        {
                            if (stdParamsX.Item2[i] > stdParamsY.Item2[i])
                            {
                                return 1;
                            }
                            else if (stdParamsX.Item2[i] < stdParamsY.Item2[i])
                            {
                                return -1;
                            }
                        }

                        // сортируем по параметрам компонента либо по наименованию

                        // исключим стандартный документ и тип из наименования 
                        // и в оставшемся наименовании пробуем найти параметры по шаблону D*xD*, D* - число целое или дробное, а x - символом латинского алфавита
                        string nameX = x.GetProperty(Constants.ComponentName);
                        string nameY = y.GetProperty(Constants.ComponentName);

                        string name_without_standardX = GetNameWithoutStandardAndType(nameX, docX, typeX);
                        string name_without_standardY = GetNameWithoutStandardAndType(nameY, docY, typeY);
                        Tuple<float, float> paramsX = ParseParams(name_without_standardX);
                        Tuple<float, float> paramsY = ParseParams(name_without_standardY);

                        if (paramsX.Item1 > paramsY.Item1)
                        {
                            return 1;
                        } else if (paramsX.Item1 < paramsY.Item1)
                        {
                            return -1;
                        } else
                        {
                            if (paramsX.Item2 > paramsY.Item2)
                            {
                                return 1;
                            } else if (paramsX.Item2 < paramsY.Item2)
                            {
                                return -1;
                            } else // эта ситуация возможна когда не удалось ничего распарсить
                            {
                                // перед тем как просто сравним строки, попытаемся разбить на массив и выделить числа
                                //result = String.CompareOrdinal(name_without_standardX, name_without_standardY);
                                result = CompareNames(name_without_standardX, name_without_standardY);
                            }
                        }
                    }
                }

                return result;

            });
            return aItems;
        }
    }

    public class MaterialsSort : ISort<Component>
    {
        private List<string> _groups = new List<string>();

        #region Singleton
        private static readonly Lazy<MaterialsSort> _instance = new Lazy<MaterialsSort>(() => new MaterialsSort(), true);
        public static MaterialsSort Instance => _instance.Value;
        #endregion

        private MaterialsSort()
        {
            foreach (var line in Utils.ReadCfgFileLines(Constants.MaterialGroupsCfg))
            {
                _groups.Add(line);
            }
        }

        public List<Component> Sort(List<Component> aItems)
        {
            aItems.Sort((x, y) =>
            {
                string gpX = x.GetProperty(Constants.ComponentMaterialGroup);
                string gpY = y.GetProperty(Constants.ComponentMaterialGroup);

                if (string.IsNullOrEmpty(gpX))
                {
                    return 1;
                }
                if (string.IsNullOrEmpty(gpY))
                {
                    return -1;
                }

                int result = _groups.IndexOf(gpX).CompareTo(_groups.IndexOf(gpY));
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
                case SortType.SpMaterials:
                    return MaterialsSort.Instance;
                case SortType.Name:
                    return new NameSort();
                case SortType.DesignatorID:
                    return new DesignatorIDSort();
                case SortType.None:
                    return new NoSort();
            }
            return null;
        }
    }
}
