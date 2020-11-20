using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GostDOC.Common;

namespace GostDOC.Models
{
    public class Material
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("note")]
        public string Note { get; set; }
    }

    public class MaterialGroupXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("materials")]
        [XmlArrayItem(typeof(Material), ElementName = "material")]
        public List<Material> Materials { get; set; } = new List<Material>();

        [XmlArray("subgroups")]
        [XmlArrayItem(typeof(MaterialGroupXml), ElementName = "subgroup", IsNullable = true)]
        public List<MaterialGroupXml> Groups { get; set; } = new List<MaterialGroupXml>();
    }

    [XmlRootAttribute("root", IsNullable = false)]
    public class MaterialsXml
    {
        [XmlArray("groups")]
        [XmlArrayItem(typeof(MaterialGroupXml), ElementName = "group")]
        public List<MaterialGroupXml> MaterialGroups { get; set; } = new List<MaterialGroupXml>();
    }

    public class MaterialGroup
    {
        public string Name { get; set; }
        public IDictionary<string, Material> Materials { get; } = new Dictionary<string, Material>();
        public IDictionary<string, MaterialGroup> SubGroups { get; set; }

        public MaterialGroup(string aName)
        {
            Name = aName;
        }
    }

    class MaterialTypes
    {        
        private string _filePath = Path.Combine(Environment.CurrentDirectory, Constants.Settings, "Materials.xml");

        public IDictionary<string, MaterialGroup> Materials { get; } = new Dictionary<string, MaterialGroup>();

        public void Load()
        {
            if (!Import(_filePath))
            {
                foreach (var line in Utils.ReadCfgFileLines(Constants.MaterialGroupsCfg))
                {
                    Materials.Add(line, new MaterialGroup(line));
                }
                Save();
            }
        }

        private void AddGroup(IDictionary<string, MaterialGroup> aGroups, MaterialGroupXml aGroup)
        {
            if (!aGroups.ContainsKey(aGroup.Name))
            {
                var gp = new MaterialGroup(aGroup.Name);
                foreach (var material in aGroup.Materials)
                {
                    gp.Materials.Add(material.Name, material);
                }
                aGroups.Add(aGroup.Name, gp);

                if (aGroup.Groups != null)
                {
                    gp.SubGroups = new Dictionary<string, MaterialGroup>();
                    foreach (var subGroup in aGroup.Groups)
                    {
                        AddGroup(gp.SubGroups, subGroup);
                    }
                }
            }
        }

        private void AddGroup(List<MaterialGroupXml> aGroups, MaterialGroup aGroup)
        {
            MaterialGroupXml gp = new MaterialGroupXml();
            gp.Name = aGroup.Name;
            gp.Materials.AddRange(aGroup.Materials.Values);
            aGroups.Add(gp);

            if (aGroup.SubGroups != null)
            {
                foreach (var group in aGroup.SubGroups)
                {
                    AddGroup(gp.Groups, group.Value);
                }
            }
        }

        public bool Import(string aFilePath)
        {
            MaterialsXml materials = null;
            if (XmlSerializeHelper.LoadXmlStructFile(ref materials, aFilePath))
            {
                foreach (var group in materials.MaterialGroups)
                {
                    AddGroup(Materials, group);
                }
                return true;
            }
            return false;
        }

        public void Save(string aFilePath = null)
        {
            MaterialsXml materials = new MaterialsXml();
            foreach (var kvp in Materials)
            {
                AddGroup(materials.MaterialGroups, kvp.Value);
            }
            XmlSerializeHelper.SaveXmlStructFile(materials, string.IsNullOrEmpty(aFilePath) ? _filePath : aFilePath);
        }

        public bool AddMaterial(string aGroup, string aSubGroup, Material aMaterial)
        {
            MaterialGroup group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                if (!string.IsNullOrEmpty(aSubGroup))
                {
                    if (!group.SubGroups.TryGetValue(aSubGroup, out group))
                    {
                        return false;
                    }
                }
                group.Materials.Add(aMaterial.Name, aMaterial);
                return true;
            }
            return false;
        }

        public bool RemoveMaterial(string aGroup, string aSubGroup, string aMaterialName)
        {
            MaterialGroup group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                if (!string.IsNullOrEmpty(aSubGroup))
                {
                    if (!group.SubGroups.TryGetValue(aSubGroup, out group))
                    {
                        return false;
                    }
                }
                return group.Materials.Remove(aMaterialName);
            }
            return false;
        }

        public bool AddSubGroup(string aGroup, string aSubGroup)
        {
            MaterialGroup group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                if (group.SubGroups == null)
                {
                    group.SubGroups = new Dictionary<string, MaterialGroup>();
                }
                if (!group.SubGroups.ContainsKey(aSubGroup))
                {
                    group.SubGroups.Add(aSubGroup, new MaterialGroup(aSubGroup));
                    return true;
                }
            }
            return false;
        }

        public bool RemoveSubGroup(string aGroup, string aSubGroup)
        {
            MaterialGroup group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                if (group.SubGroups != null)
                {
                    return group.SubGroups.Remove(aSubGroup);
                }
            }
            return false;
        }

        public bool EditSubGroup(string aGroup, string aOldName, string aNewName)
        {
            MaterialGroup group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                if (group.SubGroups != null)
                {
                    MaterialGroup subgroup;
                    if (group.SubGroups.TryGetValue(aOldName, out subgroup))
                    {
                        subgroup.Name = aNewName;
                        group.SubGroups.Remove(aOldName);
                        group.SubGroups.Add(aNewName, subgroup);
                        return true;
                    }
                }
            }
            return false;
        }

        public Material GetMaterial(string aGroup, string aSubGroup, string aName)
        {
            MaterialGroup group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                if (!string.IsNullOrEmpty(aSubGroup))
                {
                    if (!group.SubGroups.TryGetValue(aSubGroup, out group))
                    {
                        return null;
                    }
                }

                Material material;
                if (group.Materials.TryGetValue(aName, out material))
                {
                    return material;
                }
            }
            return null;
        }
    }
}
