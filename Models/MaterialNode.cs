using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class MaterialNode : IComparable<MaterialNode>
    {
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public MaterialNode Parent { get; set; }
        public ObservableCollection<MaterialNode> Nodes { get; set; }

        public MaterialNode(string aName)
        {
            Name.Value = aName;
        }

        public int CompareTo(MaterialNode other)
        {
            return Name.Value.CompareTo(other.Name.Value);
        }
    }
}
