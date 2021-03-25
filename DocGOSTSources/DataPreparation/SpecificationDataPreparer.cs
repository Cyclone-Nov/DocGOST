using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GostDOC.Common;
using GostDOC.Models;
using iText.Layout.Properties;
using GostDOC.PDF;

namespace GostDOC.DataPreparation
{

    /// <summary>
    /// 
    /// </summary>
    enum ChangeNameBySubGroupName
    {
        /// <summary>
        /// добавить в имя компонента имя группы в единственно числе
        /// </summary>
        AddSubgroupName,
        /// <summary>
        /// исключить из имени компонента имя группы в единственном числе
        /// </summary>
        ExcludeSubgroupSingleName,
        /// <summary>
        /// ничего не менять
        /// </summary>
        WithoutChanges
    }

    internal class SpecificationDataPreparer : BasePreparer
    {
        /// <summary>
        /// признак что это спецификация для печатной платы
        /// </summary>
        public bool IsPCBSpecification { get; private set; } = false;
        
        public override string GetDocSign(Configuration aMainConfig)
        {
            return "СП";
        }

        /// <summary>
        /// формирование таблицы данных
        /// </summary>
        /// <param name="aConfigs"></param>
        /// <returns></returns>    
        public override DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs)
        {
            // выбираем основную конфигурацию
            Configuration mainConfig = null;
            var listPreparedConfigs = PrepareConfigs(aConfigs, out mainConfig);

            if (mainConfig == null)
            {
                // todo: add to log
                return null;
            }

            bool appliedConfigs = (listPreparedConfigs != null && listPreparedConfigs.Count() > 0);                        

            appliedParams.Clear();
            IDictionary<string, List<Tuple<string, int>>> Positions = new Dictionary<string, List<Tuple<string, int>>>();

            DataTable table = CreateTable("SpecificationData");

            // получим обозначение изделия
            string sign = GetDeviceSign(aConfigs);

            // позиция должна быть сквозной для всего документа
            int position = 0;

            // если есть остальные исполнения то составим имя для общих данных
            if (appliedConfigs)
            {
                // будем передавать имя конфигурации через название таблицы
                table.TableName = String.Join(",", aConfigs.Keys); // aConfigs.Keys.Aggregate((a, b) => a + "," + b)
            }
            else
                table.TableName = mainConfig.Name; 

            // заполнение данных из основного исполнения или общих данных при наличии нескольких исполнений
            FillConfiguration(table, mainConfig, ref position, Positions, sign);

            // заполним переменные данные исполнений, если они есть
            if (appliedConfigs)
            {
                AddConfigsVariableDataSign(table);

                foreach (var config in listPreparedConfigs.OrderBy(key => key.Key))
                {
                    table.TableName = config.Key; // будем передавать имя конфигурации через название таблицы
                    FillConfiguration(table, config.Value, ref position, Positions, sign, false);
                }
            }
            RemoveEmptyRowsAtEnd(table);
                        
            appliedParams.Add(Constants.AppDataSpecPositions, Positions);

            return table;           
        }

        /// <summary>
        /// подготовить данные конфигураций к выводу в таблицу данных
        /// </summary>
        /// <param name="aConfigs">все исполнения спецификации</param>
        /// <param name="aCommonConfig">общая часть конфигураций для всех исполнений спецификации</param>
        /// <returns></returns>
        private IDictionary<string, Configuration> PrepareConfigs(IDictionary<string, Configuration> aConfigs, out Configuration aCommonConfig)
        {            
            if (aConfigs == null || !aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var mainConfig))
            {
                aCommonConfig = null;
                return null;
            }

            // если конфигурация одна то, она же общая часть
            if (aConfigs.Count() == 1)
            {
                aCommonConfig = mainConfig;
                return null;
            }

            // если конфигураций несколько            
            aCommonConfig = new Configuration();
            var deltaMainConfig = new Configuration();
            deltaMainConfig.Graphs = aCommonConfig.Graphs = mainConfig.Graphs;
            var mainData = mainConfig.Specification;

            // выберем неглавные конфигурации
            Dictionary<string, Configuration> otherConfigs = new Dictionary<string, Configuration>();
            foreach (var config in aConfigs)
            {
                if (!string.Equals(config.Key, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase))
                {
                    otherConfigs.Add(config.Key, config.Value.DeepCopy());
                }
            }
             
            // для каждого раздела спецификации
            foreach (var group in mainData)
            {
                // для каждого компонента из корня каждого раздела
                foreach (var component in group.Value.Components)
                {
                    if (ExcludeCommonComponentFormOtherConfigs(otherConfigs, component, group.Key))
                    {
                        if (!aCommonConfig.Specification.ContainsKey(group.Key))
                        {
                            aCommonConfig.Specification.Add(group.Key, new Group());
                            aCommonConfig.Specification[group.Key].Name = group.Key;
                        }
                        aCommonConfig.Specification[group.Key].Components.Add(component);
                    } else
                    {
                        if (!deltaMainConfig.Specification.ContainsKey(group.Key))
                        {
                            deltaMainConfig.Specification.Add(group.Key, new Group());
                        }
                        deltaMainConfig.Specification[group.Key].Components.Add(component);                        
                    }
                }

                // для каждой подгруппы из данного раздела 
                foreach (var subgroup in group.Value.SubGroups)
                {
                    // для каждого компонента из подгруппы данного раздела
                    foreach (var component in subgroup.Value.Components)
                    {                           
                        if (ExcludeCommonComponentFormOtherConfigs(otherConfigs, component, group.Key, subgroup.Key))
                        {
                            // поместим исключенный компонент в общую часть
                            if (!aCommonConfig.Specification.ContainsKey(group.Key))
                            {
                                aCommonConfig.Specification.Add(group.Key, new Group());
                            }
                            if (!aCommonConfig.Specification[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                aCommonConfig.Specification[group.Key].SubGroups.Add(subgroup.Key, new Group());
                            }
                            aCommonConfig.Specification[group.Key].SubGroups[subgroup.Key].Components.Add(component);
                        } else
                        {
                            // поместим компонент, который встречается не во всех исполнениях в часть отличий
                            if (!deltaMainConfig.Specification.ContainsKey(group.Key))
                            {
                                deltaMainConfig.Specification.Add(group.Key, new Group());
                            }
                            if (!deltaMainConfig.Specification[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                deltaMainConfig.Specification[group.Key].SubGroups.Add(subgroup.Key, new Group());
                            }
                            deltaMainConfig.Specification[group.Key].SubGroups[subgroup.Key].Components.Add(component);
                        }
                    }
                }
            }

            // если часть основного исполнения с различиями не пуста, то добавим ее к прочим исполнениям
            if (deltaMainConfig.Specification.Count > 0)
            {
                deltaMainConfig.Graphs = mainConfig.Graphs;
                otherConfigs.Add(Constants.MAIN_CONFIG_INDEX, deltaMainConfig);
                return otherConfigs;
            }

            // так как часть основного исполнения с различиями пуста, то и прочие исполения нас не интересуют
            return null;
        }

        /// <summary>
        /// заполнить таблицу данных <paramref name="aTable"/> данными из конфигурации <paramref name="aConfig"/>
        /// </summary>
        /// <param name="aTable">итоговая таблица с данными</param>
        /// <param name="aConfig">конфигурация</param>
        /// <param name="aManyConfigs">признак наличия нескольких конфигураций: если <c>true</c> то несколько конфигураций</param>
        private void FillConfiguration(DataTable aTable, Configuration aConfig, ref int aPosition, IDictionary<string, List<Tuple<string, int>>> aPositions, string aSign, bool aСommonConfig = true)
        {
            var data = aConfig.Specification;            
            if (data.Count == 0)
                return;

            // если это не общая конфигурация, а 
            if (!aСommonConfig)
            {
                string configName;
                if (string.Equals(aConfig.Name, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase))                
                    configName = aSign; // "Обозначение"
                 else                
                    configName = $"{aSign}{aConfig.Name}"; // "Обозначение""aConfigName"

                var row = aTable.NewRow();
                row[Constants.ColumnName] = new FormattedString { Value = configName, IsUnderlined = true, TextAlignment = TextAlignment.CENTER };
                aTable.Rows.Add(row);                
            }
                        
            if (aConfig.PrivateProperties.TryGetValue(Constants.SignPCB, out var val))
            {
                IsPCBSpecification = (bool)val;
            }

            AddGroup(aTable, Constants.GroupDoc, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupComplex, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupAssemblyUnits, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupDetails, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupStandard, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupOthers, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupMaterials, data, ref aPosition, aPositions);
            AddGroup(aTable, Constants.GroupKits, data, ref aPosition, aPositions);

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
        }

        /// <summary>
        /// заполнить таблицу данных
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        /// <param name="aComponents"></param>
        /// <param name="aOtherComponents"></param>
        /// <param name="aSchemaDesignation"></param>
        private void AddGroup(DataTable aTable, string aGroupName, IDictionary<string, Group> aGroupDic, ref int aPos, IDictionary<string, List<Tuple<string, int>>> aPositions)
        {
            if (aGroupDic == null || !aGroupDic.ContainsKey(aGroupName))
                return;

            if (!aGroupDic.TryGetValue(aGroupName, out var group))
                return;

            if (group.Components?.Count == 0 && group.SubGroups?.Count == 0)            
                return;            

            // наименование раздела
            AddEmptyRow(aTable);
            if (!string.IsNullOrEmpty(aGroupName))
            {
                ShiftToNextPageIfEnd(aTable, true);
                AddGroupName(aTable, aGroupName);
                AddEmptyRow(aTable);
            }

            var сomponents = group.Components;

            // добавим в наименование компонента название группы для компонентов без групппы для раздела Прочие изделия и раздела Стандартны изделия
            bool isStandardGroup = string.Equals(aGroupName, Constants.GroupStandard);
            bool isOthersGroup = string.Equals(aGroupName, Constants.GroupOthers);
            ChangeNameBySubGroupName changeComponentName = ChangeNameBySubGroupName.WithoutChanges;
            if (isOthersGroup || isStandardGroup)            
                changeComponentName = ChangeNameBySubGroupName.AddSubgroupName;                            

            // будем добавлять позицию, если раздел не Документация и не Комплексы
            bool setPos = !string.Equals(aGroupName, Constants.GroupDoc) && !string.Equals(aGroupName, Constants.GroupComplex);

            if (!IsPCBSpecification)
            {
                if (сomponents.Count > 0)
                {
                    AddComponents(aTable, сomponents, ref aPos, aPositions, setPos, changeComponentName);
                    AddEmptyRow(aTable);
                }
            }

            // добавляем подгруппы
            foreach (var subgroup in group.SubGroups.OrderBy(key => key.Value.SortName ?? key.Value.Name).
                                                     Where(key => !string.Equals(key.Key, Constants.SUBGROUPFORSINGLE)))
            {
                if (subgroup.Value.Components.Count > 0)
                {
                    string subGroupName = GetSubgroupNameByCount(subgroup);                    
                    bool hasDifferSubgroupNamesInComponents = HasDifferentSubGroupNames(subgroup.Value.Components);
                    int componentsCount = subgroup.Value.Components.Count;

                    // поменяем имя подгруппы если все компоненты имеют одно и тоже имя подгруппы, но оно отличается от исходного для раздела прочие изделия
                    if (!hasDifferSubgroupNamesInComponents && isOthersGroup && IsPCBSpecification)
                    {
                        string newSubGroupName = subgroup.Value.Components.First().GetProperty(Constants.SubGroupNameSp);
                        if (!string.IsNullOrEmpty(newSubGroupName))
                        {
                            // у нас есть пустые компонеты (имя пусто), но они не должны учитываться при расчете количества
                            var components = subgroup.Value.Components.Where(cmp => !string.IsNullOrEmpty(cmp.GetProperty(Constants.ComponentName)));
                            componentsCount = components.Count();
                            subGroupName = GetSubgroupName(newSubGroupName, componentsCount == 1);
                        }
                    }
                     
                    if (hasDifferSubgroupNamesInComponents || componentsCount == 1)
                    {
                        changeComponentName = ChangeNameBySubGroupName.AddSubgroupName;
                    } else
                        changeComponentName = ChangeNameBySubGroupName.WithoutChanges;

                    AddSubgroup(aTable, subGroupName, subgroup.Value.Components, ref aPos, aPositions, changeComponentName);
                    AddEmptyRow(aTable);
                }
            }

            // отдельно запишем подгруппу "Прочие"
            if (group.SubGroups.TryGetValue(Constants.SUBGROUPFORSINGLE, out var subgroup_other))
            {
                if (subgroup_other.Components.Count > 0)
                {                    
                    AddSubgroup(aTable, Constants.SUBGROUPFORSINGLE, subgroup_other.Components, ref aPos, aPositions, ChangeNameBySubGroupName.AddSubgroupName);
                    AddEmptyRow(aTable);
                }                
            }

            if (IsPCBSpecification)
            {
                changeComponentName = ChangeNameBySubGroupName.AddSubgroupName;
                AddComponents(aTable, сomponents, ref aPos, aPositions, setPos, changeComponentName);
                AddEmptyRow(aTable);
            }

            RemoveLastRow(aTable);
        }

        /// <summary>
        /// Adds the subgroup.
        /// </summary>
        /// <param name="aTable">a table.</param>
        /// <param name="aGroupName">Name of a group.</param>
        /// <param name="aSortComponents">a sort components.</param>
        /// <param name="aPos">a position.</param>
        /// <param name="aPositions">a positions.</param>
        /// <param name="aChangeComponentName">Name of a change component.</param>
        /// <returns></returns>
        private bool AddSubgroup(DataTable aTable, 
                                 string aGroupName, 
                                 List<Component> aSortComponents, 
                                 ref int aPos, 
                                 IDictionary<string, List<Tuple<string, int>>> aPositions,
                                 ChangeNameBySubGroupName aChangeComponentName = ChangeNameBySubGroupName.WithoutChanges)
        {
            if (!aSortComponents.Any())            
                return false;

            // не будем добавлять имя подгруппы если надо добавлять наименование группы каждому компоненту
            if (aChangeComponentName != ChangeNameBySubGroupName.AddSubgroupName)
            {
                ShiftToNextPageIfEnd(aTable, false);

                AddGroupName(aTable, aGroupName, false, TextAlignment.LEFT);
            }

            AddComponents(aTable, aSortComponents, ref aPos, aPositions, true, aChangeComponentName);                        
            aTable.AcceptChanges();
            return true;
        }

        /// <summary>
        /// Adds the components.
        /// </summary>
        /// <param name="aTable">a table.</param>
        /// <param name="aSortComponents">a sort components.</param>
        /// <param name="aPos">a position.</param>
        /// <param name="aPositions">a positions.</param>
        /// <param name="aSetPos">if set to <c>true</c> [a set position].</param>
        /// <param name="aChangeComponentName">Name of a change component.</param>
        private void AddComponents(DataTable aTable, 
                                   List<Component> aSortComponents, 
                                   ref int aPos, 
                                   IDictionary<string, List<Tuple<string, int>>> aPositions, 
                                   bool aSetPos = true, 
                                   ChangeNameBySubGroupName aChangeComponentName = ChangeNameBySubGroupName.WithoutChanges)
        {
            DataRow row;
            foreach (var component in aSortComponents)
            {   
                string component_name = component.GetProperty(Constants.ComponentName);
                string prepared_component_name = component_name;
                string groupSp = component.GetProperty(Constants.GroupNameSp);
                int component_count = GetComponentCount(component, string.Equals(groupSp, Constants.GroupDoc));

                // признак что компонент не пустой
                bool nonEmptyComponent = !string.IsNullOrEmpty(component_name);
                if (nonEmptyComponent)
                {
                    string subGroupName = GetSubgroupName(component.GetProperty(Constants.SubGroupNameSp), true);
                    if (aChangeComponentName == ChangeNameBySubGroupName.AddSubgroupName)
                    {                        
                        if (component_name.IndexOf(subGroupName, 0, StringComparison.InvariantCultureIgnoreCase) < 0)
                            prepared_component_name = $"{subGroupName} {component_name}";
                    }
                    else if (aChangeComponentName == ChangeNameBySubGroupName.ExcludeSubgroupSingleName)
                    {                                                
                        if (component_name.IndexOf(subGroupName, 0, StringComparison.InvariantCultureIgnoreCase) == 0)
                            prepared_component_name = component_name.Substring(subGroupName.Length).TrimStart();
                    }

                    if (IsPCBSpecification)
                    {
                        string doc = component.GetProperty(Constants.ComponentDoc);
                        if (!string.IsNullOrEmpty(doc) && !prepared_component_name.Contains(doc))
                            prepared_component_name += $" {doc}";
                    }
                }

                string[] namearr = PdfUtils.SplitStringByWidth(Constants.SpecificationColumn5NameWidth - 4, prepared_component_name, new char[] {' '}, Constants.SpecificationFontSize, true).ToArray();                
                var note = component.GetProperty(Constants.ComponentNote);
                string[] notearr = PdfUtils.SplitStringByWidth(Constants.SpecificationColumn7FootnoteWidth - 3, note, new char[] {' ','-', ',' }, Constants.SpecificationFontSize).ToArray();
                string designation = component.GetProperty(Constants.ComponentSign);
                string zone = component.GetProperty(Constants.ComponentZone);

                row = aTable.NewRow();
                row[Constants.ColumnFormat] = new FormattedString { Value = component.GetProperty(Constants.ComponentFormat) };
                row[Constants.ColumnZone] = new FormattedString{ Value = zone }; 
                
                // если необходимо установить позицию
                if (aSetPos)
                {
                    ++aPos;
                    // если элемент не пустой
                    if (nonEmptyComponent)
                        row[Constants.ColumnPosition] = new FormattedString { Value = aPos.ToString(), TextAlignment = TextAlignment.CENTER };

                    string posComponentName = DataPreparationUtils.GetNameForPositionDictionary(component);
                    var configNames = aTable.TableName.Split(',');
                    foreach (var cfgName in configNames)
                    {
                        string key = ($"{cfgName} {groupSp}").Trim();                        
                        if (!aPositions.ContainsKey(key))
                            aPositions.Add(key, new List<Tuple<string, int>>() { new Tuple<string, int>(posComponentName, aPos) });
                        else
                            aPositions[key].Add(new Tuple<string, int>(posComponentName, aPos));
                    }
                }
                                                
                row[Constants.ColumnSign] = new FormattedString { Value = designation };
                row[Constants.ColumnName] = new FormattedString { Value = namearr.First() };
                row[Constants.ColumnQuantity] = component_count;
                row[Constants.ColumnFootnote] = new FormattedString { Value = notearr.First() };
                aTable.Rows.Add(row);

                int max = Math.Max(namearr.Length, notearr.Length);
                if (max > 1)
                {
                    int ln_name = namearr.Length;
                    int ln_note = notearr.Length;

                    for (int ln = 1; ln < max; ln++)
                    {
                        row = aTable.NewRow();                        
                        row[Constants.ColumnName] = (ln_name > ln) ? new FormattedString { Value = namearr[ln] } : null;
                        row[Constants.ColumnFootnote] = (ln_note > ln) ? new FormattedString { Value = notearr[ln] } : null;
                        aTable.Rows.Add(row);
                    }
                }
            }
        }


        /// <summary>
        /// добавить имя группы в таблицу
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        private void AddGroupName(DataTable aTable, string aGroupName, bool aIsUnderline = true, TextAlignment aTextAlignment = TextAlignment.CENTER) 
        {
            if (string.IsNullOrEmpty(aGroupName)) 
                return;
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString {Value = aGroupName, IsUnderlined = aIsUnderline, TextAlignment = aTextAlignment };
            aTable.Rows.Add(row);            
        }


        /// <summary>
        /// создание таблицы данных для документа Спецификация
        /// </summary>
        /// <param name="aDataTableName"></param>
        /// <returns></returns>
        protected override DataTable CreateTable(string aDataTableName)
        {
            DataTable table = new DataTable(aDataTableName);
            DataColumn column = new DataColumn("id", typeof(Int32)) { 
                Caption = "id", Unique = true, AutoIncrement = true, AllowDBNull = false
            };
            
            table.Columns.Add(column);
            table.PrimaryKey = new DataColumn[] { column };
            
            AddColumn(table, Constants.ColumnFormat,   "Формат",       typeof(FormattedString));
            AddColumn(table, Constants.ColumnZone,     "Зона",         typeof(FormattedString));
            AddColumn(table, Constants.ColumnPosition, "Поз.",         typeof(FormattedString));
            AddColumn(table, Constants.ColumnSign,     "Обозначение",  typeof(FormattedString));
            AddColumn(table, Constants.ColumnName,     "Наименование", typeof(FormattedString));
            AddColumn(table, Constants.ColumnQuantity, "Кол.",         typeof(Int32));
            AddColumn(table, Constants.ColumnFootnote, "Примечание",   typeof(FormattedString));

            return table;
        }


        private string GetDeviceSign(IDictionary<string, Configuration> aConfigs)
        {
            if (aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var aConfig))
            {
                if (aConfig.Graphs.TryGetValue(Constants.GRAPH_2, out var sign)) // получим значение графы "Обозначение"
                {
                    return sign;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// добавить в таблицу данны надписи "Переменные данные исполнений"
        /// </summary>
        /// <param name="table">The table.</param>
        private void AddConfigsVariableDataSign(DataTable aTable)
        {
            //AddEmptyRow(aTable);
            var row = aTable.NewRow();
            row[Constants.ColumnSign] = new FormattedString { Value = "Переменные данные", IsUnderlined = true, TextAlignment = TextAlignment.RIGHT };
            row[Constants.ColumnName] = new FormattedString { Value = "для исполнений", IsUnderlined = true, TextAlignment = TextAlignment.LEFT };            
            aTable.Rows.Add(row);
            AddEmptyRow(aTable);
        }

        /// <summary>
        /// получить количество компонентов
        /// </summary>
        /// <param name="aCountStr"></param>
        /// <returns></returns>
        private int GetComponentCount(Component aComponent, bool ZeroCount) {

            if (ZeroCount)            
                return 0;
            
            string aCountStr = aComponent.GetProperty(Constants.ComponentCountDev);
            uint count = 1;
            if (!string.IsNullOrEmpty(aCountStr)) {
                if (Int32.TryParse(aCountStr, out var cnt)) {
                    count = (uint)cnt;                    
                }
            }

            return (int)((count > aComponent.Count) ?  count : aComponent.Count);
        }

        /// <summary>
        /// исключить (удалить) компонент из прочих (не главноего) исполнений при наличие его в них
        /// </summary>
        /// <param name="aOtherConfigs">словарь прочих исполнений</param>
        /// <param name="aComponent">компонент</param>
        /// <param name="aGroupName">имя раздела</param>
        /// <param name="aGroupName">имя подгруппы</param>
        /// <returns></returns>
        private bool ExcludeCommonComponentFormOtherConfigs(Dictionary<string, Configuration> aOtherConfigs, Component aComponent, string aGroupName, string aSubGroupName = "")
        {
            List<Component> removeList = null;
            bool bRemoveGroup = false;
            Component removeComponent = null;
            bool result = false;

            foreach (var config in aOtherConfigs)
            {
                removeList = null;
                if (string.IsNullOrEmpty(aSubGroupName))
                {
                    bRemoveGroup = false;
                    if (config.Value.Specification.ContainsKey(aGroupName))
                    {
                        foreach (var othercomp in config.Value.Specification[aGroupName].Components)
                        {
                            if (EqualsSpecComponents(othercomp, aComponent))
                            {
                                removeList = config.Value.Specification[aGroupName].Components;
                                removeComponent = othercomp;
                                bRemoveGroup = (removeList.Count() == 1 && config.Value.Specification[aGroupName].SubGroups.Count == 0);
                                break;
                            }
                        }

                        if (removeList != null)
                        {
                            removeList?.Remove(removeComponent);
                            removeList = null;
                            result = true;

                            if (bRemoveGroup)
                                config.Value.Specification.Remove(aGroupName);
                        }
                    }
                } else
                {
                    if (config.Value.Specification.ContainsKey(aGroupName))
                    {

                        if (config.Value.Specification[aGroupName].SubGroups.ContainsKey(aSubGroupName))
                        {
                            foreach (var othercomp in config.Value.Specification[aGroupName].SubGroups[aSubGroupName].Components)
                            {
                                if (EqualsSpecComponents(othercomp, aComponent))
                                {
                                    removeList = config.Value.Specification[aGroupName].SubGroups[aSubGroupName].Components;
                                    removeComponent = othercomp;
                                    bRemoveGroup = (removeList.Count() == 1);
                                    break;
                                }
                            }

                            if (removeList != null)
                            {
                                removeList?.Remove(removeComponent);
                                removeList = null;
                                result = true;
                                if (bRemoveGroup)
                                    config.Value.Specification[aGroupName].SubGroups.Remove(aSubGroupName);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether [has different group names] [the specified components].
        /// </summary>
        /// <param name="Components">The components.</param>
        /// <param name="aBaseSubGroupName">The components.</param>
        /// <returns>
        ///   <c>true</c> if [has different group names] [the specified components]; otherwise, <c>false</c>.
        /// </returns>
        private bool HasDifferentSubGroupNames(IList<Component> aComponents)
        {
            string lastSubGroupName = aComponents.First().GetProperty(Constants.SubGroupNameSp);

            string currentSubGroupName = string.Empty;

            foreach (var cmp in aComponents)
            {
                currentSubGroupName = cmp.GetProperty(Constants.SubGroupNameSp);
                var componentName = cmp.GetProperty(Constants.ComponentName);
                if (!string.IsNullOrEmpty(lastSubGroupName) &&
                    !string.IsNullOrEmpty(componentName) &&
                    !string.IsNullOrEmpty(currentSubGroupName) && 
                    !string.Equals(lastSubGroupName, currentSubGroupName, StringComparison.InvariantCultureIgnoreCase))
                {                    
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// добавить пустые строки для выравния по странице если заголовок раздела или подгруппы и копоненты оказываются на разных страницах 
        /// </summary>
        /// <param name="aTable">таблица с данными</param>
        /// <param name="aIsGroup"><c>true</c> - если это раздел, иначе подгруппа (<c>false</c>).</param>
        private void ShiftToNextPageIfEnd(DataTable aTable, bool aIsGroup)
        {
            int groupNameRowNumber = aTable.Rows.Count + 1;
            int firstComponentRowNumber = aIsGroup ? groupNameRowNumber + 2 : groupNameRowNumber + 1;
            int groupNamePageNumber = CommonUtils.GetCurrentPage(DocType.Specification, groupNameRowNumber);
            int firstComponentPageNumber = CommonUtils.GetCurrentPage(DocType.Specification, firstComponentRowNumber);
            if (firstComponentPageNumber > groupNamePageNumber)
            {
                AddEmptyRowsToEndPage(aTable, DocType.Specification, groupNameRowNumber);
            }
        }


        private bool EqualsSpecComponents(Component aFirstComponent, Component aSecondComponent)
        {
            string name1 = aFirstComponent.GetProperty(Constants.ComponentName);
            string name2 = aSecondComponent.GetProperty(Constants.ComponentName);

            string sign1 = aFirstComponent.GetProperty(Constants.ComponentSign);
            string sign2 = aSecondComponent.GetProperty(Constants.ComponentSign);

            if (string.Equals(name1, name2) &&
                string.Equals(sign1, sign2) &&
                aFirstComponent.Count == aSecondComponent.Count)
            {
                return true;
            }

            return false;

            //return aFirstComponent.Equals(aSecondComponent); //TODO: override Equals
        }

        private void RemoveEmptyRowsAtEnd(DataTable table)
        {
            if (table.Rows.Count > 1)
            {                
                int last_index = table.Rows.Count - 1;
                while (IsEmptyRow(table, last_index))
                {                                
                    table.Rows.RemoveAt(last_index);
                    last_index--;
                }                
            }
        }

        private void RemoveLastRow(DataTable table)
        {
            if (table.Rows.Count > 1)
            {
                int last_index = table.Rows.Count - 1;                
                table.Rows.RemoveAt(last_index);
            }
        }

        private bool IsEmptyRow(DataTable aTable, int aIndex)
        {
            var arr = aTable.Rows[aIndex].ItemArray;
            for (int i = 1; i < arr.Length; i++)
            {
                if (!string.IsNullOrEmpty(arr[i].ToString()))
                    return false;
            }
            return true;           
        }

    }
}
