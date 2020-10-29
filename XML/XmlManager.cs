using System;
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
        //private Converter _defaults = new Converter();
        private RootXml _xml = null;
        private DocType _docType = DocType.None;
        private string _dir = string.Empty;
        private Group _currentAssemblyD27 = null;

        private ErrorHandler _error = ErrorHandler.Instance;

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

            _dir = Path.GetDirectoryName(aFilePath);

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

                // Set current D27 group
                _currentAssemblyD27 = newCfg.D27;
                _currentAssemblyD27.Name = ParseNameSign(newCfg.Graphs);

                AddComponents(newCfg, cfg.Documents, ComponentType.Document);
                AddComponents(newCfg, cfg.ComponentsPCB, ComponentType.ComponentPCB);
                AddComponents(newCfg, cfg.Components, ComponentType.Component);

                // Move single elements to group "Прочие"
                MoveSingleElements(newCfg);
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

                    SetProperty(cmp, Constants.ComponentCount, component.Count.ToString());
                    
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

        private bool IsD27Component(SubGroupInfo aGroupInfo)
        {
            return aGroupInfo.GroupName == Constants.GroupOthers
                || aGroupInfo.GroupName == Constants.GroupDetails
                || aGroupInfo.GroupName == Constants.GroupStandard
                || aGroupInfo.GroupName == Constants.GroupMaterials;
        }

        private bool IsComplexComponent(SubGroupInfo aGroupInfo)
        {
            return aGroupInfo.GroupName == Constants.GroupAssemblyUnits
                || aGroupInfo.GroupName == Constants.GroupComplex
                || aGroupInfo.GroupName == Constants.GroupKits;
        }

        private bool ParseAssemblyUnit(Configuration aNewCfg, string aUnitName)
        {
            string searchCfg = "-00";

            Regex regex = new Regex(@"\w*(-\d{2})");
            Match match = regex.Match(aUnitName);
            if (match.Success && match.Groups.Count > 0)
            {
                searchCfg = match.Groups[0].Value;
            }

            RootXml xml = new RootXml();
            string filePath = Path.Combine(_dir, aUnitName + ".xml");            
            if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref xml, filePath))
            {
                return false;
            }

            foreach (var cfg in xml.Transaction.Project.Configurations)
            {
                if (cfg.Name == searchCfg)
                {
                    Group newAssembly = new Group() { Name = ParseNameSign(cfg.Graphs) };
                    if (_currentAssemblyD27.SubGroups == null)
                    {
                        _currentAssemblyD27.SubGroups = new Dictionary<string, Group>();
                    }
                    _currentAssemblyD27.SubGroups.Add(newAssembly.Name, newAssembly);
                    _currentAssemblyD27 = newAssembly;

                    AddComponents(aNewCfg, cfg.ComponentsPCB, ComponentType.ComponentPCB);
                    AddComponents(aNewCfg, cfg.Components, ComponentType.Component);
                    break;
                }
            }
            return true;
        }

        private void AddComponents(Configuration aNewCfg, List<ComponentXml> aComponents, ComponentType aType)
        {
            var groupD27 = _currentAssemblyD27;
            Dictionary<CombineProperties, Component> components = new Dictionary<CombineProperties, Component>();
            foreach (var cmp in aComponents)
            {
                var name = cmp.Properties.Find(x => x.Name == Constants.ComponentName);
                var included = cmp.Properties.Find(x => x.Name == Constants.ComponentWhereIncluded);
                var sign = cmp.Properties.Find(x => x.Name == Constants.ComponentSign);
                var position = cmp.Properties.Find(x => x.Name == Constants.ComponentDesignatiorID);

                if (name == null)
                {
                    _error.Error($"Имя компонента не задано!");
                    continue;
                }

                if (included == null && sign == null) 
                {
                    _error.Error($"Компонент {name}: 'Куда входит' или 'Обозначение' не задано!");
                    continue;
                }

                CombineProperties combine = new CombineProperties(_docType == DocType.Specification)
                {
                    Name = name.Text,
                    Included = included?.Text ?? sign.Text,
                    Position = position?.Text ?? string.Empty
                };

                // Parse component count
                uint count = ParseCount(cmp);

                if (CombineComponent(components, combine, count))
                    continue;

                // Create component
                Component component = new Component(cmp) { Type = aType, Count = count };                

                // Fill group info
                SubGroupInfo[] groups = UpdateGroups(cmp, component);

                // Add component to specification
                if (_docType == DocType.Specification || _docType == DocType.ItemsList)
                {
                    AddComponent(aNewCfg.Specification, component, groups[0]);
                }

                // Add component to bill
                if (_docType == DocType.Bill || _docType == DocType.D27)
                {                   
                    // Parse complex components
                    if (IsComplexComponent(groups[0]))
                    {
                        string val;
                        if (component.Properties.TryGetValue(Constants.ComponentSign, out val) && !string.IsNullOrEmpty(val))
                        {
                            ParseAssemblyUnit(aNewCfg, val);
                        }
                    }                    

                    if (IsBillComponent(groups[0]))
                    {
                        // Add to Bill                        
                        if (string.IsNullOrEmpty(groups[1].GroupName))
                        {
                            groups[1].GroupName = groups[0].SubGroupName;
                        }
                        AddComponent(aNewCfg.Bill, component, groups[1]);
                    }

                    if (IsD27Component(groups[0]))
                    {
                        // Add to D27
                        groupD27.Components.Add(component);
                    }

                    // Reset current D27 group
                    _currentAssemblyD27 = groupD27;
                }

                // Save added component for counting
                components.Add(combine, component);
            }            

            if (_docType == DocType.Specification)
            {
                UpdatePositions(components);
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


            var id = aSrc.Properties.Find(x => x.Name == Constants.ComponentDesignatiorID)?.Text ?? string.Empty;
            var name = aSrc.Properties.Find(x => x.Name == Constants.ComponentName)?.Text ?? string.Empty;

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
                else if (property.Name == Constants.ComponentType && result[0].GroupName == Constants.GroupOthers && string.IsNullOrEmpty(result[0].SubGroupName))
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        // Not radio component, set group name from Type field
                        result[0].SubGroupName = property.Text;

                        if (string.IsNullOrEmpty(GroupNames.GetGroupName(property.Text)))
                        {
                            _error.Error($"Элемент типа {id} в словаре GroupNames.cfg не найден!");
                        }
                    }
                }
            }

            if (result[0].GroupName == Constants.GroupOthers && string.IsNullOrEmpty(result[0].SubGroupName))
            {
                _error.Error($"Не задан Подраздел СП для {name}!");
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

        /// <summary>
        /// объединить компоненты при соблюдении условий
        /// </summary>
        /// <param name="aComponents">a components.</param>
        /// <param name="aCombine">a combine.</param>
        /// <param name="aCount">a count.</param>
        /// <returns></returns>
        private bool CombineComponent(Dictionary<CombineProperties, Component> aComponents, CombineProperties aCombine, uint aCount)
        {
            Component existing = null;
            if (_docType == DocType.Specification)
            {
                if (aComponents.TryGetValue(aCombine, out existing))
                {
                    // Update pos
                    string currentPos;
                    if (!string.IsNullOrEmpty(aCombine.Position) && existing.Properties.TryGetValue(Constants.ComponentDesignatiorID, out currentPos))
                    {
                        if (!string.IsNullOrEmpty(currentPos) && currentPos == aCombine.Position)
                        {
                            _error.Error($"Компонент {aCombine.Name}: повторяющееся позиционное обозначение!");
                            return true;
                        }
                        existing.Properties[Constants.ComponentDesignatiorID] = currentPos + "," + aCombine.Position;
                    }

                    // If already added - increase count
                    existing.Count += aCount;

                    return true;
                }
            }
            else if (_docType == DocType.ItemsList) 
            {
                if (aComponents.TryGetValue(aCombine, out existing))
                {
                    // If already added - increase count and continue
                    existing.Count += aCount;
                    return true;
                }
            }
            else
            {
                var keys = aComponents.Keys.ToArray();                
                for (var i = 0; i < keys.Length; i++)
                {                                        
                    if (keys[i].Name == aCombine.Name && keys[i].Included == aCombine.Included)
                    {
                        aComponents[keys[i]].Count += aCount;
                        return true;
                    }
                }
            }

            return false;
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
            uint.TryParse(GetProperty(aComponent, Constants.ComponentCount), out count);
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

        private string ParseGraphValue(IDictionary<string, string> aGraphs, string aName)
        {
            string txt;
            if (aGraphs.TryGetValue(aName, out txt))
            {
                return txt;
            }
            return string.Empty;
        }

        private string ParseGraphValue(IList<GraphXml> aGraphs, string aName)
        {
            string txt = aGraphs.FirstOrDefault(x => x.Name == aName)?.Text;
            if (txt == null)
            {
                return string.Empty;
            }
            return txt;
        }

        private string ParseNameSign(IDictionary<string, string> aGraphs)
        {
            return ParseGraphValue(aGraphs, Constants.GraphName) + " " + ParseGraphValue(aGraphs, Constants.GraphSign);
        }

        private string ParseNameSign(IList<GraphXml> aGraphs)
        {
            return ParseGraphValue(aGraphs, Constants.GraphName) + " " + ParseGraphValue(aGraphs, Constants.GraphSign);
        }

        private Tuple<string, int> ParseDesignatorId(string aInput)
        {
            Regex regex = new Regex(@"(\D*)(\d*)");
            Match match = regex.Match(aInput);
            if (match.Success)
            {
                string s = match.Groups[1].Value;
                int v = int.Parse(match.Groups[2].Value);
                return new Tuple<string, int>(s, v);
            }
            return new Tuple<string, int>(string.Empty, 0);
        }

        private void UpdatePositions(IDictionary<CombineProperties, Component> aComponents)
        {
            foreach (var cmp in aComponents.Values)
            {
                string currentPos;
                if (cmp.Properties.TryGetValue(Constants.ComponentDesignatiorID, out currentPos))
                {                    
                    // Split ids
                    string[] split = currentPos.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (split.Length < 2)
                    {
                        // Not needed to process 1 or 0 values
                        continue;
                    }

                    // Sort ids
                    Array.Sort(split, (x, y) =>
                    {
                        int r = x.Length.CompareTo(y.Length);
                        // Compare length
                        if (r == 0)
                        {
                            // Compare strings
                            r = string.Compare(x, y);
                        }
                        return r;
                    });

                    string result = string.Empty;
                    List<Tuple<string, int>> items = new List<Tuple<string, int>>();
                    for (int i = 0; i < split.Length; i++)
                    {
                        if (items.Count == 0)
                        {
                            // Add current id in list
                            items.Add(ParseDesignatorId(split[i]));
                            continue;
                        }

                        // Get previous and current
                        var p = items.Last();
                        var c = ParseDesignatorId(split[i]);

                        // Add to list
                        items.Add(c);
                        // Compare with previous
                        if (c.Item1 == p.Item1)
                        {
                            if (c.Item2 == p.Item2 + 1)
                            {
                                if (i != split.Length - 1)
                                {
                                    // Skip list processing
                                    continue;
                                }
                            }
                        }

                        if (items.Count > 2)
                        {
                            // Add ids with "-"
                            var f = items.First();
                            var l = items.Last();

                            if (!string.IsNullOrEmpty(result))
                            {
                                result += ", ";
                            }
                            result += f.Item1 + f.Item2.ToString() + "-" + l.Item1 + l.Item2.ToString();
                        }
                        else
                        {
                            // Add ids with ","
                            foreach (var item in items)
                            {
                                if (!string.IsNullOrEmpty(result) && !result.EndsWith(", "))
                                {
                                    result += ", ";
                                }
                                result += item.Item1 + item.Item2.ToString();
                            }
                        }
                        // Clear ids 
                        items.Clear();
                    }

                    // Save updated id
                    cmp.Properties[Constants.ComponentDesignatiorID] = result;
                }
            }
        }


        private void MoveSingleElements(Lazy<Group> aOthers, IDictionary<string, Group> aGroups)
        {
            foreach (var gp in aGroups.AsNotNull().ToList())
            {
                MoveSingleElements(aOthers, gp.Value.SubGroups);

                if (gp.Value.Components.Count == 1)
                {
                    aOthers.Value.Components.AddRange(gp.Value.Components);
                    aGroups.Remove(gp.Key);
                }

                if (gp.Value.Components.Count == 0 && (gp.Value.SubGroups == null || gp.Value.SubGroups.Count == 0))
                {
                    aGroups.Remove(gp.Key);
                }
            }
        }

        private void MoveSingleElements(Group aGroup)
        {
            foreach (var gp in aGroup.SubGroups.AsNotNull().ToList())
            {
                if (gp.Value.Components.Count == 1)
                {
                    // Update component name
                    Component cp = gp.Value.Components.First();
                    string type = cp.GetProperty(Constants.ComponentType);
                    string name = cp.GetProperty(Constants.ComponentName);
                    cp.SetPropertyValue(Constants.ComponentName, name);//type + " " + name);
                    // Move component to parent group
                    aGroup.Components.Add(cp);
                    // Remove subgroup
                    aGroup.SubGroups.Remove(gp.Key);
                }
            }
        }

        private void MoveSingleElements(Configuration aCfg)
        {
            if (_docType == DocType.Bill)
            {
                Lazy<Group> others = new Lazy<Group>(() =>
                {
                    Group groupOthers;
                    if (!aCfg.Bill.TryGetValue(Constants.GroupOthersB, out groupOthers))
                    {
                        groupOthers = new Group() { Name = Constants.GroupOthersB };
                        aCfg.Bill.Add(groupOthers.Name, groupOthers);
                    }
                    return groupOthers;
                });

                MoveSingleElements(others, aCfg.Bill);
            }
            else if (_docType == DocType.Specification)
            {
                foreach (var gp in aCfg.Specification)
                {
                    MoveSingleElements(gp.Value);
                }
            }
        }
    }
}
