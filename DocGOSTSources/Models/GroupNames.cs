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

        /// <summary>
        /// Gets the plaural name of the group.
        /// </summary>
        /// <param name="aSingleGroupName">a name.</param>
        /// <returns></returns>
        public static string GetPluralGroupName(string aSingleGroupName)
        {
            if (!string.IsNullOrEmpty(aSingleGroupName))
            {
                string plauralName;
                if (_groupNames.TryGetValue(aSingleGroupName, out plauralName))
                {
                    return plauralName;
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
