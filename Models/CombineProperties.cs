using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class CombineProperties : IEquatable<CombineProperties>
    {
        private bool _combinePosition = false;

        /// <summary>
        /// имя
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// куда входит
        /// </summary>
        /// <value>
        /// The included.
        /// </value>
        public string Included { get; set; }

        /// <summary>
        /// позиционное обозначение
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public string Position { get; set; }

        public CombineProperties(bool aCombinePosition)
        {
            _combinePosition = aCombinePosition;
        }

        public bool Equals(CombineProperties other)
        {
            if (other == null)
            {
                return false;
            }

            if (_combinePosition)
            {
                return string.Equals(other.Name, Name) && string.Equals(other.Included, Included);
            }
            return string.Equals(other.Name, Name) && string.Equals(other.Included, Included) && string.Equals(other.Position, Position);
        }
        public override bool Equals(object other)
        {
            return Equals(other as CombineProperties);
        }
        public override int GetHashCode()
        {
            if (_combinePosition)
            {
                return (Name?.GetHashCode() ?? 0) ^ (Included?.GetHashCode() ?? 0);
            }
            return (Name?.GetHashCode() ?? 0) ^ (Included?.GetHashCode() ?? 0) ^ (Position?.GetHashCode() ?? 0);
        }
    }
}
