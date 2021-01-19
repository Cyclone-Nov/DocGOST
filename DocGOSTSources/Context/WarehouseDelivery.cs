using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class WarehouseDelivery
    {
        public int Id { get; set; }
        public string DeliveryDate { get; set; }
        public int Quantity { get; set; }
        public string WhomWereIssued { get; set; }
    }
}
