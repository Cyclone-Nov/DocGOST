using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    static class GroupNames
    {
        static Dictionary<string, string> _groupNames = new Dictionary<string, string>();

        static GroupNames()
        {
            FillGroupNames();
        }
        public static string GetGroupName(string aName)
        {
            if (!string.IsNullOrEmpty(aName))
            {
                string name;
                if (_groupNames.TryGetValue(aName, out name))
                {
                    return name;
                }
            }
            return string.Empty;
        }
        private static void FillGroupNames()
        {
            foreach (var line in Utils.ReadCfgFileLines("GroupNames"))
            {
                string[] split = line.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 2)
                {
                    _groupNames.Add(split[0], split[1]);
                }
            }
        }
    }
}
