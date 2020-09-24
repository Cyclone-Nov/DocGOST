using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();
        public uint Count { get; set; } = 1;

        public Component(Guid aGuid)
        {
            Guid = aGuid;
        }
        public Component(ComponentXml aComponent)
        {
            Properties.AddRange(aComponent.Properties);
        }
    }

    public class Group
    {
        public string Name { get; set; }
        public bool AutoSort { get; set; } = true;
        public List<Component> Components { get; set; } = new List<Component>();
        public IDictionary<string, Group> SubGroups { get; set; }
    }

    public class Configuration
    {
        public string Name { get; set; }
        public IDictionary<string, string> Graphs { get; } = new Dictionary<string, string>();
        public IDictionary<string, Group> Specification { get; set; } = new Dictionary<string, Group>();
        public IDictionary<string, Group> Bill { get; set; } = new Dictionary<string, Group>();
        public Group D27 { get; set; } = new Group();
    }

    public class Project
    {
        public string Name { get; set; }
        public ProjectType Type { get; set; } = ProjectType.GostDoc;
        public string Version { get; set; }
        public IDictionary<string, Configuration> Configurations { get; } = new Dictionary<string, Configuration>();
    }
}
