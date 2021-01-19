using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class MoveInfo
    {
        public SubGroupInfo Source { get; set; } = new SubGroupInfo();
        public SubGroupInfo Destination { get; set; } = new SubGroupInfo();
        public List<Component> Components { get; set; } = new List<Component>();
    }
}
