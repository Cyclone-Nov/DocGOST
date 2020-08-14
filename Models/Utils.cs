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

        public static SortType GetSortType(NodeType aNodeType, string aGroupName)
        {
            SortType sortType = SortType.None;
            switch (aNodeType)
            {
                case NodeType.Bill:
                    sortType = SortType.Name;
                    break;
                case NodeType.Specification:
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
            string filePath = Path.Combine(Environment.CurrentDirectory, $"{aFileName}.cfg");
            if (File.Exists(filePath))
            {
                return File.ReadAllLines(filePath);
            }
            return new string[] { };
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
        public static void UpdateComponentGroupInfo(this Component current, NodeType aParentType, SubGroupInfo info)
        {
            if (aParentType == NodeType.Specification)
            {
                current.Properties[Constants.GroupNameSp] = info.GroupName;
                current.Properties[Constants.SubGroupNameSp] = info.SubGroupName;
            }

            if (aParentType == NodeType.Bill)
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
            if (!aGroups.ContainsKey(aGroupName))
            {
                aGroups.Add(aGroupName, new Group() { Name = aGroupName, SubGroups = new Dictionary<string, Group>() });
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

            AddGroup(aCfg.Bill, string.Empty);
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
    }
}
