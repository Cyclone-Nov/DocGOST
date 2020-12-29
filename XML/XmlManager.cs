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
using iText.StyledXmlParser;

namespace GostDOC.Models
{
    class XmlManager
    {
        private Version MinVersion = new Version(1, 2);

        private RootXml _xml = null;
        private DocType _docType = DocType.None;
        private ProjectType _projectType = ProjectType.Other;
        private string _dir = string.Empty;
        private Group _currentAssemblyD27 = null;

        private ErrorHandler _error = ErrorHandler.Instance;

        public XmlManager()
        {
        }

        public void Reset()
        {
            _xml = null;
            _docType = DocType.None;
            _projectType = ProjectType.Other;
            _dir = string.Empty;
            _currentAssemblyD27 = null;
        }

        public OpenFileResult LoadData(Project aResult, string aFilePath, DocType aDocType)
        {
            _xml = new RootXml();
            _docType = aDocType;

            if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref _xml, aFilePath))
            {
                return OpenFileResult.Fail;
            }

            if (!CheckVersion(aFilePath))
                return OpenFileResult.Fail;

            _dir = Path.GetDirectoryName(aFilePath);
            _projectType = ParseProjectType(_xml.Transaction.Type);

            // Set project var's
            aResult.Name = _xml.Transaction.Project.Name;
            aResult.Type = _projectType;

            if (aDocType == DocType.Bill)
            {
                aResult.Type = ProjectType.GostDocB;
            }

            if (aResult.Type == ProjectType.GostDocB && _docType != DocType.Bill && _docType != DocType.D27)
            {
                return OpenFileResult.FileFormatError;
            }

            string unitSign = string.Empty;

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
                    if(newCfg.Graphs.ContainsKey(graph.Name))
                        _error.Error($"Обнаружено дублирование графа {graph.Name}. Будет использован первый");
                    else
                        newCfg.Graphs.Add(graph.Name, graph.Text);
                }

                if(string.IsNullOrEmpty(unitSign))
                    unitSign = ParseGraphValue(newCfg.Graphs, Constants.GraphSign);

                // Set current D27 group
                _currentAssemblyD27 = newCfg.D27;
                _currentAssemblyD27.Name = ParseNameSign(newCfg.Graphs);

                AddComponents(newCfg, cfg.Documents, ComponentType.Document, unitSign);
                if(_docType != DocType.Bill)
                    AddComponents(newCfg, cfg.ComponentsPCB, ComponentType.ComponentPCB, unitSign);
                AddComponents(newCfg, cfg.Components, ComponentType.Component, unitSign);

                if (aDocType == DocType.ItemsList)
                {
                    if (!HasASсhema(newCfg))
                    {
                        string config_name = (cfg.Name == "-00" ) ? "Базовое исполнение" : $"Исполнение {cfg.Name}";
                        _error.Error($"{config_name}: не найдена схема в разделе документации при импорте перечня элементов");
                    }
                }

                // Move single elements to group "Прочие" and update group names
                ProcessGroupNames(newCfg);
                // Sort components
                SortComponents(newCfg);
                // Fill default graphs
                newCfg.FillDefaultGraphs();
                // Fill default groups
                newCfg.FillDefaultGroups();
                // add two empty components for every group in specification
                if (aDocType == DocType.Specification && _projectType == ProjectType.Other)
                    AddEmptyComponentsToSpecificationGroups(newCfg);

                aResult.Configurations.Add(newCfg.Name, newCfg);
            }
            return OpenFileResult.Ok;
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

        private bool ParseAssemblyUnit(Configuration aNewCfg, string aUnitSign, string aUnitName, string aTopSpecFile, uint complexCount)
        {
            string searchCfg = Constants.MAIN_CONFIG_INDEX;

            Regex regex = new Regex(@"-\d{2}");
            Match match = regex.Match(aUnitSign);
            if (match.Success && match.Groups.Count > 0)
            {
                searchCfg = match.Groups[0].Value;
                aUnitSign = aUnitSign.Remove(match.Groups[0].Index);
            }

            RootXml xml = new RootXml();
            string filePath = Path.Combine(_dir, aUnitSign + ".xml");
            if (!XmlSerializeHelper.LoadXmlStructFile<RootXml>(ref xml, filePath))
            {
                _error.Error($"Ошибка загрузки файла {filePath}!");
                return false;
            }

            if (!CheckVersion(filePath))
                return false;

            string nameSignInTopSpec = ConcatNameSign(aUnitName, aUnitSign);

            string name = string.Empty;
            string sign = string.Empty;
            foreach (var cfg in xml.Transaction.Project.Configurations)
            {
                if (cfg.Name == Constants.MAIN_CONFIG_INDEX)
                {
                    name = ParseGraphValue(cfg.Graphs, Constants.GraphName);
                    sign = ParseGraphValue(cfg.Graphs, Constants.GraphSign);
                }                

                if (cfg.Name == searchCfg)
                {
                    string nameSignInThisSpec = ConcatNameSign(name, sign);
                    if (!string.Equals(nameSignInTopSpec, nameSignInThisSpec, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _error.Error($"Имя или обозначение узла ({nameSignInTopSpec}) из файла спецификации {aTopSpecFile} " +
                                     $"не соответсвует имени или обозначению ({nameSignInThisSpec}) в его спецификации ({aUnitSign}.xml)!");
                    }
                    string fullSign = string.Equals(searchCfg, Constants.MAIN_CONFIG_INDEX) ? sign : $"{sign}{searchCfg}";
                    Group newAssembly = new Group() { Name = ConcatNameSign(name, fullSign)  }; //nameSignInTopSpec
                    if (_currentAssemblyD27.SubGroups == null)
                    {
                        _currentAssemblyD27.SubGroups = new Dictionary<string, Group>();
                    }

                    if (_currentAssemblyD27.SubGroups.ContainsKey(newAssembly.Name))
                    {
                        _currentAssemblyD27 = _currentAssemblyD27.SubGroups[newAssembly.Name];
                    } else
                    {
                        _currentAssemblyD27.SubGroups.Add(newAssembly.Name, newAssembly);
                        _currentAssemblyD27 = newAssembly;
                    }                    

                    if (_docType != DocType.Bill)
                        AddComponents(aNewCfg, cfg.ComponentsPCB, ComponentType.ComponentPCB, aUnitSign, complexCount);
                    AddComponents(aNewCfg, cfg.Components, ComponentType.Component, aUnitSign, complexCount);
                    break;
                }
            }
            return true;
        }

        private void AddComponents(Configuration aNewCfg, List<ComponentXml> aComponents, ComponentType aType, string aSpecFileName, uint complexCount = 1)
        {
            var groupD27 = _currentAssemblyD27;
            Dictionary<CombineProperties, Component> components = new Dictionary<CombineProperties, Component>();
            HashSet<string> positions = new HashSet<string>();
            string _specFileName = aSpecFileName + ".xml";            

            foreach (var cmp in aComponents)
            {
                var name = cmp.Properties.Find(x => x.Name == Constants.ComponentName);
                var sign = cmp.Properties.Find(x => x.Name == Constants.ComponentSign);
                var designator = cmp.Properties.Find(x => x.Name == Constants.ComponentDesignatorID);
                var position = cmp.Properties.Find(x => x.Name == Constants.ColumnPosition);                

                if (name == null && position == null)
                {
                    _error.Error($"Файл {_specFileName}: имя компонента не задано!");
                    continue;
                }

                if (sign == null)
                {
                    _error.Error($"Файл {_specFileName}. Компонент {name}: 'Обозначение' не задано!");
                    continue;
                }

                CombineProperties combine = new CombineProperties(_docType == DocType.Specification)
                {
                    Name = name.Text,
                    Sign = sign.Text,
                    RefDesignation = designator?.Text ?? string.Empty,
                    Position = position?.Text ?? string.Empty,                    
                };

                if (!string.IsNullOrEmpty(combine.RefDesignation))
                {
                    if (positions.Contains(combine.RefDesignation))
                    {
                        _error.Error($"Найдено дублирующееся позиционное обозначение {combine.RefDesignation}!");
                    } else
                    {
                        positions.Add(combine.RefDesignation);
                    }
                }

                // Parse component count
                uint count = ParseCount(cmp) * complexCount;

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
                        string _sign;
                        if (component.Properties.TryGetValue(Constants.ComponentSign, out _sign) && !string.IsNullOrEmpty(_sign))
                        {
                            string _name;
                            component.Properties.TryGetValue(Constants.ComponentName, out _name);
                            ParseAssemblyUnit(aNewCfg, _sign.Trim(new char[] { ' ' }), _name, _specFileName, component.Count);
                        }
                    }

                    if (IsBillComponent(groups[0]) && _docType == DocType.Bill)
                    {
                        // Add to Bill                        
                        if (string.IsNullOrEmpty(groups[1].GroupName))
                        {
                            groups[1].GroupName = groups[0].SubGroupName;
                        }
                        AddComponent(aNewCfg.Bill, component, groups[1]);
                    }

                    if (IsD27Component(groups[0]) && _docType == DocType.D27)
                    {
                        AddComponent(groupD27, component);
                        //groupD27.Components.Add(component);
                    }

                    // Reset current D27 group
                    _currentAssemblyD27 = groupD27;
                }

                // Save added component for counting
                components.Add(combine, component);
            }

            if (_docType == DocType.Specification)
            {
                //UpdateAutoSort(enableAutoSort);
                UpdateDesignators(components);
            }
        }

        private void AddComponent(IDictionary<string, Group> aGroups, Component aComponent, SubGroupInfo aGroupInfo)
        {            
            if (!aGroups.TryGetValue(aGroupInfo.GroupName, out var spGroup))
            {
                // Add group
                spGroup = new Group() { Name = aGroupInfo.GroupName, SubGroups = new Dictionary<string, Group>() };
                aGroups.Add(spGroup.Name, spGroup);
            }

            if (string.IsNullOrEmpty(aGroupInfo.SubGroupName))
            {
                // Add component, no subgroup                
                AddComponent(spGroup, aComponent);
            } else
            {                
                if (!spGroup.SubGroups.TryGetValue(aGroupInfo.SubGroupName, out var subGroup))
                {
                    // Add subgroup
                    subGroup = new Group() { Name = aGroupInfo.SubGroupName };
                    spGroup.SubGroups.Add(subGroup.Name, subGroup);
                }
                // Add component to subgroup
                AddComponent(subGroup, aComponent);
            }
        }

        private SubGroupInfo[] UpdateGroups(ComponentXml aSrc, Component aDst)
        {
            SubGroupInfo[] result = new SubGroupInfo[2]
            {
                new SubGroupInfo(),
                new SubGroupInfo()
            };

            var id = aSrc.Properties.Find(x => x.Name == Constants.ComponentDesignatorID)?.Text ?? string.Empty;
            var name = aSrc.Properties.Find(x => x.Name == Constants.ComponentName)?.Text ?? string.Empty;
            var included = aSrc.Properties.Find(x => x.Name == Constants.ComponentWhereIncluded)?.Text ?? string.Empty;

            foreach (var property in aSrc.Properties)
            {
                if (property.Name == Constants.GroupNameSp)
                {
                    result[0].GroupName = property.Text;
                } else if (property.Name == Constants.SubGroupNameSp)
                {
                    result[0].SubGroupName = property.Text;
                } else if (property.Name == Constants.GroupNameB)
                {
                    result[1].GroupName = property.Text;
                } else if (property.Name == Constants.SubGroupNameB)
                {
                    result[1].SubGroupName = property.Text;
                }
            }

            if (result[0].GroupName == Constants.GroupOthers && string.IsNullOrEmpty(result[0].SubGroupName))
            {
                _error.Error($"Не задан Подраздел СП для раздела {result[0].GroupName} в файле {included} компонента {name}!");
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

            aGroup.AutoSort = CheckEnableAutoSort(aGroup.Components);
            if (aGroup.AutoSort)
            {
                // Sort components
                SortType sortType = Utils.GetSortType(aDocType, aGroupName);
                ISort<Component> sorter = SortFactory.GetSort(sortType);
                if (sorter != null)
                {
                    aGroup.Components = sorter.Sort(aGroup.Components);
                }
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
                    if (!string.IsNullOrEmpty(aCombine.RefDesignation) && existing.Properties.TryGetValue(Constants.ComponentDesignatorID, out currentPos))
                    {
                        if (!string.IsNullOrEmpty(currentPos) && currentPos == aCombine.RefDesignation)
                        {
                            _error.Error($"Компонент {aCombine.Name}: повторяющееся позиционное обозначение!");
                            return true;
                        }
                        existing.Properties[Constants.ComponentDesignatorID] = currentPos + "," + aCombine.RefDesignation;
                    }

                    // If already added - increase count
                    existing.Count += aCount;

                    return true;
                }
            } else if (_docType == DocType.ItemsList)
            {
                if (aComponents.TryGetValue(aCombine, out existing))
                {
                    // If already added - increase count and continue
                    existing.Count += aCount;
                    return true;
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
            } else
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
              
        private string ConcatNameSign(string aName, string aSign)
        {
            return $"{aName} {aSign}";
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

        private void ProcessItems(Dictionary<string, Tuple<string, int>> aItems, StringBuilder aBuilder)
        {
            if (aItems.Count == 0)
            {
                return;
            } else if (aItems.Count <= 2)
            {
                // Add ids with ","
                foreach (var item in aItems)
                {
                    if (aBuilder.Length > 0 && !aBuilder.EndsWith(","))
                    {
                        aBuilder.Append(", ");
                    }
                    aBuilder.Append(item.Key);
                }
            } else
            {
                // Add ids with "-"
                var f = aItems.First();
                var l = aItems.Last();

                if (aBuilder.Length > 0)
                {
                    aBuilder.Append(", ");
                }
                aBuilder.Append(f.Key + "-" + l.Key);
            }
        }

        private void UpdateNote(Component aCmp, string aNote)
        {
            aCmp.Properties[Constants.ComponentNote] = aNote;
        }

        private void UpdateDesignators(IDictionary<CombineProperties, Component> aComponents)
        {
            foreach (var cmp in aComponents.Values)
            {
                string currentPos;
                if (cmp.Properties.TryGetValue(Constants.ComponentDesignatorID, out currentPos))
                {
                    if (_projectType != ProjectType.Other)
                    {
                        // Not needed to process already combined components
                        UpdateNote(cmp, currentPos);
                        continue;
                    }

                    // Split ids
                    string[] split = currentPos.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length < 2)
                    {
                        // Not needed to process 1 or 0 values
                        UpdateNote(cmp, currentPos);
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

                    StringBuilder result = new StringBuilder();
                    Dictionary<string, Tuple<string, int>> items = new Dictionary<string, Tuple<string, int>>();
                    for (int i = 0; i < split.Length; i++)
                    {
                        if (items.Count == 0)
                        {
                            // Add current id in list
                            items.Add(split[i], ParseDesignatorId(split[i]));
                            continue;
                        }

                        // Get previous and current
                        var p = items.Last();
                        var c = ParseDesignatorId(split[i]);

                        // Compare with previous
                        if (c.Item1 == p.Value.Item1)
                        {
                            if (c.Item2 == p.Value.Item2 + 1)
                            {
                                // Add to list
                                items.Add(split[i], c);
                            } else
                            {
                                // Process collected items
                                ProcessItems(items, result);
                                // Clear ids 
                                items.Clear();
                                // Add to list
                                items.Add(split[i], c);
                            }
                        } else
                        {
                            // Process collected items
                            ProcessItems(items, result);
                            // Clear ids 
                            items.Clear();
                            // Add to list
                            items.Add(split[i], c);
                        }
                    }
                    // Process remained items
                    ProcessItems(items, result);
                    // Save updated id
                    cmp.Properties[Constants.ComponentDesignatorID] = result.ToString();
                    // Update note
                    UpdateNote(cmp, result.ToString());
                }
            }
        }

        private void UpdateGroupNames(IDictionary<string, Group> aGroups, Group aGroup, string aNewName)
        {
            aGroups.Remove(aGroup.Name);
            // Update name, add back
            aGroup.Name = aNewName;
            aGroups.Add(aNewName, aGroup);           
        }

        private void UpdateGroupNames(IDictionary<string, Group> aGroups, Group aGroup)
        {
            if (aGroup.Name.Contains(@"\"))
            {
                string[] split = aGroup.Name.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 2)
                {
                    string name = aGroup.Components.Count > 1 ? split[1] : split[0];
                    UpdateGroupNames(aGroups, aGroup, name);
                }
            } else
            {
                if (aGroup.Components.Count > 1)
                {
                    var name = GroupNames.GetGroupName(aGroup.Name);
                    if (string.IsNullOrEmpty(name))
                    {
                        _error.Error($"Множественное число для группы {aGroup.Name} в словаре GroupNames.cfg не найдено!");
                    } else
                    {
                        UpdateGroupNames(aGroups, aGroup, name);
                    }
                }
            }
        }

        private void ProcessGroupNames(Lazy<Group> aOthers, IDictionary<string, Group> aGroups)
        {
            foreach (var gp in aGroups.AsNotNull().ToList().OrderBy(item => item.Key))
            {
                ProcessGroupNames(aOthers, gp.Value.SubGroups);

                if (gp.Value.Components.Count == 1)
                {
                    aOthers.Value.Components.AddRange(gp.Value.Components);
                    aGroups.Remove(gp.Key);
                } else
                {
                    UpdateGroupNames(aGroups, gp.Value);
                }

                if (gp.Value.Components.Count == 0 && (gp.Value.SubGroups == null || gp.Value.SubGroups.Count == 0))
                {
                    aGroups.Remove(gp.Key);
                }
            }
        }

        private void ProcessGroupNames(Group aGroup)
        {
            foreach (var gp in aGroup.SubGroups.AsNotNull().ToList())
            {
                // Update group names
                UpdateGroupNames(aGroup.SubGroups, gp.Value);

                // Move single component to parent group
                if (gp.Value.Components.Count == 1)
                {
                    // Update component name
                    Component cp = gp.Value.Components.First();
                    string type = cp.GetProperty(Constants.ComponentType);
                    string name = cp.GetProperty(Constants.ComponentName);
                    cp.SetPropertyValue(Constants.ComponentName, name.Contains(type) ? name : type + " " + name);
                    // Move component to parent group
                    aGroup.Components.Add(cp);
                    // Remove subgroup
                    aGroup.SubGroups.Remove(gp.Value.Name);
                }
            }
        }

        private void ProcessGroupNames(Configuration aCfg)
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

                ProcessGroupNames(others, aCfg.Bill);
            } else
            {
                foreach (var gp in aCfg.Specification)
                {
                    ProcessGroupNames(gp.Value);
                }
            }
        }

        private bool CheckVersion(string aFilePath)
        {
            Version v;
            if (Version.TryParse(_xml.Transaction.Version, out v))
            {
                if (v.CompareTo(MinVersion) < 0)
                {
                    _error.Error($"Версия ({v}) загружаемого файла {aFilePath} меньше текущей рабочей версии {MinVersion}!");
                    return false;
                }
            }
            return true;
        }

        private bool HasASсhema(Configuration aConfifg)
        {
            Group docs;
            if (aConfifg.Specification.TryGetValue(Constants.GroupDoc, out docs))
            {
                var doc = docs.Components.Where(key => key.GetProperty(Constants.ComponentName).ToLower().Contains("схема"));
                if (doc != null && doc.Count() > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// добавить по два пустых компонента (пустых строки) в каждый раздел спецификации
        /// </summary>
        /// <param name="aConfig">a configuration.</param>
        private void AddEmptyComponentsToSpecificationGroups(Configuration aConfig)
        {
            foreach (var gp in aConfig.Specification)
            {
                if (gp.Value.SubGroups.Count == 0)
                {
                    if (gp.Value.Components.Count > 0)
                    {
                        gp.Value.Components.Add(new Component(Guid.NewGuid(), 0));
                        gp.Value.Components.Add(new Component(Guid.NewGuid(), 0));
                    }
                } else
                {
                    var componenets = gp.Value.SubGroups.Last().Value.Components;
                    componenets.Add(new Component(Guid.NewGuid(), 0));
                    componenets.Add(new Component(Guid.NewGuid(), 0));
                }
            }
        }


        private bool CheckEnableAutoSort(List<Component> aComponents)
        {
            foreach (var cmp in aComponents)
            {
                var position = cmp.GetProperty(Constants.ComponentPosition);
                if (string.IsNullOrEmpty(position) || string.Equals(position, "0"))
                {
                    return true;
                } else
                    return false;
            }

            return true;
        }

        private void AddComponent(Group aGroup, Component aComponent)
        {
            if (aGroup.Components.Count == 0)
                aGroup.Components.Add(aComponent);
            else
            {                
                
                bool inc = false;                
                foreach (var cmp in aGroup.Components) 
                {                    
                    if (EqualComponents(_docType, aComponent, cmp))
                    {
                        cmp.Count += aComponent.Count;
                        inc = true;
                        break;
                    }
                }

                if (!inc)
                    aGroup.Components.Add(aComponent);
            }
        }

        private bool EqualComponents(DocType aDocType, Component aFirstComponent, Component aSecondComponent)
        {            
            string name = aFirstComponent.GetProperty(Constants.ComponentName);
            string sign = aFirstComponent.GetProperty(Constants.ComponentSign);
            

            string cmp_name = aSecondComponent.GetProperty(Constants.ComponentName);            
            string cmp_sign = aSecondComponent.GetProperty(Constants.ComponentSign);
            

            if (aDocType == DocType.ItemsList)
            {
                string designator = aFirstComponent.GetProperty(Constants.ComponentDesignatorID);
                string cmp_designator = aSecondComponent.GetProperty(Constants.ComponentDesignatorID);
                return (string.Equals(cmp_name, name) && string.Equals(designator, cmp_designator) && string.Equals(cmp_sign, sign));
            }

            if (aDocType == DocType.Bill || aDocType == DocType.D27)
            {
                string cmp_whereIncluded = aSecondComponent.GetProperty(Constants.ComponentWhereIncluded);
                string whereIncluded = aFirstComponent.GetProperty(Constants.ComponentWhereIncluded);
                return (string.Equals(cmp_name, name) && string.Equals(cmp_whereIncluded, whereIncluded) && string.Equals(cmp_sign, sign));
            }

            return (string.Equals(cmp_name, name) && string.Equals(cmp_sign, sign));
                        
        }
    }
}
