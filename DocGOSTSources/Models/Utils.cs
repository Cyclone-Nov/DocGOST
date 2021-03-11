using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using GostDOC.Common;
using GostDOC.Interfaces;
using SoftCircuits.Collections;

namespace GostDOC.Models
{
    static class Utils
    {
        private static Random random = new Random();

        /// <summary>
        /// Randoms the string.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        /// <summary>
        /// Gets the type of the sort.
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <param name="aGroupName">Name of a group.</param>
        /// <returns></returns>
        public static SortType GetSortType(DocType aDocType, string aGroupName)
        {
            SortType sortType = SortType.None;
            switch (aDocType)
            {
                case DocType.Bill:
                    sortType = SortType.Name;
                    break;
                case DocType.Specification:
                    if (aGroupName.Equals(Constants.GroupComplex) || aGroupName.Equals(Constants.GroupAssemblyUnits) || aGroupName.Equals(Constants.GroupDetails))
                    {
                        return SortType.SpComplex;
                    }
                    if (aGroupName.Equals(Constants.GroupStandard))
                    {
                        return SortType.SpStandard;
                    }
                    if (aGroupName.Equals(Constants.GroupOthers))
                    {
                        return SortType.SpOthers;
                    }
                    if (aGroupName.Equals(Constants.GroupMaterials))
                    {
                        return SortType.SpMaterials;
                    }
                    if (aGroupName.Equals(Constants.GroupKits))
                    {
                        return SortType.SpKits;
                    }
                    break;
            }
            return sortType;
        }

        /// <summary>
        /// Reads the CFG file lines.
        /// </summary>
        /// <param name="aFileName">Name of a file.</param>
        /// <returns></returns>
        public static string[] ReadCfgFileLines(string aFileName)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, Constants.Settings, $"{aFileName}.cfg");
            if (File.Exists(filePath))
            {
                return File.ReadAllLines(filePath);
            }
            return new string[] { };
        }

        /// <summary>
        /// Gets the template path.
        /// </summary>
        /// <param name="aTemplateName">Name of a template.</param>
        /// <returns></returns>
        public static string GetTemplatePath(string aTemplateName)
        {
            return Path.Combine(Environment.CurrentDirectory, Constants.TemplatesFolder, aTemplateName);
        }

        /// <summary>
        /// Gets the graph value.
        /// </summary>
        /// <param name="graphs">The graphs.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static string GetGraphValue(IDictionary<string, string> graphs, string name)
        {
            string result;
            graphs.TryGetValue(name, out result);
            return result;
        }

        /// <summary>
        /// проверка имени конфигурации на соотвествие формату
        /// </summary>
        /// <param name="aConfigName">имя конфигурации</param>
        /// <returns><param>true</param> - имя имеет верный формат</returns>
        public static bool FormatConfigurationNameIsValid(string aConfigName)
        {   
            Regex regex = new Regex(@"^-\d{2}");
            var matches = regex.Matches(aConfigName);
            if (matches.Count == 1)            
                return true;            
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    static class Extensions
    {
        public static void AddRange<T, V>(this IDictionary<T, V> dst, IDictionary<T, V> src)
        {
            foreach (var val in src)
            {
                dst.Add(val);
            }
        }

        public static void AddRange(this IDictionary<string, string> dic, IList<PropertyXml> data)
        {
            foreach (var val in data)
            {
                dic.Add(val.Name, val.Text);
            }
        }
        public static void AddRange<T, V>(this IDictionary<T, V> dic, OrderedDictionary data)
        {
            foreach (DictionaryEntry val in data)
            {
                dic.Add((T)val.Key, (V)val.Value);
            }
        }
        public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> original)
        {
            return original ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<KeyValuePair<T, V>> AsNotNull<T, V>(this IDictionary<T, V> original)
        {
            return original ?? Enumerable.Empty<KeyValuePair<T, V>>();
        }

        public static IList<object> GetMementos<V>(this ObservableCollection<V> src) where V : IMemento<object>
        {
            List<object> result = new List<object>();
            foreach (var obj in src)
            {
                result.Add(obj.Memento);
            }
            return result;
        }

        public static void AddRange<T>(this ICollection<T> aDst, ICollection<T> aSrc)
        {
            foreach (T item in aSrc)
            {
                aDst.Add(item);
            }
        }

        public static void SetMementos<V>(this ObservableCollection<V> dst, IList<object> src) where V : IMemento<object>, new()
        {
            if (src != null)
            {
                dst.Clear();
                foreach (var mem in src)
                {
                    dst.Add(new V() { Memento = mem });
                }
            }
        }

        public static int IndexOf<T>(this OrderedDictionary dictionary, T key)
        {
            int i = 0;
            foreach (var k in dictionary.Keys)
            {
                if (k.Equals(key))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public static void InsertSorted<T>(this ObservableCollection<T> collection, T item) where T : IComparable<T>
        {
            if (collection.Count == 0)
                collection.Add(item);
            else
            {
                bool last = true;
                for (int i = 0; i < collection.Count; i++)
                {
                    int result = collection[i].CompareTo(item);
                    if (result >= 1)
                    {
                        collection.Insert(i, item);
                        last = false;
                        break;
                    }
                }
                if (last)
                    collection.Add(item);
            }
        }

        public static void UpdateComponentProperties(this Component current, Component update)
        {
            foreach (var prop in update.Properties)
            {
                current.Properties[prop.Key] = prop.Value;
            }
        }
        public static void UpdateComponentGroupInfo(this Component current, DocType aDocType, SubGroupInfo info)
        {
            if (aDocType == DocType.Specification)
            {
                current.Properties[Constants.GroupNameSp] = info.GroupName;
                current.Properties[Constants.SubGroupNameSp] = info.SubGroupName;
            }

            if (aDocType == DocType.Bill)
            {
                current.Properties[Constants.GroupNameB] = info.GroupName;
                current.Properties[Constants.SubGroupNameB] = info.SubGroupName;
            }
        }

        public static string GetProperty(this Component current, string name)
        {
            var prop = current.Properties.FirstOrDefault(x => x.Key == name);
            return prop.Value ?? string.Empty;
        }

        public static uint GetPropertyNum(this Component current, string name)
        {
            var prop = current.Properties.FirstOrDefault(x => x.Key == name);
            uint res = 0;
            uint.TryParse(prop.Value, out res);
            return res;
        }

        public static uint GetCountProperty(this Component current)
        {
            uint count = current.GetPropertyNum(Constants.ComponentCount);
            return count == 0 ? 1 : count;
        }

        public static void SetPropertyValue(this Component current, string aPropertyName, string aValue)
        {
            if (!string.IsNullOrEmpty(aPropertyName))
            {
                if (current.Properties.ContainsKey(aPropertyName))
                {
                    current.Properties[aPropertyName] = aValue;
                }
                else
                {
                    current.Properties.Add(aPropertyName, aValue);
                }
            }
        }

        public static int CompareTo(this Component first, Component second, string propertyName)
        {
            string x = first.GetProperty(propertyName);
            string y = second.GetProperty(propertyName);

            if (string.IsNullOrEmpty(x))
                return -1;

            if (string.IsNullOrEmpty(y))
                return 1;

            // Compare 1st letter
            return x.CompareTo(y);
        }

        public static void AddGroup(IDictionary<string, Group> aGroups, string aGroupName, int aPosition)
        {
            //Group gp;
            //if (!aGroups.TryGetValue(aGroupName, out gp))
            //{
            //    gp = new Group() { Name = aGroupName, SubGroups = new Dictionary<string, Group>() };
            //    aGroups.Add(aGroupName, gp);
            //}

            OrderedDictionary<string, Group> collection = aGroups as OrderedDictionary<string, Group>;
            if (collection != null)
            {
                if (!collection.ContainsKey(aGroupName))
                {
                    var gp = new Group() { Name = aGroupName, SubGroups = new Dictionary<string, Group>() };
                    collection.Insert(aPosition, aGroupName, gp);
                }
                else
                {
                    collection.ReplacePosition(aPosition, aGroupName);
                }
            }
        }

        public static void AddGraph(IDictionary<string, string> aGraphs, string aName)
        {
            if (!aGraphs.ContainsKey(aName))
            {
                aGraphs.Add(aName, string.Empty);
            }
        }

        public static void FillDefaultGroups(this Configuration aCfg)
        {            
            AddGroup(aCfg.Specification, Constants.GroupDoc, 0);
            AddGroup(aCfg.Specification, Constants.GroupComplex, 1);
            AddGroup(aCfg.Specification, Constants.GroupAssemblyUnits, 2);
            AddGroup(aCfg.Specification, Constants.GroupDetails, 3);
            AddGroup(aCfg.Specification, Constants.GroupStandard, 4);
            AddGroup(aCfg.Specification, Constants.GroupOthers, 5);
            AddGroup(aCfg.Specification, Constants.GroupMaterials, 6);
            AddGroup(aCfg.Specification, Constants.GroupKits, 7);         
        }

        public static void FillDefaultGraphs(this Configuration aCfg)
        {
            AddGraph(aCfg.Graphs, Constants.GraphCommentsSp);
            AddGraph(aCfg.Graphs, Constants.GraphCommentsB);
        }

        public static bool EndsWith(this StringBuilder sb, string test)
        {
            if (sb.Length < test.Length)
                return false;

            string end = sb.ToString(sb.Length - test.Length, test.Length);
            return end.Equals(test);
        }
    }
}
