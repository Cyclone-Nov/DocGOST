﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GostDOC.Common;

namespace GostDOC.Models
{
    class Material
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("note")]
        public string Note { get; set; }
    }

    class MaterialGroupsXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("materials")]
        [XmlArrayItem(typeof(MaterialGroupsXml), ElementName = "material")]
        public List<Material> Materials { get; set; } = new List<Material>();
    }

    [XmlRootAttribute("root", IsNullable = false)]
    class MaterialsXml
    {
        [XmlArray("groups")]
        [XmlArrayItem(typeof(MaterialGroupsXml), ElementName = "group")]
        public List<MaterialGroupsXml> MaterialGroups { get; set; } = new List<MaterialGroupsXml>();
    }

    class MaterialTypes
    {        
        private string _filePath = Path.Combine(Environment.CurrentDirectory, "Materials.xml");

        public IDictionary<string, IDictionary<string, Material>> Materials { get; } = new Dictionary<string, IDictionary<string, Material>>();

        public bool Load()
        {
            MaterialsXml materials = null;
            if (XmlSerializeHelper.LoadXmlStructFile(ref materials, _filePath))
            {
                foreach (var group in materials.MaterialGroups)
                {
                    IDictionary<string, Material> dic;
                    if (!Materials.TryGetValue(group.Name, out dic))
                    {
                        dic = new Dictionary<string, Material>();
                        Materials.Add(group.Name, dic);
                    }

                    foreach (var material in group.Materials)
                    {
                        dic.Add(material.Name, material);
                    }
                }
                return true;
            }
            else
            {
                Materials.Add("Металлы черные", new Dictionary<string, Material>());
                Materials.Add("Металлы магнитоэлектрические и ферромагнитные", new Dictionary<string, Material>());
                Materials.Add("Металлы цветные, благородные и редкие", new Dictionary<string, Material>());
                Materials.Add("Кабели, провода и шнуры", new Dictionary<string, Material>());
                Materials.Add("Пластмассы и пресс-материалы", new Dictionary<string, Material>());
                Materials.Add("Бумажные и текстильные материалы", new Dictionary<string, Material>());
                Materials.Add("Резиновые и кожевенные материалы", new Dictionary<string, Material>());
                Materials.Add("Минеральные, керамические и стеклянные материалы", new Dictionary<string, Material>());
                Materials.Add("Лаки, краски, нефтепродукты и химикаты", new Dictionary<string, Material>());
                Materials.Add("Металлические, неметаллические порошки", new Dictionary<string, Material>());
                Materials.Add("Прочие материалы", new Dictionary<string, Material>());
            }
            return false;
        }

        public void Save()
        {
            MaterialsXml materials = new MaterialsXml();
            foreach (var kvp in Materials)
            {
                MaterialGroupsXml group = new MaterialGroupsXml() { Name = kvp.Key };
                foreach (var material in kvp.Value)
                {
                    group.Materials.Add(material.Value);
                }
                materials.MaterialGroups.Add(group);
            }

            XmlSerializeHelper.SaveXmlStructFile(materials, _filePath);
        }

        public void AddMaterial(string aGroup, Material aMaterial)
        {
            IDictionary<string, Material> group;
            if (Materials.TryGetValue(aGroup, out group))
            {
                group.Add(aMaterial.Name, aMaterial);
            }
        }
    }
}