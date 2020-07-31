using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using GostDOC.Common;

namespace GostDOC.Models
{
    class XmlManager
    {
        private Converter _defaults = new Converter();

        public XmlManager()
        {
        }

        public bool LoadData(Project aResult, string[] aFiles, string aMainFile)
        {
            RootXml rootXml = null;
            if (aFiles.Length > 1)
            {
                // Merge files to one
                rootXml = MergeFiles(aFiles, aMainFile);
            }
            else if (aFiles.Length == 1)
            {
                XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref rootXml, aFiles[0]);
            }       
            
            if (rootXml == null)
            {
                return false;
            }

            // Set project name
            aResult.Name = rootXml.Transaction.Project.Name;
            // Clear configurations
            aResult.Configurations.Clear();

            // Fill configurations
            foreach (var cfg in rootXml.Transaction.Project.Configurations)
            {
                // Set cfg name
                Configuration newCfg = new Configuration() { Name = cfg.Name };

                // Fill graphs
                foreach (var graph in cfg.Graphs)
                {
                    newCfg.Graphs.Add(graph.Name, graph.Text);
                }

                // Fill groups
                foreach (var doc in cfg.Documents)
                {
                    AddComponent(newCfg, doc, ComponentType.Document);
                }
                foreach (var doc in cfg.ComponentsPCB)
                {
                    AddComponent(newCfg, doc, ComponentType.ComponentPCB);
                }
                foreach (var component in cfg.Components)
                {
                    AddComponent(newCfg, component, ComponentType.Component);
                }

                aResult.Configurations.Add(newCfg.Name, newCfg);
            }
            return true;
        }

        private void AddComponent(Configuration aNewCfg, ComponentXml aComponent, ComponentType aType)
        {
            string[] groups = GetGroups(aComponent);

            var component = new Component(aComponent) { Type = aType };
            AddComponent(aNewCfg.Specification, component, groups[0], groups[1]);
            AddComponent(aNewCfg.Bill, component, groups[2], groups[3]);
        }

        private void AddComponent(IDictionary<string, Group> aGroups, Component aComponent, string aGroup, string aSubGroup)
        {
            Group spGroup = null;
            if (!aGroups.TryGetValue(aGroup, out spGroup))
            {
                // Add group
                spGroup = new Group() { Name = aGroup, SubGroups = new Dictionary<string, Group>() };
                aGroups.Add(spGroup.Name, spGroup);
            }

            if (string.IsNullOrEmpty(aSubGroup))
            {
                // Add component, no subgroup
                spGroup.Components.Add(aComponent);
            }
            else
            {
                Group subGroup = null;
                if (!spGroup.SubGroups.TryGetValue(aSubGroup, out subGroup))
                {
                    // Add subgroup
                    subGroup = new Group() { Name = aSubGroup };
                    spGroup.SubGroups.Add(subGroup.Name, subGroup);
                }
                // Add component to subgroup
                subGroup.Components.Add(aComponent);
            }
        }

        private string[] GetGroups(ComponentXml aComponent)
        {
            string[] result = Enumerable.Repeat(string.Empty, 4).ToArray();

            string designatorID = string.Empty;
            foreach (var property in aComponent.Properties)
            {
                if (property.Name == Constants.GroupNameSp)
                {
                    result[0] = property.Text;
                }
                else if (property.Name == Constants.SubGroupNameSp)
                {
                    result[1] = property.Text;
                }
                else if (property.Name == Constants.GroupNameB)
                {
                    result[2] = property.Text;
                }
                else if (property.Name == Constants.SubGroupNameB)
                {
                    result[3] = property.Text;
                }
                else if (property.Name == Constants.ComponentDesignatiorID)
                {
                    designatorID = property.Text;
                }
            }

            if (!string.IsNullOrEmpty(designatorID))
            {
                string subGroupName = _defaults.GetGroupName(designatorID);                
                if (result[0] == Constants.GroupOthers && string.IsNullOrEmpty(result[1]))
                {
                    result[1] = subGroupName;
                }
                if (result[2] == Constants.GroupOthers && string.IsNullOrEmpty(result[3]))
                {
                    result[3] = subGroupName;
                }
            }

            return result;
        }

        private RootXml MergeFiles(string[] aFiles, string aMainFile)
        {
            RootXml rootXml = null;

            // Find main file
            List<string> otherFiles = new List<string>();
            foreach (var file in aFiles)
            {
                if (file.EndsWith(aMainFile))
                {
                    if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref rootXml, file))
                    {
                        return null;
                    }
                }
                else
                {
                    otherFiles.Add(file);
                }
            }

            // Read all components and write them to main file
            foreach (var file in otherFiles)
            {
                RootXml otherRootXml = null;
                if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref otherRootXml, file))
                {
                    continue;
                }
                
                foreach (var cfg in otherRootXml.Transaction.Project.Configurations)
                {
                    foreach (var mainCfg in rootXml.Transaction.Project.Configurations)
                    {
                        if (mainCfg.Name == cfg.Name)
                        {
                            mainCfg.Components.AddRange(cfg.Components);
                            break;
                        }
                    }
                }
            }
            return rootXml;
        }
    }
}
