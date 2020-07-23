using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GostDOC
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
        public static void AddRange<T>(this HashSet<T> col, IEnumerable<T> data)
        {
            foreach (T val in data)
            {
                col.Add(val);
            }
        }

        public static void AddRange(this ObservableCollection<TreeViewItem> col, IEnumerable<string> data)
        {
            foreach (string val in data)
            {
                col.Add(new TreeViewItem() { Header = val });
            }
        }
    }
}
