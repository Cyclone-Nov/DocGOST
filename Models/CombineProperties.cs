using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class CombineProperties : IEquatable<CombineProperties>
    {
        public string Name { get; set; }
        public string Included { get; set; }
        public bool Equals(CombineProperties other)
        {
            if (other == null)
            {
                return false;
            }
            return other.Name.Equals(Name) && other.Included.Equals(Included);
        }
        public override bool Equals(object other)
        {
            return Equals(other as CombineProperties);
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Included.GetHashCode();
        }

    }
}
