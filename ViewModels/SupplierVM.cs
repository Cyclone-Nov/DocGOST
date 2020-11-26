using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

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

        public SupplierVM(string aName, int aQuantity, float aPrice, TaxTypes aTax)
        {
            Name.Value = aName;
            Quantity.Value = aQuantity;
            Price.Value = aPrice;
            TaxType.Value = aTax;
            PriceWithTax.Value = Common.Converters.GetPriceWithTax(aPrice, aTax);

        }
    }
}
