using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class ElementsEntry
    {
        /// <summary>
        /// Позиционное обозначение
        /// </summary>
        public string PositionDesignation { get; set; }

        /// <summary>
        /// Наименование
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Количество
        /// </summary>
        public uint Quantity { get; set; }

        /// <summary>
        /// Примечание
        /// </summary>
        public string Note { get; set; }

    }
}
