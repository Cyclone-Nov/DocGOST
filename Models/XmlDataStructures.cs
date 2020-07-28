using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GostDOC.Common;

namespace GostDOC.Models
{
    public class PropertyXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("value")]
        public string Text { get; set; }
    }

    [XmlRootAttribute("graph", IsNullable = false)]
    public class GraphXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("value")]
        public string Text { get; set; }
    }

    public class ComponentXml
    {
        [XmlArray("properties")]
        [XmlArrayItem(typeof(PropertyXml), ElementName = "property")]
        public List<PropertyXml> Properties { get; set; } = new List<PropertyXml>();
    }

    public class ConfigurationXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("graphs")]
        [XmlArrayItem(typeof(GraphXml), ElementName = "graph")]
        public List<GraphXml> Graphs { get; set; } = new List<GraphXml>();

        [XmlArray("documents")]
        [XmlArrayItem(typeof(ComponentXml), ElementName = "document")]
        public List<ComponentXml> Documents { get; set; } = new List<ComponentXml>();

        [XmlArray("components")]
        [XmlArrayItem(typeof(ComponentXml), ElementName = "component")]
        public List<ComponentXml> Components { get; set; } = new List<ComponentXml>();

        [XmlArray("components_pcb")]
        [XmlArrayItem(typeof(ComponentXml), ElementName = "component_pcb")]
        public List<ComponentXml> ComponentsPCB { get; set; } = new List<ComponentXml>();
    }

    [XmlRootAttribute("project", IsNullable = false)]
    public class ProjectXml
    {
        [XmlAttribute("Project_Name")]
        public string Name { get; set; }

        [XmlAttribute("Project_Path")]
        public string Path { get; set; }

        [XmlArray("configurations")]
        [XmlArrayItem(typeof(ConfigurationXml), ElementName = "configuration")]
        public List<ConfigurationXml> Configurations { get; set; } = new List<ConfigurationXml>();
    }

    [XmlRootAttribute("transaction", IsNullable = false)]
    public class TransactionXml
    {
        [XmlAttribute("Type")]
        public string Type { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("Date")]
        public string Date { get; set; }

        [XmlAttribute("Time")]
        public string Time { get; set; }

        [XmlElement("project")]
        public ProjectXml Project { get; set; }
    }

    [XmlRootAttribute("xml", IsNullable = false)]
    public class RootXml
    {
        [XmlElement("transaction")]
        public TransactionXml Transaction { get; set; }
    }
}
