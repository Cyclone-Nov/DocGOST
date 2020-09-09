﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using GostDOC.Common;

namespace GostDOC.Models
{
    class XmlManager
    {
        private Converter _defaults = new Converter();
        private RootXml _xml = null;
        private DocType _docType = DocType.None;

        public XmlManager()
        {
        }

        public bool LoadData(Project aResult, string aFilePath, DocType aDocType)
        {
            _xml = new RootXml();
            _docType = aDocType;

            if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref _xml, aFilePath))
            {
                return false;
            }

            string dir = Path.GetDirectoryName(aFilePath);

            // Set project var's
            aResult.Name = _xml.Transaction.Project.Name;
            aResult.Type = ParseProjectType(_xml.Transaction.Type);

            if (aResult.Type == ProjectType.GostDocB && (_docType != DocType.Bill || _docType != DocType.D27))
            {
                return false;
            }

            aResult.Version = _xml.Transaction.Version;
            // Clear configurations
            aResult.Configurations.Clear();
            // Add to specification or not
            bool addToSp = aResult.Type != ProjectType.GostDocB;
            // Fill configurations
            foreach (var cfg in _xml.Transaction.Project.Configurations)
            {
                // Set cfg name
                Configuration newCfg = new Configuration() { Name = cfg.Name };

                // Fill graphs
                foreach (var graph in cfg.Graphs)
                {
                    newCfg.Graphs.Add(graph.Name, graph.Text);
                }

                AddComponents(newCfg, cfg.Documents, ComponentType.Document, dir, addToSp);
                AddComponents(newCfg, cfg.ComponentsPCB, ComponentType.ComponentPCB, dir, addToSp);
                AddComponents(newCfg, cfg.Components, ComponentType.Component, dir, addToSp);

                // Sort components
                SortComponents(newCfg);
                // Fill default graphs
                newCfg.FillDefaultGraphs();
                // Fill default groups
                newCfg.FillDefaultGroups();

                aResult.Configurations.Add(newCfg.Name, newCfg);
            }
            return true;
        }

        public bool SaveData(Project aPrj, string aFilePath)
        {
            if (_xml == null)
            {
                _xml = new RootXml();
            }

            var dt = DateTime.Now;

            _xml.Transaction.Project.Name = aPrj.Name;
            _xml.Transaction.Type = GetProjectType(aPrj.Type);
            _xml.Transaction.Version = aPrj.Version;

            _xml.Transaction.Date = dt.ToString("MM.dd.yyyy");
            _xml.Transaction.Time = dt.ToString("HH:mm:ss");

            _xml.Transaction.Project.Configurations.Clear();

            foreach (var cfg in aPrj.Configurations)
            {
                ConfigurationXml cfgXml = new ConfigurationXml() { Name = cfg.Key };
                
                // Fill graphs
                foreach (var graph in cfg.Value.Graphs)
                {
                    cfgXml.Graphs.Add(new GraphXml() { Name = graph.Key, Text = graph.Value });
                }

                Dictionary<Guid, Component> components = new Dictionary<Guid, Component>();

                // Fill components
                FillComponents(cfg.Value.Specification, components);
                FillComponents(cfg.Value.Bill, components);

                foreach (var component in components.Values)
                {
                    ComponentXml cmp = new ComponentXml();
                    PropertiesToXml(component.Properties, cmp.Properties);
                    for (int i = 0; i < component.Count; i++)
                    {
                        switch (component.Type)
                        {
                            case ComponentType.Document:
                                cfgXml.Documents.Add(cmp);
                                break;
                            case ComponentType.ComponentPCB:
                                cfgXml.ComponentsPCB.Add(cmp);
                                break;
                            default:
                                cfgXml.Components.Add(cmp);
                                break;
                        }
                    }
                }

                _xml.Transaction.Project.Configurations.Add(cfgXml);
            }

            return XmlSerializeHelper.SaveXmlStructFile<RootXml>(_xml, aFilePath);
        }

        private bool IsBillComponent(SubGroupInfo aGroupInfo)
        {
            return aGroupInfo.GroupName == Constants.GroupOthers 
                || aGroupInfo.GroupName == Constants.GroupStandard 
                || aGroupInfo.GroupName == Constants.GroupMaterials;
        }

        private bool ParseAssemblyUnit(string aUnitName, string aDir, Configuration aNewCfg)
        {
            string searchCfg = "-00";

            Regex regex = new Regex(@"\w*(-\d{2})");
            Match match = regex.Match(aUnitName);
            if (match.Success && match.Groups.Count > 0)
            {
                searchCfg = match.Groups[0].Value;
            }

            RootXml xml = new RootXml();
            string filePath = Path.Combine(aDir, aUnitName + ".xml");            
            if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref xml, filePath))
            {
                return false;
            }

            foreach (var cfg in xml.Transaction.Project.Configurations)
            {
                if (cfg.Name == searchCfg)
                {
                    AddComponents(aNewCfg, cfg.ComponentsPCB, ComponentType.ComponentPCB, aDir, false);
                    AddComponents(aNewCfg, cfg.Components, ComponentType.Component, aDir, false);
                    break;
                }
            }
            return true;
        }

        private void AddComponents(Configuration aNewCfg, List<ComponentXml> aComponents, ComponentType aType, string aDir, bool aAddToSp = true)
        {
            Dictionary<CombineProperties, Component> components = new Dictionary<CombineProperties, Component>();
            foreach (var cmp in aComponents)
            {
                var name = cmp.Properties.Find(x => x.Name == Constants.ComponentName);
                var included = cmp.Properties.Find(x => x.Name == Constants.ComponentWhereIncluded);
                if (name == null || included == null) 
                {
                    continue;
                }

                CombineProperties combine = new CombineProperties()
                {
                    Name = name.Text,
                    Included = included.Text
                };

                // Parse component count
                uint count = ParseCount(cmp);

                Component existing = null;
                if (components.TryGetValue(combine, out existing))
                {
                    // If already added - increase count and continue
                    existing.Count += count;
                    continue;
                }

                // Create component
                Component component = new Component(cmp) { Type = aType, Count = count };
                
                // Fill group info
                SubGroupInfo[] groups = UpdateGroups(cmp, component);

                if (aAddToSp)
                {
                    // Add component to specification
                    AddComponent(aNewCfg.Specification, component, groups[0]);
                }

                // Parse assembly units
                if (groups[0].GroupName == Constants.GroupAssemblyUnits)
                {
                    if (_docType == DocType.Bill || _docType == DocType.D27)
                    {
                        string val;
                        if (component.Properties.TryGetValue(Constants.ComponentSign, out val))
                        {
                            ParseAssemblyUnit(val, aDir, aNewCfg);
                        }
                    }
                }

                // Add component to bill
                if (IsBillComponent(groups[0]))
                {
                    AddComponent(aNewCfg.Bill, component, groups[1]);
                }
                // Save added component for counting
                components.Add(combine, component);
            }
        }

        private void AddComponent(IDictionary<string, Group> aGroups, Component aComponent, SubGroupInfo aGroupInfo)
        {
            Group spGroup = null;
            if (!aGroups.TryGetValue(aGroupInfo.GroupName, out spGroup))
            {
                // Add group
                spGroup = new Group() { Name = aGroupInfo.GroupName, SubGroups = new Dictionary<string, Group>() };
                aGroups.Add(spGroup.Name, spGroup);
            }

            if (string.IsNullOrEmpty(aGroupInfo.SubGroupName))
            {
                // Add component, no subgroup
                spGroup.Components.Add(aComponent);
            }
            else
            {
                Group subGroup = null;
                if (!spGroup.SubGroups.TryGetValue(aGroupInfo.SubGroupName, out subGroup))
                {
                    // Add subgroup
                    subGroup = new Group() { Name = aGroupInfo.SubGroupName };
                    spGroup.SubGroups.Add(subGroup.Name, subGroup);
                }
                // Add component to subgroup
                subGroup.Components.Add(aComponent);
            }
        }

        private SubGroupInfo[] UpdateGroups(ComponentXml aSrc, Component aDst)
        {
            SubGroupInfo[] result = new SubGroupInfo[2]
            {
                new SubGroupInfo(),
                new SubGroupInfo()
            };

            string designatorID = string.Empty;
            foreach (var property in aSrc.Properties)
            {
                if (property.Name == Constants.GroupNameSp)
                {
                    result[0].GroupName = property.Text;
                }
                else if (property.Name == Constants.SubGroupNameSp)
                {
                    result[0].SubGroupName = property.Text;
                }
                else if (property.Name == Constants.GroupNameB)
                {
                    result[1].GroupName = property.Text;
                }
                else if (property.Name == Constants.SubGroupNameB)
                {
                    result[1].SubGroupName = property.Text;
                }
                else if (property.Name == Constants.ComponentDesignatiorID)
                {
                    designatorID = property.Text;
                }
            }

            // Set group name from designator ID
            string groupName = _defaults.GetGroupName(designatorID);
            if (!string.IsNullOrEmpty(groupName))
            {
                if (string.IsNullOrEmpty(result[1].GroupName))
                {
                    result[1].GroupName = groupName;
                    aDst.Properties[Constants.GroupNameB] = groupName;
                }

                if (result[0].GroupName == Constants.GroupOthers)
                {
                    if (string.IsNullOrEmpty(result[0].SubGroupName))
                    {
                        result[0].SubGroupName = groupName;
                        aDst.Properties[Constants.SubGroupNameSp] = groupName;
                    }
                    if (string.IsNullOrEmpty(result[1].SubGroupName))
                    {
                        result[1].SubGroupName = groupName;
                        aDst.Properties[Constants.SubGroupNameB] = groupName;
                    }
                }
            }

            return result;
        }

        private void PropertiesToXml(IDictionary<string, string> aSrc, List<PropertyXml> aDst)
        {
            foreach (var prop in aSrc)
            {
                aDst.Add(new PropertyXml() { Name = prop.Key, Text = prop.Value });
            }
        }

        private void FillComponents(IDictionary<string, Group> aSrc, IDictionary<Guid, Component> aDst)
        {
            foreach (var group in aSrc.AsNotNull())
            {
                foreach (var component in group.Value.Components)
                {
                    if (!aDst.ContainsKey(component.Guid))
                    {
                        aDst.Add(component.Guid, component);
                    }
                }
                FillComponents(group.Value.SubGroups, aDst);
            }
        }

        private void SortComponents(Group aGroup, string aGroupName, DocType aDocType)
        {
            foreach (var subGroup in aGroup.SubGroups.AsNotNull())
            {
                // Recursive call for subgroups
                SortComponents(subGroup.Value, aGroupName, aDocType);
            }

            // Sort components
            SortType sortType = Utils.GetSortType(aDocType, aGroupName);
            ISort<Component> sorter = SortFactory.GetSort(sortType);
            if (sorter != null) {
                aGroup.Components = sorter.Sort(aGroup.Components);
            }
        }

        private void SortComponents(Configuration aCfg)
        {
            foreach (var group in aCfg.Specification.Values)
            {
                SortComponents(group, group.Name, DocType.Specification);
            }

            foreach (var group in aCfg.Bill.Values)
            {
                SortComponents(group, group.Name, DocType.Bill);
            }
        }

        private void SetProperty(ComponentXml aComponent, string aName, string aText)
        {
            var property = aComponent.Properties.FirstOrDefault(x => x.Name == aName);
            if (property == null)
            {
                aComponent.Properties.Add(new PropertyXml() { Name = aName, Text = aText });
            }
            else
            {
                property.Text = aText;
            }            
        }

        private string GetProperty(ComponentXml aComponent, string aName)
        {
            var property = aComponent.Properties.FirstOrDefault(x => x.Name == aName);
            return property == null ? string.Empty : property.Text;
        }

        private uint ParseCount(ComponentXml aComponent)
        {
            uint count = 0;
            // Read count if set
            uint.TryParse(GetProperty(aComponent, Constants.ComponentCountDev), out count);
            // Return 1 as default or count if set
            return count > 0 ? count : 1;                
        }

        private ProjectType ParseProjectType(string aType)
        {
            if (aType == Constants.GostDocType)
                return ProjectType.GostDoc;
            if (aType == Constants.GostDocTypeB)
                return ProjectType.GostDocB;
            return ProjectType.Other;
        }

        private string GetProjectType(ProjectType aType)
        {
            if (aType == ProjectType.GostDocB)
                return Constants.GostDocTypeB;
            return Constants.GostDocType;
        }
    }
}
