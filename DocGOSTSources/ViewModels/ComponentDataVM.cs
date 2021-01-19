using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class ComponentDataVM
    {
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Data { get; } = new ObservableProperty<string>();

        public ComponentDataVM(string aName, string aData)
        {
            Name.Value = aName;
            Data.Value = aData;
        }
    }
}
