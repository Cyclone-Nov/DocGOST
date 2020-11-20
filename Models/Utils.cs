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
using GostDOC.Common;
using GostDOC.Interfaces;

namespace GostDOC.Models
{
    static class Utils
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void AddRange<T>(this ObservableCollection<T> col, IEnumerable<T> data) 
        {
            foreach (T val in data)
            {
                col.Add(val);
            }
        }
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
                        return SortType.Name;
                    }
                    if (aGroupName.Equals(Constants.GroupKits))
                    {
                        return SortType.SpKits;
                    }
                    break;
            }
            return sortType;
        }
        
        public static string[] ReadCfgFileLines(string aFileName)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, Constants.Settings, $"{aFileName}.cfg");
            if (File.Exists(filePath))
            {
                return File.ReadAllLines(filePath);
            }
            return new string[] { };
        }

        public static string GetTemplatePath(string aTemplateName)
        {
            return Path.Combine(Environment.CurrentDirectory, Constants.TemplatesFolder, aTemplateName);
        }

        public static string GetGraphValue(IDictionary<string, string> graphs, string name)
        {
            string result;
            graphs.TryGetValue(name, out result);
            return result;
        }
    }

    static class Extensions
    {
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
            if (!string.IsNullOrEmpty(aPropertyName) && current.Properties.ContainsKey(aPropertyName))
            {
                current.Properties[aPropertyName] = aValue;
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

        public static void AddGroup(IDictionary<string, Group> aGroups, string aGroupName)
        {
            Group gp;
            if (!aGroups.TryGetValue(aGroupName, out gp))
            {
                gp = new Group() { Name = aGroupName, SubGroups = new Dictionary<string, Group>() };
                aGroups.Add(aGroupName, gp);
            }
        }

        public static void FillDefaultGroups(this Configuration aCfg)
        {
            AddGroup(aCfg.Specification, Constants.GroupDoc);
            AddGroup(aCfg.Specification, Constants.GroupComplex);
            AddGroup(aCfg.Specification, Constants.GroupAssemblyUnits);
            AddGroup(aCfg.Specification, Constants.GroupDetails);
            AddGroup(aCfg.Specification, Constants.GroupStandard);
            AddGroup(aCfg.Specification, Constants.GroupOthers);
            AddGroup(aCfg.Specification, Constants.GroupMaterials);
            AddGroup(aCfg.Specification, Constants.GroupKits);
        }

        public static void AddGraph(IDictionary<string, string> aGraphs, string aName)
        {
            if (!aGraphs.ContainsKey(aName))
            {
                aGraphs.Add(aName, string.Empty);
            }
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
