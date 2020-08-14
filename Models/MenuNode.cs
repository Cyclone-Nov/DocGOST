using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class MenuNode
    {
        public string Name { get; set; }
        public MenuNode Parent { get; set; }
        public ObservableCollection<MenuNode> Nodes { get; set; }
    }
}
