using GostDOC.Common;
using GostDOC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public float Price { get; set; }
        public TaxTypes TaxType { get; set; }
        public float PriceWithTax { get; set; }
        public AcceptanceTypes AcceptanceType { get; set; }
        public DeliveryInterval Delivery { get; set; }
        public string Packing { get; set; }
        public string Note { get; set; }
    }
}
