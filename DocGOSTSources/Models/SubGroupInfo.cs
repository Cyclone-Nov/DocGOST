using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    public class SubGroupInfo : IEquatable<SubGroupInfo>
    {
        public string GroupName { get; set; } = string.Empty;
        public string SubGroupName { get; set; } = string.Empty;

        public SubGroupInfo()
        {
        }
        public SubGroupInfo(string aGroupName, string aSubGroupName)
        {
            GroupName = aGroupName;
            SubGroupName = aSubGroupName;
        }

        public bool Equals(SubGroupInfo other)
        {
            if (other == null)
            {
                return false;
            }
            return other.GroupName.Equals(GroupName) && other.SubGroupName.Equals(SubGroupName);
        }
        public override bool Equals(object other)
        {
            return Equals(other as SubGroupInfo);
        }
        public override int GetHashCode()
        {
            return GroupName.GetHashCode() ^ SubGroupName.GetHashCode();
        }
    }
}
