using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class GroupData
    {
        public bool AutoSort { get; set; } = true;
        public IList<Component> Components { get; set; } = new List<Component>();
        public GroupData()
        {
        }
        public GroupData(bool aAutoSort, IList<Component> aComponents)
        {
            AutoSort = aAutoSort;
            Components = aComponents;
        }
    }
}
