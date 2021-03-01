using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    public class SubGroupInfo : IEquatable<SubGroupInfo>
    {
        /// <summary>
        /// имя группы
        /// </summary>
        /// <value>
        /// имя группы
        /// </value>
        public string GroupName { get; set; } = string.Empty;

        private string subGroupName = string.Empty;
        /// <summary>
        /// имя подгруппы
        /// </summary>
        /// <value>
        /// имя подгруппы
        /// </value>
        public string SubGroupName
        {
            get
            {
                return subGroupName;
            }
            set
            {
                subGroupName = value;
                if (string.IsNullOrEmpty(SubGroupSortName))
                    SubGroupSortName = subGroupName;
            }
        }
        
        /// <summary>
        /// имя подгруппы, по которому можно производить сортировку подгрупп
        /// </summary>
        /// <value>
        /// The name of the sub group sort.
        /// </value>
        public string SubGroupSortName { get; set; } = string.Empty;

        public SubGroupInfo()
        {
        }
        public SubGroupInfo(string aGroupName, string aSubGroupName, string aSubGroupSortName = null)
        {
            if (aSubGroupSortName == null) 
                aSubGroupSortName = aSubGroupName;

            SubGroupSortName = aSubGroupSortName;
            GroupName = aGroupName;
            SubGroupName = aSubGroupName;
        }

        public bool Equals(SubGroupInfo other)
        {
            if (other == null)
            {
                return false;
            }
            return other.GroupName.Equals(GroupName) && other.SubGroupName.Equals(SubGroupName) && other.SubGroupSortName.Equals(SubGroupSortName);
        }

        public override bool Equals(object other)
        {
            return Equals(other as SubGroupInfo);
        }

        public override int GetHashCode()
        {
            return GroupName.GetHashCode() ^ SubGroupName.GetHashCode() ^ SubGroupSortName.GetHashCode();
        }
    }
}
