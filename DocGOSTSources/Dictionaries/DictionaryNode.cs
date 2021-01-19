using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    enum DictionaryNodeType
    {
        Component,
        Group,
        SubGroup
    }

    class DictionaryNode : IComparable<DictionaryNode>
    {
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<bool> IsSelected { get; } = new ObservableProperty<bool>();
        public DictionaryNodeType Type { get; set; } = DictionaryNodeType.Group;
        public DictionaryNode Parent { get; set; }
        public ObservableCollection<DictionaryNode> Nodes { get; set; }

        public DictionaryNode(string aName, DictionaryNodeType aType = DictionaryNodeType.Group)
        {
            Name.Value = aName;
            Type = aType;
        }

        public int CompareTo(DictionaryNode other)
        {
            return Name.Value.CompareTo(other.Name.Value);
        }
    }
}
