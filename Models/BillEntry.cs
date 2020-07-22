using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class BillEntry
    {
        /// <summary>
        /// Наименование
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Код
        /// </summary>
        public string Code { get; set; } 

        /// <summary>
        /// Код
        /// </summary>
        public string Document { get; set; }

        /// <summary>
        /// Код
        /// </summary>
        public string Supplier { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string WhereIncluded { get; set; }

        /// <summary>
        /// Количество на изделие
        /// </summary>
        public string QuantityOnProduct { get; set; }

        /// <summary>
        /// Количество на комплекты
        /// </summary>
        public uint QuantityOnSets { get; set; }

        /// <summary>
        /// Количество на регулир.
        /// </summary>
        public uint QuantityOnReg { get; set; }

        /// <summary>
        /// Всего
        /// </summary>
        public uint Sum { get; set; }

        /// <summary>
        /// Примечание
        /// </summary>
        public string Note { get; set; }
    }
}
