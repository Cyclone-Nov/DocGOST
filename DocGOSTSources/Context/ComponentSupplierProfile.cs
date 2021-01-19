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
        public string ComponentName { get; set; }
        public virtual SupplierProperties Properties { get; set; } = new SupplierProperties();
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public virtual ICollection<WarehouseAcceptance> WarehouseAcceptances { get; set; } = new List<WarehouseAcceptance>();
        public virtual ICollection<WarehouseDelivery> WarehouseDeliveries { get; } = new List<WarehouseDelivery>();
        public virtual ICollection<ComponentEntry> ComponentsEntry { get; } = new List<ComponentEntry>();
    }
}
