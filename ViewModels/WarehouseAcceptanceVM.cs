using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class WarehouseAcceptanceVM : BaseChanged
    {
        /// <summary>
        /// дата поступления на склад
        /// </summary>
        /// <value>
        /// The acceptance date.
        /// </value>
        public ObservableProperty<string> AcceptanceDate { get; } = new ObservableProperty<string>();

        /// <summary>
        /// количество поступившее на склад
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public ObservableProperty<int> Quantity { get; } = new ObservableProperty<int>();

        public WarehouseAcceptanceVM(string aData, int aQuantity)
        {
            AcceptanceDate.Value = aData;
            AcceptanceDate.PropertyChanged += PropertyChanged;            
            Quantity.Value = aQuantity;
            Quantity.PropertyChanged += PropertyChanged;
        }

        ~WarehouseAcceptanceVM()
        {
            AcceptanceDate.PropertyChanged -= PropertyChanged;
            Quantity.PropertyChanged -= PropertyChanged;
        }
    }
}
