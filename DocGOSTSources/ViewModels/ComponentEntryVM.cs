using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class ComponentEntryVM
    {
        public ObservableProperty<string> Entry { get; } = new ObservableProperty<string>();
        public ObservableProperty<int> Count { get; } = new ObservableProperty<int>();

        public ComponentEntryVM(string aEntry, int aCount)
        {
            Entry.Value = aEntry;
            Count.Value = aCount;
        }
    }
}
