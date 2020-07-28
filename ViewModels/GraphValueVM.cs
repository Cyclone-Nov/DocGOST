using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class GraphValueVM
    {
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Text { get; } = new ObservableProperty<string>();

        public GraphValueVM(string aName, string aText)
        {
            Name.Value = aName;
            Text.Value = aText;
        }
    }
}
