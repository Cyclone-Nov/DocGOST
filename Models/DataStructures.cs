using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    public class Component
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public ComponentType Type { get; set; }
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();
        public Component(ComponentXml aComponent)
        {
            Properties.AddRange(aComponent.Properties);
        }
    }

    public class Group
    {
        public string Name { get; set; }
        public Dictionary<Guid, Component> Components { get; } = new Dictionary<Guid, Component>();
        public Dictionary<string, Group> SubGroups { get; set; } 
    }

    public class Configuration
    {
        public string Name { get; set; }
        public Dictionary<string, string> Graphs { get; } = new Dictionary<string, string>();
        public Dictionary<string, Group> Specification { get; set; } = new Dictionary<string, Group>();
        public Dictionary<string, Group> Bill { get; set; } = new Dictionary<string, Group>();
    }

    public class Project
    {
        public string Name { get; set; }
        public Dictionary<string, Configuration> Configurations { get; } = new Dictionary<string, Configuration>();
    }
}
