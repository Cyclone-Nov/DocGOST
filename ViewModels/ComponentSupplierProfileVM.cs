using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class ComponentSupplierProfileVM
    {
        /// <summary>
        /// Gets the supplier properties.
        /// </summary>
        /// <value>
        /// The supplier properties.
        /// </value>
        public SupplierPropertiesVM Properties { get; set; } = new SupplierPropertiesVM();

        /// <summary>
        /// Gets the suppliers.
        /// </summary>
        /// <value>
        /// The suppliers.
        /// </value>
        public ObservableCollection<SupplierVM> Suppliers { get; } = new ObservableCollection<SupplierVM>();

        /// <summary>
        /// Gets the warehouse acceptances.
        /// </summary>
        /// <value>
        /// The warehouse acceptances.
        /// </value>
        public ObservableCollection<WarehouseAcceptanceVM> WarehouseAcceptances { get; } = new ObservableCollection<WarehouseAcceptanceVM>();

        /// <summary>
        /// Gets the warehouse deliveries.
        /// </summary>
        /// <value>
        /// The warehouse deliveries.
        /// </value>
        public ObservableCollection<WarehouseDeliveryVM> WarehouseDeliveries { get; } = new ObservableCollection<WarehouseDeliveryVM>();

        /// <summary>
        /// Gets the warehouse deliveries.
        /// </summary>
        /// <value>
        /// The warehouse deliveries.
        /// </value>
        public ObservableCollection<ComponentEntryVM> ComponentsEntry { get; } = new ObservableCollection<ComponentEntryVM>();


        public ComponentSupplierProfileVM()
        {
            Suppliers.Add(new SupplierVM("дядя вася", 50, 1000.0f, TaxTypes.Tax20, AcceptanceTypes.TCD, new DeliveryInterval() { DeliveryTimeMin = 2, DeliveryTimeMax = 4},"О", "примечание"));
            Suppliers.Add(new SupplierVM("рога и копыта", 50, 999.0f, TaxTypes.Tax20, AcceptanceTypes.MA, new DeliveryInterval() { DeliveryTimeMin = 6, DeliveryTimeMax = 8 }, "Н", "примечание"));

            WarehouseAcceptances.Add(new WarehouseAcceptanceVM("11.12.2020", 50));
            WarehouseAcceptances.Add(new WarehouseAcceptanceVM("21.12.2020", 40));

            WarehouseDeliveries.Add(new WarehouseDeliveryVM("12.12.2020", 40, "Иванов"));
            WarehouseDeliveries.Add(new WarehouseDeliveryVM("22.12.2020", 40, "Петров"));

            ComponentsEntry.Add(new ComponentEntryVM("Изделие\\Муодуль1", 50));
            ComponentsEntry.Add(new ComponentEntryVM("Изделие\\Муодуль2", 40));
        }
    }
}
