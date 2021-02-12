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
        WithoutCahnges
    }

    internal class SpecificationDataPreparer : BasePreparer
    {

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
        /// <param name="aConfigs">a configs.</param>
        /// <param name="aMainConfig">a main configuration.</param>
        /// <returns></returns>
        private IDictionary<string, Configuration> PrepareConfigs(IDictionary<string, Configuration> aConfigs, out Configuration aMainConfig)
        {
            
            if (aConfigs == null || !aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var mainConfig))
            {
                aMainConfig = null;
                return null;
            }

            if (aConfigs.Count() == 1)
            {
                aMainConfig = mainConfig;
                return null;
            }

            // если конфигураций несколько            
            aMainConfig = new Configuration();
            var deltaMainConfig = new Configuration();
            deltaMainConfig.Graphs = aMainConfig.Graphs = mainConfig.Graphs;
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
                        
            foreach (var group in mainData)
            {
                // для списка компонентов из корня каждого раздела
                foreach (var component in group.Value.Components)
                {
                    if (CheckComponentInOtherConfigs(otherConfigs, component, group.Key))
                    {
                        if (!aMainConfig.Specification.ContainsKey(group.Key))
                        {
                            aMainConfig.Specification.Add(group.Key, new Group());
                            aMainConfig.Specification[group.Key].Name = group.Key;
                        }
                        aMainConfig.Specification[group.Key].Components.Add(component);
                    } else
                    {
                        if (!deltaMainConfig.Specification.ContainsKey(group.Key))
                        {
                            deltaMainConfig.Specification.Add(group.Key, new Group());
                        }
                        deltaMainConfig.Specification[group.Key].Components.Add(component);                        
                    }
                }

                // для списка компонентов из корня каждого раздела                
                foreach (var subgroup in group.Value.SubGroups)
                {
                    foreach (var component in subgroup.Value.Components)
                    {
                        if (CheckComponentInOtherConfigs(otherConfigs, component, group.Key, subgroup.Key))
                        {
                            if (!aMainConfig.Specification.ContainsKey(group.Key))
                            {
                                aMainConfig.Specification.Add(group.Key, new Group());
                            }
                            if (!aMainConfig.Specification[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                aMainConfig.Specification[group.Key].SubGroups.Add(subgroup.Key, new Group());
                            }
                            aMainConfig.Specification[group.Key].SubGroups[subgroup.Key].Components.Add(component);
                        } else
                        {
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

            otherConfigs.Add(Constants.MAIN_CONFIG_INDEX, deltaMainConfig);
            return otherConfigs;
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

            if (!aСommonConfig)
            {
                string configName;
                if (string.Equals(aConfig.Name, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase))                
                    configName = aSign; // "Обозначение"
                 else                
                    configName = $"{aSign}{aConfig.Name}"; // "Обозначение""aConfigName"

                var row = aTable.NewRow();
                row[Constants.ColumnName] = new FormattedString { Value = configName, IsUnderlined = true, TextAlignment = TextAlignment.LEFT };                
                aTable.Rows.Add(row);                
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
            if (AddGroupName(aTable, aGroupName))
                AddEmptyRow(aTable);           

            var сomponents = group.Components;

            // добавим в наименование компонента название группы, если это раздел Прочие изделия
            ChangeNameBySubGroupName changeComponentName = ChangeNameBySubGroupName.WithoutCahnges;
            if (string.Equals(aGroupName, Constants.GroupOthers))
                changeComponentName = ChangeNameBySubGroupName.AddSubgroupName;

            // будем добавлять позицию, если раздел не Документация и не Комплексы
            bool setPos = !string.Equals(aGroupName, Constants.GroupDoc) && !string.Equals(aGroupName, Constants.GroupComplex);
            AddComponents(aTable, сomponents, ref aPos, aPositions, setPos, changeComponentName);
            
            if (сomponents.Count > 0)            
                AddEmptyRow(aTable);            

            // добавляем подгруппы
            foreach (var subgroup in group.SubGroups.OrderBy(key => key.Key).Where(key => !string.Equals(key.Key, Constants.SUBGROUPFORSINGLE)))
            {
                if (subgroup.Value.Components.Count > 0)
                {
                    string subGroupName = GetSubgroupNameByCount(subgroup);
                    if (string.Equals(aGroupName, Constants.GroupStandard) && !string.IsNullOrEmpty(subGroupName))
                        changeComponentName = ChangeNameBySubGroupName.ExcludeSubgroupSingleName;

                    AddSubgroup(aTable, subGroupName, subgroup.Value.Components, ref aPos, aPositions, changeComponentName);
                    AddEmptyRow(aTable);
                }
            }

            // отдельно запишем подгруппу "Прочие"
            if (group.SubGroups.TryGetValue(Constants.SUBGROUPFORSINGLE, out var subgroup_other))
            {
                if (subgroup_other.Components.Count > 0)
                {                       
                    AddSubgroup(aTable, Constants.SUBGROUPFORSINGLE, subgroup_other.Components, ref aPos, aPositions);
                    AddEmptyRow(aTable);
                }                
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
                                 ChangeNameBySubGroupName aChangeComponentName = ChangeNameBySubGroupName.WithoutCahnges)
        {
            if (!aSortComponents.Any())            
                return false;            

            AddGroupName(aTable, aGroupName, false, TextAlignment.LEFT);

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
                                   ChangeNameBySubGroupName aChangeComponentName = ChangeNameBySubGroupName.WithoutCahnges)
        {
            DataRow row;
            foreach (var component in aSortComponents)
            {   
                string component_name = component.GetProperty(Constants.ComponentName);
                string prepared_component_name = component_name;
                string groupSp = component.GetProperty(Constants.GroupNameSp);
                int component_count = GetComponentCount(component, string.Equals(groupSp, Constants.GroupDoc));                

                if (aChangeComponentName == ChangeNameBySubGroupName.AddSubgroupName)
                {                    
                    string subGroupName = GetSubgroupName(component.GetProperty(Constants.SubGroupNameSp), true);
                    if (component_name.IndexOf(subGroupName, 0, StringComparison.InvariantCultureIgnoreCase) < 0)
                        prepared_component_name = $"{subGroupName} {component_name}";                    
                }
                else if (aChangeComponentName == ChangeNameBySubGroupName.ExcludeSubgroupSingleName)
                {
                    string subGroupName = GetSubgroupName(component.GetProperty(Constants.SubGroupNameSp), true);
                    int val = component_name.IndexOf(subGroupName, 0, StringComparison.InvariantCultureIgnoreCase);
                    if (val == 0)
                        prepared_component_name = component_name.Substring(subGroupName.Length).TrimStart();
                }

                string[] namearr = PdfUtils.SplitStringByWidth(Constants.SpecificationColumn5NameWidth - 3, prepared_component_name, new char[] {' '}, Constants.SpecificationFontSize).ToArray();                
                var note = component.GetProperty(Constants.ComponentNote);
                string[] notearr = PdfUtils.SplitStringByWidth(Constants.SpecificationColumn7FootnoteWidth - 3, note, new char[] {'-', ',' }, Constants.SpecificationFontSize).ToArray();
                string designation = component.GetProperty(Constants.ComponentSign);
                string zone = component.GetProperty(Constants.ComponentZone);

                row = aTable.NewRow();
                row[Constants.ColumnFormat] = new FormattedString { Value = component.GetProperty(Constants.ComponentFormat) };
                row[Constants.ColumnZone] = new FormattedString{ Value = zone };                
                if (aSetPos)
                {
                    ++aPos;
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
        private bool AddGroupName(DataTable aTable, string aGroupName, bool aIsUnderline = true, TextAlignment aTextAlignment = TextAlignment.CENTER) 
        {
            if (string.IsNullOrEmpty(aGroupName)) 
                return false;
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString {Value = aGroupName, IsUnderlined = aIsUnderline, TextAlignment = aTextAlignment };
            aTable.Rows.Add(row);
            return true;
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
        /// определение наличия компонента во всех конфигурациях
        /// </summary>
        /// <param name="aComponent">a component.</param>
        /// <param name="aGroupName">Name of a group.</param>
        /// <param name="aOtherConfigs">a other configs.</param>
        /// <returns></returns>
        private bool CheckComponentInOtherConfigs(Dictionary<string, Configuration> aOtherConfigs, Component aComponent, string aGroupName, string aSubGroupName = "")
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


        private void AddEmptyRow(DataTable aTable) 
        {
            DataRow row = aTable.NewRow();            
            aTable.Rows.Add(row);
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
