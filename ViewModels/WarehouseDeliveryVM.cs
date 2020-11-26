using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class WarehouseDeliveryVM
    {
        /// <summary>
        /// дата выачи со склада
        /// </summary>
        /// <value>
        /// The delivery date.
        /// </value>
        public ObservableProperty<string> DeliveryDate { get; } = new ObservableProperty<string>();

        /// <summary>
        /// количество поступившее на склад
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public ObservableProperty<int> Quantity { get; } = new ObservableProperty<int>();

        /// <summary>
        /// кому выдано
        /// </summary>
        /// <value>
        /// 
        /// </value>
        public ObservableProperty<string> WhomWereIssued { get; } = new ObservableProperty<string>();

        public WarehouseDeliveryVM(string aDate, int aQuantity, string WhomIssued)
        {
            DeliveryDate.Value = aDate;
            Quantity.Value = aQuantity;
            WhomWereIssued.Value = WhomIssued;
        }
    }
}
