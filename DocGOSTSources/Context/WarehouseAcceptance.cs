using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class WarehouseAcceptance
    {
        public int Id { get; set; }
        public string AcceptanceDate { get; set; }
        public int Quantity { get; set; }
    }
}
