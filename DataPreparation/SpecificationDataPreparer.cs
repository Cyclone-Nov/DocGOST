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
    internal class SpecificationDataPreparer : BasePreparer
    {

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
            DataTable table = CreateTable("SpecificationData");

            // заполнение данных из основного исполнения
            FillConfiguration(table, mainConfig);

            // заполним переменные данные исполнений, если они есть
            if (listPreparedConfigs != null && listPreparedConfigs.Count() > 0)
            {
                AddAppDataSign(table);

                foreach (var config in listPreparedConfigs.OrderBy(key => key.Key))
                {
                    FillConfiguration(table, config.Value, config.Key, false);
                }
            }

            RemoveLastEmptyRows(table);

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
            IDictionary<string, Configuration> preparedConfigs = new Dictionary<string, Configuration>();
            aMainConfig = new Configuration();
            aMainConfig.Graphs = mainConfig.Graphs;
            var deltaMainConfig = new Configuration();
            deltaMainConfig.Graphs = mainConfig.Graphs;
            var mainData = mainConfig.Specification;

            Dictionary<string, Configuration> otherConfigs = new Dictionary<string, Configuration>();
            foreach (var config in aConfigs)
            {
                if (!string.Equals(config.Key, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase))
                {
                    otherConfigs.Add(config.Key, config.Value.DeepCopy());
                }
            }

            foreach (var group in mainData.OrderBy(key => key.Key))
            {
                foreach (var component in group.Value.Components)
                {
                    if (CheckComponent(otherConfigs, component, group.Key))
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
                        //otherConfigs[Constants.MAIN_CONFIG_INDEX].Bill[group.Key].Components.Add(component);
                    }
                }

                foreach (var subgroup in group.Value.SubGroups.OrderBy(key2 => key2.Key))
                {
                    foreach (var component in subgroup.Value.Components)
                    {
                        if (CheckComponent(otherConfigs, component, group.Key, subgroup.Key))
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

            preparedConfigs = otherConfigs;
            preparedConfigs.Add(Constants.MAIN_CONFIG_INDEX, deltaMainConfig);

            return preparedConfigs;
        }

        /// <summary>
        /// заполнить таблицу данных <paramref name="aTable"/> данными из конфигурации <paramref name="aConfig"/>
        /// </summary>
        /// <param name="aTable">итоговая таблица с данными</param>
        /// <param name="aConfig">конфигурация</param>
        /// <param name="aManyConfigs">признак наличия нескольких конфигураций: если <c>true</c> то несколько конфигураций</param>
        private void FillConfiguration(DataTable aTable, Configuration aConfig, string aConfigName = "", bool aMainConfig = true)
        {
            string configName = "";
            if (!aMainConfig)
            {
                if (aConfig.Graphs.TryGetValue(Constants.GRAPH_2, out var sign)) // получим значение графы "Обозначение"
                {
                    if (string.Equals(aConfigName, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase))
                    {
                        configName = sign; // "Обозначение"
                    } else
                    {
                        configName = $"{sign}{aConfigName}"; // "Обозначение"-"aConfigName"
                    }
                }

                var row = aTable.NewRow();
                row[Constants.ColumnName] = configName;
                row[Constants.ColumnTextFormat] = "1";
                aTable.Rows.Add(row);
                AddEmptyRow(aTable);
            }

            var data = aConfig.Specification;
            int position = 0;

            AddGroup(aTable, Constants.GroupDoc, data, ref position);
            AddGroup(aTable, Constants.GroupComplex, data, ref position);
            AddGroup(aTable, Constants.GroupAssemblyUnits, data, ref position);
            AddGroup(aTable, Constants.GroupDetails, data, ref position);
            AddGroup(aTable, Constants.GroupStandard, data, ref position);
            AddGroup(aTable, Constants.GroupOthers, data, ref position);
            AddGroup(aTable, Constants.GroupMaterials, data, ref position);
            AddGroup(aTable, Constants.GroupKits, data, ref position);

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
        private void AddGroup(DataTable aTable, string aGroupName, IDictionary<string, Group> aGroupDic, ref int aPos)
        {
            if (aGroupDic == null || !aGroupDic.ContainsKey(aGroupName))
                return;

            if (!aGroupDic.TryGetValue(aGroupName, out var group))
                return;

            if(group.Components.Count == 0 && group.SubGroups.Count == 0)
            {
                return;
            }

            // наименование раздела
            AddEmptyRow(aTable);
            if (AddGroupName(aTable, aGroupName))
                AddEmptyRow(aTable);

            var sortType = SortType.None;
            if (aGroupName == Constants.GroupDoc)
            {
                sortType = SortType.None;
            } else if (aGroupName == Constants.GroupComplex || aGroupName == Constants.GroupAssemblyUnits || aGroupName == Constants.GroupDetails)
            {
                sortType = SortType.SpComplex;
            } else if (aGroupName == Constants.GroupStandard)
            {
                sortType = SortType.SpStandard;
            } else if (aGroupName == Constants.GroupOthers)
            {
                sortType = SortType.SpOthers;
            } else if (aGroupName == Constants.GroupKits)
            {
                sortType = SortType.SpKits;
            }
            var sort = SortFactory.GetSort(sortType);

            var сomponents = sort.Sort(group.Components.ToList());
            AddComponents(aTable, сomponents, ref aPos, false);

            // добавляем подгруппы
            foreach (var subgroup in group.SubGroups.OrderBy(key => key.Key))
            {
                if (subgroup.Value.Components.Count > 0)
                {
                    var mainсomponents = sort.Sort(subgroup.Value.Components.ToList());
                    AddSubgroup(aTable, subgroup.Key, mainсomponents, ref aPos);
                }
            }

            if (aGroupName != Constants.GroupDoc)
            {
                AddEmptyRow(aTable);
                AddEmptyRow(aTable);
                aPos += 2;
            }
        }

        private bool AddSubgroup(DataTable aTable, string aGroupName, List<Component> aSortComponents, ref int aPos)
        {
            if (!aSortComponents.Any())
            {
                return false;
            }

            if (AddGroupName(aTable, aGroupName))
                AddEmptyRow(aTable);

            AddComponents(aTable, aSortComponents, ref aPos);

            AddEmptyRow(aTable);
            aTable.AcceptChanges();

            return true;
        }


        private void AddComponents(DataTable aTable, List<Component> aSortComponents, ref int aPos, bool aSetPos = true)
        {
            DataRow row;
            foreach (var component in aSortComponents)
            {   
                string component_name = component.GetProperty(Constants.ComponentName);
                uint component_count = component.Count;// GetComponentCount(component.GetProperty(Constants.ComponentCountDev));

                string[] namearr = PdfUtils.SplitStringByWidth(63, component_name).ToArray();
                var desigantor_id = component.GetProperty(Constants.ComponentDesignatiorID);
                var note = string.IsNullOrEmpty(desigantor_id) ? component.GetProperty(Constants.ComponentNote) : desigantor_id;
                string[] notearr = PdfUtils.SplitStringByWidth(22, note).ToArray();

                row = aTable.NewRow();
                row[Constants.ColumnFormat] = new FormattedString { Value = component.GetProperty(Constants.ComponentFormat) };
                row[Constants.ColumnZone] = new FormattedString{Value = component.GetProperty(Constants.ComponentZone)};                
                if (aSetPos)
                {
                    ++aPos;
                    row[Constants.ColumnPosition] = new FormattedString { Value = aPos.ToString() };
                }

                string designation = component.GetProperty(Constants.ComponentSign);
                //if (dataToFill.GroupName == Constants.GroupDoc) {
                //designation += component.GetProperty(Constants.ComponentDocCode);
                //}
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
                        row[Constants.ColumnZone] = new FormattedString { Value = "1" }; // используем данную колонку для установки признака переноса строки ????
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
        private bool AddGroupName(DataTable aTable, string aGroupName) 
        {
            if (string.IsNullOrEmpty(aGroupName)) return false;
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString {Value = aGroupName, IsUnderlined = true, TextAlignment = TextAlignment.CENTER};
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
            DataColumn column = new DataColumn("id", System.Type.GetType("System.Int32"));
            column.Unique = true;
            column.AutoIncrement = true;
            column.Caption = "id";
            table.Columns.Add(column);
            table.PrimaryKey = new DataColumn[] { column };

            void AddColumn(string aColumnName, string aCaption, Type aType) => this.AddColumn(table,aColumnName,aCaption,aType);

            AddColumn(Constants.ColumnFormat, "Формат", typeof(FormattedString));
            AddColumn(Constants.ColumnZone, "Зона", typeof(FormattedString) );
            AddColumn(Constants.ColumnPosition, "Поз.", typeof(FormattedString));
            AddColumn(Constants.ColumnSign, "Обозначение", typeof(FormattedString));
            AddColumn(Constants.ColumnName, "Наименование", typeof(FormattedString));
            AddColumn(Constants.ColumnQuantity, "Кол.", typeof(Int32));
            AddColumn(Constants.ColumnFootnote, "Примечание", typeof(FormattedString));

            return table;
        }
       

        /// <summary>
        /// добавить в таблицу данны надписи "Переменные данные исполнений"
        /// </summary>
        /// <param name="table">The table.</param>
        private void AddAppDataSign(DataTable aTable)
        {
            AddEmptyRow(aTable);
            var row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString{Value= "Переменные данные"};
            row[Constants.ColumnProductCode] = new FormattedString{Value = "исполнений"};
            aTable.Rows.Add(row);
            AddEmptyRow(aTable);
        }

        /// <summary>
        /// получить количество компонентов
        /// </summary>
        /// <param name="aCountStr"></param>
        /// <returns></returns>
        private int GetComponentCount(string aCountStr) {
            int count = 1;
            if (!string.IsNullOrEmpty(aCountStr)) {
                if (!Int32.TryParse(aCountStr, out count)) {
                    count = 1;
                    //throw new Exception($"Не удалось распарсить значение свойства \"Количество на изд.\" для компонента с именем {component_name}");
                }
            }

            return count;
        }

        /// <summary>
        /// определение наличия компонента во всех конфигурациях
        /// </summary>
        /// <param name="aComponent">a component.</param>
        /// <param name="aGroupName">Name of a group.</param>
        /// <param name="aOtherConfigs">a other configs.</param>
        /// <returns></returns>
        private bool CheckComponent(Dictionary<string, Configuration> aOtherConfigs, Component aComponent, string aGroupName, string aSubGroupName = "")
        {
            List<Component> removeList = null;
            bool bRemoveGroup = false;
            Component removeComponent = null;
            bool result = false;

            foreach (var config in aOtherConfigs)
            {
                if (string.IsNullOrEmpty(aSubGroupName))
                {
                    bRemoveGroup = false;
                    if (config.Value.Specification.ContainsKey(aGroupName))
                    {
                        foreach (var othercomp in config.Value.Specification[aGroupName].Components)
                        {
                            if (EquealsSpecComponents(othercomp, aComponent))
                            {
                                removeList = config.Value.Specification[aGroupName].Components;
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
                                if (EquealsSpecComponents(othercomp, aComponent))
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


        private bool EquealsSpecComponents(Component aFirstComponent, Component aSecondComponent)
        {
            string name1 = aFirstComponent.GetProperty(Constants.ComponentName);
            string name2 = aSecondComponent.GetProperty(Constants.ComponentName);

            string entry1 = aFirstComponent.GetProperty(Constants.ComponentWhereIncluded);
            string entry2 = aSecondComponent.GetProperty(Constants.ComponentWhereIncluded);

            if (string.Equals(name1, name2) &&
                string.Equals(entry1, entry2) &&
                aFirstComponent.Count == aSecondComponent.Count)
            {
                return true;
            }

            return false;
        }


        private new void AddEmptyRow(DataTable aTable) 
        {
            DataRow row = aTable.NewRow();            
            aTable.Rows.Add(row);
        }

        private void RemoveLastEmptyRows(DataTable table)
        {
            if (table.Rows.Count > 1)
            {
                bool empty_str = false;
                int last_index = table.Rows.Count - 1;
                do
                {
                    empty_str = false;
                    var arr = table.Rows[last_index].ItemArray;
                    if (string.IsNullOrEmpty(arr[1].ToString()) &&
                        string.IsNullOrEmpty(arr[2].ToString()) &&
                        string.IsNullOrEmpty(arr[3].ToString()) &&
                        string.IsNullOrEmpty(arr[4].ToString()) &&
                        string.IsNullOrEmpty(arr[5].ToString()) &&
                        string.IsNullOrEmpty(arr[6].ToString()) &&
                        string.IsNullOrEmpty(arr[7].ToString()))
                    {
                        empty_str = true;
                        table.Rows.RemoveAt(last_index);
                        last_index = table.Rows.Count - 1;
                    }
                }
                while (empty_str);
            }
        }

    }
}
