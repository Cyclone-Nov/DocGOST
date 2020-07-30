using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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

        public static void AddRange(this Dictionary<string, string> dic, IList<PropertyXml> data)
        {
            foreach (var val in data)
            {
                dic.Add(val.Name, val.Text);
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
    }
}
