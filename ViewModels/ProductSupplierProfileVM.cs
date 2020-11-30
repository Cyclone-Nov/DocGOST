using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class ProductSupplierProfileVM
    {
        /// <summary>
        /// количество комплектов изделия
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public ObservableProperty<int> Quantity { get; } = new ObservableProperty<int>();

        /// <summary>
        /// примечание
        /// </summary>
        /// <value>
        /// 
        /// </value>
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();

        public ProductSupplierProfileVM() : this(1, string.Empty)
        {            
        }

        public ProductSupplierProfileVM(int aQuantity, string aNote)
        {
            Note.Value = aNote;
            Quantity.Value = aQuantity;            
        }
    }
}
