using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class SupplierVM
    {
        /// <summary>
        /// имя поставщика
        /// </summary>
        /// <value>
        /// The supplier.
        /// </value>
        public ObservableProperty<string> Name { get; set; } = new ObservableProperty<string>();

        /// <summary>
        /// количество
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public ObservableProperty<int> Quantity { get; set; } = new ObservableProperty<int>();

        /// <summary>
        /// цена, руб
        /// </summary>
        /// <value>
        /// The price.
        /// </value>
        public ObservableProperty<float> Price { get; set; } = new ObservableProperty<float>();

        /// <summary>
        /// Тип НДС
        /// </summary>
        /// <value>
        /// The type of the tax.
        /// </value>
        public ObservableProperty<TaxTypes> TaxType { get; set; } = new ObservableProperty<TaxTypes>();
                
        /// <summary>
        /// Итоговая стоимость с учетом НДС
        /// </summary>
        /// <value>
        /// The price with tax.
        /// </value>
        public ObservableProperty<float> PriceWithTax { get; set; } = new ObservableProperty<float>(0);

        /// <summary>
        /// категория приемки изделия
        /// </summary>
        /// <value>
        /// The type of the acceptance.
        /// </value>
        public ObservableProperty<AcceptanceTypes> AcceptanceType { get; } = new ObservableProperty<AcceptanceTypes>(AcceptanceTypes.No);

        /// <summary>
        /// Интервал поставки в неделях
        /// </summary>
        /// <value>
        /// The price with tax.
        /// </value>
        public ObservableProperty<DeliveryInterval> Delivery { get; set; } = new ObservableProperty<DeliveryInterval>();

        /// <summary>
        /// норма упаковки
        /// </summary>
        /// <value>
        /// The supplier.
        /// </value>
        public ObservableProperty<string> Packing { get; set; } = new ObservableProperty<string>();

        /// <summary>
        /// примечание
        /// </summary>
        /// <value>
        /// The supplier.
        /// </value>
        public ObservableProperty<string> Note { get; set; } = new ObservableProperty<string>();

        public SupplierVM(string aName, int aQuantity, float aPrice, TaxTypes aTax, AcceptanceTypes aAcceptance, DeliveryInterval aDelivery, string aPacking, string aNote = "")
        {
            Name.Value = aName;
            Quantity.Value = aQuantity;
            Price.Value = aPrice;
            TaxType.Value = aTax;
            PriceWithTax.Value = Common.Converters.GetPriceWithTax(aPrice, aTax);
            AcceptanceType.Value = aAcceptance;
            Delivery.Value = aDelivery;
            Packing.Value = aPacking;
            Note.Value = aNote;
        }
    }
}
