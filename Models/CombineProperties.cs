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
        /// обозначение
        /// </summary>
        /// <value>
        /// The sign
        /// </value>
        public string Sign { get; set; }

        /// <summary>
        /// позиционное обозначение
        /// </summary>
        /// <value>
        /// reference desigantion
        /// </value>
        public string RefDesignation { get; set; }

        /// <summary>
        /// позиция компонента
        /// </summary>
        /// <value>
        /// reference desigantion
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
                return string.Equals(other.Name, Name) && string.Equals(other.Sign, Sign) && string.Equals(other.Position, Position);
            }
            return string.Equals(other.Name, Name) && string.Equals(other.Sign, Sign) && string.Equals(other.RefDesignation, RefDesignation) && string.Equals(other.Position, Position);
        }

        public override bool Equals(object other)
        {
            return Equals(other as CombineProperties);
        }
        public override int GetHashCode()
        {
            if (_combinePosition)
            {
                return (Name?.GetHashCode() ?? 0) ^ (Sign?.GetHashCode() ?? 0);
            }
            return (Name?.GetHashCode() ?? 0) ^ (Sign?.GetHashCode() ?? 0) ^ (RefDesignation?.GetHashCode() ?? 0) ^ (Position?.GetHashCode() ?? 0);
        }
    }
}
