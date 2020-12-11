using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using GostDOC.Common;
using GostDOC.Models;
using GostDOC.Context;

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
        }
    }
}
