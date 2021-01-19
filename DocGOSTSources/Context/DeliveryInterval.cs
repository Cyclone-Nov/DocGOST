using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class DeliveryInterval
    {
        public int Id { get; set; }
        public float DeliveryTimeMin { get; set; } = 0;
        public float DeliveryTimeMax { get; set; } = 0;
    }
}
