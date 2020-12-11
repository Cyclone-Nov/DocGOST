using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.ViewModels
{
    class SupplierPropertiesVM : BaseChanged
    {

        /// <summary>
        /// Производитель
        /// </summary>
        /// <value>
        /// The manufacturer.
        /// </value>
        public ObservableProperty<string> Manufacturer { get; } = new ObservableProperty<string>();
        /// <summary>
        /// количество
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public ObservableProperty<int> Quantity { get; } = new ObservableProperty<int>(0);
        /// <summary>
        /// количество
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        public ObservableProperty<int> AllQuantity { get; } = new ObservableProperty<int>(0);
        
        /// <summary>
        /// Отечественный производитель
        /// </summary>
        /// <value>
        /// The format.
        /// </value>
        public ObservableProperty<bool> DomesticManufacturer { get; } = new ObservableProperty<bool>(true);
        /// <summary>
        /// Итоговый поставщик
        /// </summary>
        /// <value>
        /// The final supplier.
        /// </value>
        public ObservableProperty<string> FinalSupplier { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Итоговая цена, руб
        /// </summary>
        /// <value>
        /// The finel price.
        /// </value>
        public ObservableProperty<float> FinalPrice { get; } = new ObservableProperty<float>(0);
        /// <summary>
        /// Тип НДС
        /// </summary>
        /// <value>
        /// The type of the tax.
        /// </value>
        public ObservableProperty<TaxTypes> TaxType { get; } = new ObservableProperty<TaxTypes>(TaxTypes.Tax20);
        /// <summary>
        /// Итоговая стоимость с учетом НДС
        /// </summary>
        /// <value>
        /// The final price with tax.
        /// </value>
        public ObservableProperty<float> FinalPriceWithTax { get; } = new ObservableProperty<float>(0);

        /// <summary>
        /// категория приемки изделия
        /// </summary>
        /// <value>
        /// The type of the acceptance.
        /// </value>
        public ObservableProperty<AcceptanceTypes> AcceptanceType { get; } = new ObservableProperty<AcceptanceTypes>(AcceptanceTypes.No);
        
        /// <summary>
        /// заказанное количество
        /// </summary>
        /// <value>
        /// The ordered count.
        /// </value>
        public ObservableProperty<uint> CountOrdered { get; } = new ObservableProperty<uint>(0);
        /// <summary>
        /// Поступило на склад
        /// </summary>
        /// <value>
        /// The count reg.
        /// </value>
        public ObservableProperty<uint> CountWarehouse { get; } = new ObservableProperty<uint>(0);
        /// <summary>
        /// дефицит
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public ObservableProperty<int> CountDeficit { get; } = new ObservableProperty<int>(0);
        /// <summary>
        /// выдано со склада
        /// </summary>
        /// <value>
        /// The count issued.
        /// </value>
        public ObservableProperty<int> CountIssued{ get; } = new ObservableProperty<int>(0);
        /// <summary>
        /// Остаток на складе
        /// </summary>
        /// <value>
        /// The count balance.
        /// </value>
        public ObservableProperty<int> CountBalance { get; } = new ObservableProperty<int>(0);

        public SupplierPropertiesVM()
        {
            DomesticManufacturer.PropertyChanged += PropertyChanged;
            FinalSupplier.PropertyChanged += PropertyChanged;
            FinalPrice.PropertyChanged += PropertyChanged;
            TaxType.PropertyChanged += PropertyChanged;
            AcceptanceType.PropertyChanged += PropertyChanged;
            CountOrdered.PropertyChanged += PropertyChanged;
        }

        ~SupplierPropertiesVM()
        {
            DomesticManufacturer.PropertyChanged -= PropertyChanged;
            FinalSupplier.PropertyChanged -= PropertyChanged;
            FinalPrice.PropertyChanged -= PropertyChanged;
            TaxType.PropertyChanged -= PropertyChanged;
            AcceptanceType.PropertyChanged -= PropertyChanged;
            CountOrdered.PropertyChanged -= PropertyChanged;
        }
    }
}
