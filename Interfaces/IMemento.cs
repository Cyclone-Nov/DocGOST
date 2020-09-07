using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Interfaces
{
    interface IMemento<T>
    {
        T Memento { get; set; }
    }
}
