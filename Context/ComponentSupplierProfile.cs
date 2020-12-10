using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class ComponentSupplierProfile
    {
        public int Id { get; set; }
        public SupplierProperties Properties { get; set; } = new SupplierProperties();
        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public ICollection<WarehouseAcceptance> WarehouseAcceptances { get; set; } = new List<WarehouseAcceptance>();
        public ICollection<WarehouseDelivery> WarehouseDeliveries { get; } = new List<WarehouseDelivery>();
        public ICollection<ComponentEntry> ComponentsEntry { get; } = new List<ComponentEntry>();
    }
}
