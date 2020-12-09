using GostDOC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class SupplierProperties
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public int Quantity { get; set; }
        public int AllQuantity { get; set; }
        public bool DomesticManufacturer { get; set; }
        public string FinalSupplier { get; set; }
        public float FinalPrice { get; set; }
        public TaxTypes TaxType { get; set; }
        public float FinalPriceWithTax { get; set; }
        public AcceptanceTypes AcceptanceType { get; set; }
        public uint CountOrdered { get; set; }
        public uint CountWarehouse { get; set; }
        public int CountDeficit { get; set; }
        public int CountIssued { get; set; }
        public int CountBalance { get; set; }
    }
}
