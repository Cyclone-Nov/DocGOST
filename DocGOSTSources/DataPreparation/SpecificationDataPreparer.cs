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

        /// <summary>
        /// текущая позиция компонента
        /// </summary>
        private int _position = 0;

        /// <summary>
        /// словарь позиций
        /// </summary>
        IDictionary<string, List<Tuple<string, int>>> _Positions = new Dictionary<string, List<Tuple<string, int>>>();

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

            DataTable table = CreateTable("SpecificationData");

            // получим обозначение изделия
            string sign = GetDeviceSign(aConfigs);

            // обнулим позиции для нового расчета
            _position = 0;
            _Positions.Clear();

            // если есть остальные исполнения то составим имя для общих данных
            if (appliedConfigs)
            {
                // будем передавать имя конфигурации через название таблицы
                table.TableName = String.Join(",", aConfigs.Keys); // aConfigs.Keys.Aggregate((a, b) => a + "," + b)
            }
            else
                table.TableName = mainConfig.Name; 

            // заполнение данных из основного исполнения или общих данных при наличии нескольких исполнений
            FillConfiguration(table, mainConfig, sign);

            // заполним переменные данные исполнений, если они есть
            if (appliedConfigs)
            {
                AddConfigsVariableDataSign(table);

                foreach (var config in listPreparedConfigs.OrderBy(key => key.Key))
                {
                    table.TableName = config.Key; // будем передавать имя конфигурации через название таблицы
                    FillConfiguration(table, config.Value, sign, false);
                }
            }
            RemoveEmptyRowsAtEnd(table);
                        
            appliedParams.Add(Constants.AppDataSpecPositions, _Positions);

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

            aCommonConfig = new Configuration(); // общая конфигурация, куда попадают одинаковые компоненты
            var deltaMainConfig = new Configuration(); // дельта между основной конифигурацией и остальными, сюда попадают те компоненты что есть только в основной конфигурации
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
                    // если компонент исключили из всех конфигураций - значит он общий и его надо добавить в общую конфигурацию
                    if (ExcludeCommonComponentFormOtherConfigs(otherConfigs, component, group.Key))
                    {
                        if (!aCommonConfig.Specification.ContainsKey(group.Key))
                        {
                            aCommonConfig.Specification.Add(group.Key, new Group());
                            aCommonConfig.Specification[group.Key].Name = group.Key;
                        }
                        aCommonConfig.Specification[group.Key].Components.Add(component);
                    } 
                    else // компонент не общий - добавим в дельту основной конфигурации
                    {
                        if (!deltaMainConfig.Specification.ContainsKey(group.Key))
                        {
                            deltaMainConfig.Specification.Add(group.Key, new Group());
                            deltaMainConfig.Specification[group.Key].Name = group.Key;
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
                                aCommonConfig.Specification[group.Key].Name = group.Key;
                            }
                            if (!aCommonConfig.Specification[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                aCommonConfig.Specification[group.Key].SubGroups.Add(subgroup.Key, new Group());
                                aCommonConfig.Specification[group.Key].SubGroups[subgroup.Key].Name = subgroup.Key;
                            }
                            aCommonConfig.Specification[group.Key].SubGroups[subgroup.Key].Components.Add(component);
                        } else
                        {
                            // поместим компонент, который встречается не во всех исполнениях в часть отличий
                            if (!deltaMainConfig.Specification.ContainsKey(group.Key))
                            {
                                deltaMainConfig.Specification.Add(group.Key, new Group());
                                deltaMainConfig.Specification[group.Key].Name = group.Key;
                            }
                            if (!deltaMainConfig.Specification[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                deltaMainConfig.Specification[group.Key].SubGroups.Add(subgroup.Key, new Group());
                                deltaMainConfig.Specification[group.Key].SubGroups[subgroup.Key].Name = subgroup.Key;
                            }
                            deltaMainConfig.Specification[group.Key].SubGroups[subgroup.Key].Components.Add(component);
                        }
                    }
                }
            }

            // если дельта основного исполнения заполнена, то добавим ее к списку прочих дельт остальных исполнений
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
        private void FillConfiguration(DataTable aTable, Configuration aConfig, string aSign, bool aСommonConfig = true)
        {
            var data = aConfig.Specification;            
            if (data.Count == 0)
                return;

            // если это не общая конфигурация, а переменные данные
            if (!aСommonConfig)
            {
                string configName = string.Equals(aConfig.Name, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase) ? aSign : $"{aSign}{aConfig.Name}";
                ShiftToNextPageIfEnd(aTable, DocType.Specification, HeaderType.Configuration);
                AddGroupName(aTable, configName, true, TextAlignment.CENTER);
            }
                        
            if (aConfig.PrivateProperties.TryGetValue(Constants.SignPCB, out var val))
            {
                IsPCBSpecification = (bool)val;
            }

            // тип заголовка
            HeaderType headerType = aСommonConfig ? HeaderType.Group : HeaderType.Config_Group;

            AddGroup(aTable, Constants.GroupDoc, data, ref headerType);
            AddGroup(aTable, Constants.GroupComplex, data, ref headerType);
            AddGroup(aTable, Constants.GroupAssemblyUnits, data, ref headerType);
            AddGroup(aTable, Constants.GroupDetails, data, ref headerType);
            AddGroup(aTable, Constants.GroupStandard, data, ref headerType);
            AddGroup(aTable, Constants.GroupOthers, data, ref headerType);
            AddGroup(aTable, Constants.GroupMaterials, data, ref headerType);
            AddGroup(aTable, Constants.GroupKits, data, ref headerType);

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
        }

        /// <summary>
        /// добавить раздел
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        /// <param name="aGroupDic"></param>        
        private void AddGroup(DataTable aTable, string aGroupName, IDictionary<string, Group> aGroupDic, ref HeaderType aHeaderType)
        {   
            if (aGroupDic == null || !aGroupDic.ContainsKey(aGroupName))
                return;

            if (!aGroupDic.TryGetValue(aGroupName, out var group))
                return;

            // если в разделе нет ни одного непустого компонента ни в одной подгруппе
            if (IsEmptyGroup(group))
                return;

            // для переноса заголовков на одну страницу с данными заведем признак что данному наименование предшествует название раздела
            HeaderType nextHeaderType = HeaderType.Subgroup;            

            // наименование раздела
            AddEmptyRow(aTable);
            if (!string.IsNullOrEmpty(aGroupName))
            {
                ShiftToNextPageIfEnd(aTable, DocType.Specification, aHeaderType);
                AddGroupName(aTable, aGroupName);
                AddEmptyRow(aTable);                
                nextHeaderType = HeaderType.Group_Subgroup;
            }
            aHeaderType = HeaderType.Group;

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
                    AddComponents(aTable, сomponents, setPos, changeComponentName);
                    AddEmptyRow(aTable);                    
                    nextHeaderType = HeaderType.Subgroup;
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

                    AddSubgroup(aTable, subGroupName, subgroup.Value.Components, nextHeaderType, changeComponentName);
                    AddEmptyRow(aTable);
                    nextHeaderType = HeaderType.Subgroup;                    
                }
            }

            // отдельно запишем подгруппу "Прочие"
            if (group.SubGroups.TryGetValue(Constants.SUBGROUPFORSINGLE, out var subgroup_other))
            {
                if (subgroup_other.Components.Count > 0)
                {                    
                    AddSubgroup(aTable, Constants.SUBGROUPFORSINGLE, subgroup_other.Components, nextHeaderType, ChangeNameBySubGroupName.AddSubgroupName);
                    AddEmptyRow(aTable);
                }                
            }

            if (IsPCBSpecification)
            {
                changeComponentName = ChangeNameBySubGroupName.AddSubgroupName;
                AddComponents(aTable, сomponents, setPos, changeComponentName);
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
                                 HeaderType aHeaderType,
                                 ChangeNameBySubGroupName aChangeComponentName = ChangeNameBySubGroupName.WithoutChanges)
        {
            if (!aSortComponents.Any())            
                return false;

            // не будем добавлять имя подгруппы если надо добавлять наименование группы каждому компоненту
            if (aChangeComponentName != ChangeNameBySubGroupName.AddSubgroupName)
            {                
                ShiftToNextPageIfEnd(aTable, DocType.Specification, aHeaderType);

                AddGroupName(aTable, aGroupName, false, TextAlignment.LEFT);
            }

            AddComponents(aTable, aSortComponents, true, aChangeComponentName);                        
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
                                   bool aSetPos = true, 
                                   ChangeNameBySubGroupName aChangeComponentName = ChangeNameBySubGroupName.WithoutChanges)
        {
            DataRow row;
            foreach (var component in aSortComponents)
            {   
                string component_name = component.GetProperty(Constants.ComponentName);
                string prepared_component_name = component_name;
                string groupSp = component.GetProperty(Constants.GroupNameSp);
                float component_count = GetComponentCount(component, string.Equals(groupSp, Constants.GroupDoc));

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
                    ++_position;
                    // если элемент не пустой
                    if (nonEmptyComponent)
                        row[Constants.ColumnPosition] = new FormattedString { Value = _position.ToString(), TextAlignment = TextAlignment.CENTER };

                    string posComponentName = DataPreparationUtils.GetNameForPositionDictionary(component);
                    var configNames = aTable.TableName.Split(',');
                    foreach (var cfgName in configNames)
                    {
                        string key = ($"{cfgName} {groupSp}").Trim();                        
                        if (!_Positions.ContainsKey(key))
                            _Positions.Add(key, new List<Tuple<string, int>>() { new Tuple<string, int>(posComponentName, _position) });
                        else
                            _Positions[key].Add(new Tuple<string, int>(posComponentName, _position));
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
            AddColumn(table, Constants.ColumnQuantity, "Кол.",         typeof(Single));
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
            ShiftToNextPageIfEnd(aTable, DocType.Specification, HeaderType.AppData);

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
        private float GetComponentCount(Component aComponent, bool ZeroCount) {

            if (ZeroCount)            
                return 0;
            
            string aCountStr = aComponent.GetProperty(Constants.ComponentCountDev);
            float count = 1;
            if (!string.IsNullOrEmpty(aCountStr)) {
                if (Single.TryParse(aCountStr, out var cnt)) {
                    count = cnt;                    
                }
            }

            return (float)((count > (float)aComponent.Count) ?  count : aComponent.Count);
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

        private bool IsEmptyGroup(Group aGroup)
        {            
            if (!IsEmptyComponents(aGroup.Components))
                return false;
                            
            foreach (var subgr in aGroup.SubGroups)
            {   
                if (!IsEmptyGroup(subgr.Value))
                    return false;
            }
            return true;
        }
                

        private bool IsEmptyComponents(List<Component> aComponents)
        {
            if (aComponents?.Count == 0)
                return true;

            var nonzero_components = aComponents.Where(cmp => !string.IsNullOrEmpty(cmp.GetProperty(Constants.ComponentName))).ToList();
            if (nonzero_components == null || nonzero_components.Count == 0)
                return true;

            return false;
        }

    }
}
