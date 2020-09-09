using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    public class Node
    {
        public string Name { get; set; }
        public NodeType NodeType { get; set; } = NodeType.Root;
        public Node Parent { get; set; }
        public ObservableCollection<Node> Nodes { get; set; } 
    }
}
