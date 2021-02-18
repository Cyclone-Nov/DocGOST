using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GostDOC.Common;
using GostDOC.Models;
using GostDOC.PDF;
using iText.Layout.Properties;

namespace GostDOC.DataPreparation
{
    internal class ElementListDataPreparer : BasePreparer 
    {    
        private string SchemaDesignation = string.Empty;

        public override string GetDocSign(Configuration aMainConfig)
        {        
            SchemaDesignation = GetSchemaDesignation(aMainConfig, out var DocCode);
            return DocCode;
        }

        /// <summary>
        /// формирование таблицы данных
        /// </summary>
        /// <param name="aConfigs"></param>
        /// <returns></returns>    
        public override DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs)
        {
            // выбираем основную конфигурацию        
            if (!aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var mainConfig))
                return null;

            var data = mainConfig.Specification;

            SchemaDesignation = GetSchemaDesignation(mainConfig, out var DocCode);        
        
            appliedParams.Clear();
            appliedParams.Add(Constants.AppParamDocSign, DocCode);

            // из остальных конфигураций получаем список словарей с соответсвующими компонентами
            var otherConfigsElements = MakeComponentDesignatorsDictionaryOtherConfigs(aConfigs);

            // инициализируем таблицу данных
            DataTable table = CreateTable("ElementListData");
            Dictionary<string, Tuple<string, Component, uint>> allComponentsDic = null;

            void AddComponents(string aGroupName) 
            {                
                if (data.TryGetValue(aGroupName, out var group)) 
                {              
                    if (group.Components.Count() > 0 || group.SubGroups.Count() > 0) {
                        // подготавливаем список из всех компонентов
                        if (allComponentsDic == null)                
                            allComponentsDic = PrepareComponentsList(group);
                    else
                        allComponentsDic.AddRange(PrepareComponentsList(group));
                    }
                }           
            }

            // работаем по основной конфигурации,нужны только компоненты из раздела "Прочие изделия" и "Сборочные единицы"        
            AddComponents(Constants.GroupOthers);
            AddComponents(Constants.GroupAssemblyUnits);
            AddComponents(Constants.GroupDetails);

            if (allComponentsDic!= null)
            {
                FillDataTable(table, allComponentsDic, otherConfigsElements, SchemaDesignation);            
                RemoveLastEmptyRows(table);            
            }

            return table;
        }


        /// <summary>
        /// заполнить таблицу данных
        /// </summary>
        /// <param name="aTable">таблица данных</param>
        /// <param name="aGroupName">имя группы</param>
        /// <param name="aComponentsDic">словарь компонентов с ключем по позиционому обозначению, объединных</param>
        /// <param name="aOtherComponents"></param>
        /// <param name="aSchemaDesignation">обзначение схемы</param>
        private void FillDataTable(
                DataTable aTable,
                Dictionary<string, Tuple<string, Component, uint>> aComponentsDic,                
                IEnumerable<Dictionary<string, Component>> aOtherComponents,
                string aSchemaDesignation)
        {
            if (!aComponentsDic.Any()) 
                return;

            string changed_name = $"см. табл. {aSchemaDesignation}";
            string disabled_name = "Не устанавливать";
                         
            // отсортируем компоненты
            var comparer = new DesignatorIDComparer(); 
            var component_pair_arr = aComponentsDic.OrderBy(key => key.Key, comparer).ToArray();

            var groupNames = MakeGroupNamesDic(component_pair_arr);

            bool addGroupNameToNameField = true;
            int countDifferentComponents = 0;
            
            for (int i = 0; i < component_pair_arr.Length;)
            {
                var united_component = component_pair_arr[i].Value.Item2;
                string first_designator = component_pair_arr[i].Key;
                string last_designator = component_pair_arr[i].Value.Item1;
                uint count = component_pair_arr[i].Value.Item3;

                string designator = GetDesignator(first_designator, last_designator, count);
                string doc = united_component.GetProperty(Constants.ComponentDoc);                
                string component_name = united_component.GetProperty(Constants.ComponentName);
                string component_sign = united_component.GetProperty(Constants.ComponentSign);                
                string subGroupName = united_component.GetProperty(Constants.SubGroupNameSp);

                bool component_disabled = IsComponentDisabled(united_component);
                bool component_disabled_anywhere = DisabledInOtherConfigs(first_designator, aOtherComponents) && component_disabled;

                // определим надо ли будет изменить название компонента при выводе в документ
                bool haveToChangeName = component_disabled_anywhere || DifferNameInOtherConfigs(first_designator, component_name, aOtherComponents);

                List<string> component_designators = new List<string> { designator };
                countDifferentComponents++;

                
                int j = i + 1;
                var component_count = count;
                int sameComponents = 1;
                if (j < component_pair_arr.Length && !haveToChangeName)
                {
                    bool same;
                    do
                    {
                        var next_united_component = component_pair_arr[j].Value.Item2;
                        var next_first_designator = component_pair_arr[j].Key;
                        var next_last_designator = component_pair_arr[j].Value.Item1;
                        uint next_count = component_pair_arr[j].Value.Item3;
                        string componentNext_name = next_united_component.GetProperty(Constants.ComponentName);
                        string nextSubGroupName = next_united_component.GetProperty(Constants.SubGroupNameSp);
                        string componentNext_sign = next_united_component.GetProperty(Constants.ComponentSign);
                        bool next_component_disabled = string.Equals(next_united_component.GetProperty(Constants.ComponentPresence), "0");

                        if (string.Equals(component_name, componentNext_name) && 
                            string.Equals(subGroupName, nextSubGroupName) && 
                            string.Equals(component_sign, componentNext_sign) &&
                            (next_component_disabled == component_disabled))
                        {
                            same = true;
                            component_count+= next_count;
                            sameComponents++;
                            j++;
                            component_designators.Add(GetDesignator(next_first_designator, next_last_designator, next_count));
                        } else
                            same = false;
                    } while (same && j < component_pair_arr.Length);
                }
                i = j;

                // если позиционное обозначение есть в словаре имен групп, то запишем наименование группы если оно есть                
                if (groupNames.ContainsKey(designator)) 
                {   
                    string groupName = groupNames[designator].Item1;
                    int ncount = groupNames[designator].Item2;
                    addGroupNameToNameField = string.IsNullOrEmpty(groupName) || (ncount == sameComponents);
                    
                    // первая строка на первом листе не может быть пустой
                    if(aTable.Rows.Count > 0)
                        AddEmptyRow(aTable);

                    // если не надо добавлять имя группы в строку с именем, то добавим имя группы отдельно
                    if (!addGroupNameToNameField)
                    {                        
                        AddGroupName(aTable, GetGroupNameByCount(groupName, false));
                    }                    

                    countDifferentComponents = 0;
                }

                var designators = MakeComponentDesignatorsString(component_designators);
                if (string.Equals(doc, component_name))
                    doc = string.Empty;

                string name = string.Empty;
                string mainGroupName = united_component.GetProperty(Constants.GroupNameSp);

                // если надо менять наименование
                if (haveToChangeName)
                {
                    if (component_disabled_anywhere)
                    {
                        name = disabled_name;
                    }
                    else
                    {
                        name = (addGroupNameToNameField) ? $"{GetGroupNameByCount(subGroupName, true)} {changed_name}": changed_name;
                    }                    
                }
                else
                {                    
                    if (string.Equals(mainGroupName, Constants.GroupAssemblyUnits) ||
                        string.Equals(mainGroupName, Constants.GroupDetails))
                    {
                        name = $"{component_name.Trim()} {united_component.GetProperty(Constants.ComponentSign)}";
                    }
                    else
                    {
                        name = (addGroupNameToNameField) ? $"{GetGroupNameByCount(subGroupName, true)} {component_name} {doc}" : $"{component_name} {doc}";
                    }
                }
                
                var note = united_component.GetProperty(Constants.ComponentNote);
                AddNewRow(aTable, designators, name, component_count, note);
            }

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
        }

        /// <summary>
        /// подготовить список компонентов
        /// </summary>
        /// <param name="aGroup">группа с компонентами</param>
        /// <returns>словарь </returns>
        private Dictionary<string, Tuple<string, Component, uint>> 
        PrepareComponentsList(Group aMainGroup)
        {
            var dic = new Dictionary<string, Tuple<string, Component, uint>>();
            
            // выбираем из корня группы компоненты с заполненным тегом Позиционное обозначение
            var mainсomponents = aMainGroup.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));

            //  заполним словарь компонентов
            FillPrepareDictionary(dic, mainсomponents);

            // выбираем из подгрупп группы компоненты с заполненным тегом Позиционное обозначение и заполним словарь
            foreach (var subgroup in aMainGroup.SubGroups.OrderBy(key => GroupNameConverter.GetSymbol(key.Key)))
            {   
                var сomponents = subgroup.Value.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));
                FillPrepareDictionary(dic, сomponents);
            }
            return dic;
        }

        /// <summary>
        /// заполнение словарья компонентов
        /// </summary>
        /// <param name="aDic">словарь компонентов</param>
        /// <param name="aComponents">список компонентов</param>
        private void FillPrepareDictionary(Dictionary<string, Tuple<string, Component, uint>> aDic,
                                           IEnumerable<Component> aComponents)
        {
            foreach (var component in aComponents)
            {
                string designator = component.GetProperty(Constants.ComponentDesignatorID);
                string note = component.GetProperty(Constants.ComponentNote);

                // если позиционное обозначение и примечания совпадают, то сделаем примечание пустым
                if (string.Equals(designator, note))
                {
                    component.SetPropertyValue(Constants.ComponentNote, string.Empty);
                }

                // позиционное обозначение может объединять несколько позциионных обозначений - разобем их
                var designators = designator.Split(new char[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    if (designators.Length == 1)
                    {
                        aDic.Add(designator.Trim(), new Tuple<string, Component, uint>(string.Empty, component, component.Count));
                    } else if (designators.Length == 2)
                    {
                        if (designator.Contains('-'))
                        {
                            aDic.Add(designators[0].TrimStart(), new Tuple<string, Component, uint>(designators[1].TrimEnd(), component, component.Count));
                        } else
                        {
                            string firstDesignator = designators[0].TrimEnd();
                            string lastDesignator = designators[1].TrimStart();
                            if (GetCountByDesignators(firstDesignator, lastDesignator) == 2)
                            {
                                aDic.Add(firstDesignator, new Tuple<string, Component, uint>(lastDesignator, component, 2));
                            } else
                            {
                                aDic.Add(firstDesignator, new Tuple<string, Component, uint>(string.Empty, component, 1));
                                aDic.Add(lastDesignator, new Tuple<string, Component, uint>(string.Empty, component, 1));
                            }
                        }
                    } else
                    {
                        designators = designator.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < designators.Length;)
                        {
                            var first_designator = designators[i].Trim();
                            if (designators[i].Contains('-'))
                            {
                                int ind = first_designator.IndexOf('-');
                                string keyDesigantor = first_designator.Substring(0, ind);
                                string lastDesigantor = first_designator.Substring(ind + 1);
                                uint subcount = GetCountByDesignators(keyDesigantor, lastDesigantor);
                                Tuple<string, Component, uint> component_rec = new Tuple<string, Component, uint>(lastDesigantor, component, subcount);
                                aDic.Add(keyDesigantor, component_rec);
                                i++;
                            } else
                            {
                                if (designators.Length - i > 1)
                                {
                                    var next_designator = designators[i + 1].Trim();
                                    if (!next_designator.Contains('-') && GetCountByDesignators(first_designator, next_designator) == 2)
                                    {
                                        aDic.Add(first_designator, new Tuple<string, Component, uint>(next_designator, component, 2));
                                        i += 2;
                                    } else
                                    {
                                        aDic.Add(first_designator, new Tuple<string, Component, uint>(string.Empty, component, 1));
                                        i++;
                                    }
                                } else
                                {
                                    aDic.Add(first_designator, new Tuple<string, Component, uint>(string.Empty, component, 1));
                                    i++;
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    // todo: log
                }

            }
        }


        /// <summary>
        /// создание таблицы данных для документа Перечень элементов
        /// </summary>
        /// <param name="aDataTableName"></param>
        /// <returns></returns>
        protected override DataTable CreateTable(string aDataTableName) {
            DataTable table = new DataTable(aDataTableName);
            DataColumn column = new DataColumn("id", typeof(Int32));
            column.Unique = true;
            column.AutoIncrement = true;
            column.Caption = "id";
            table.Columns.Add(column);
            table.PrimaryKey = new DataColumn[] {column};

            void AddColumn(string aColumnName, string aCaption, Type aType) =>
                this.AddColumn(table, aColumnName, aCaption, aType);

            AddColumn(Constants.ColumnPosition, "Поз. обозначение", typeof(FormattedString));
            AddColumn(Constants.ColumnName, "Наименование", typeof(FormattedString));
            AddColumn(Constants.ColumnQuantity, "Кол.", typeof(Int32));
            AddColumn(Constants.ColumnFootnote, "Примечание", typeof(FormattedString));

            return table;
        }

        /// <summary>
        /// добавить новую строку (или строки) с данными в таблицу
        /// </summary>
        /// <param name="aTable">a table.</param>
        /// <param name="aDesignators">a designators.</param>
        /// <param name="aName">a name.</param>
        /// <param name="aCount">a count.</param>
        /// <param name="aNote">a note.</param>
        private void AddNewRow(DataTable aTable, string aDesignators, string aName, uint aCount, string aNote)
        {
            string[] designatorarr = PdfUtils.SplitStringByWidth(Constants.ItemsListColumn1PositionWidth, aDesignators, new char[] { ',', ' ', '-' }, Constants.ItemListFontSize).ToArray();
            string[] namearr = PdfUtils.SplitStringByWidth(Constants.ItemsListColumn2NameWidth, aName, new char[] { '.', ' ', '-' }, Constants.ItemListFontSize, true).ToArray();
            string[] notearr = PdfUtils.SplitStringByWidth(Constants.ItemsListColumn4FootnoteWidth, aNote, new char[] { ',', ' ', '-' }, Constants.ItemListFontSize).ToArray();

            var row = aTable.NewRow();
            row[Constants.ColumnPosition] = new FormattedString { Value = designatorarr.First() };
            row[Constants.ColumnName] = new FormattedString { Value = namearr.First() };
            row[Constants.ColumnQuantity] = aCount;
            row[Constants.ColumnFootnote] = new FormattedString { Value = notearr.First() };
            aTable.Rows.Add(row);

            int max = Math.Max(namearr.Length, notearr.Length);
            max = Math.Max(max, designatorarr.Length);
            if (max > 1)
            {
                int ln_name = namearr.Length;
                int ln_note = notearr.Length;
                int ln_designator = designatorarr.Length;

                for (int ln = 1; ln < max; ln++)
                {
                    row = aTable.NewRow();
                    row[Constants.ColumnPosition] = new FormattedString { Value = (ln_designator > ln) ? designatorarr[ln] : string.Empty };
                    row[Constants.ColumnName] = new FormattedString { Value = (ln_name > ln) ? namearr[ln] : string.Empty };
                    row[Constants.ColumnFootnote] = new FormattedString { Value = (ln_note > ln) ? notearr[ln] : string.Empty };
                    aTable.Rows.Add(row);
                }
            }
        }
               

        /// <summary>
        /// добавить имя группы в таблицу
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        private void AddGroupName(DataTable aTable, string aGroupName) {
            if (string.IsNullOrEmpty(aGroupName)) 
                return;
                            
            int groupNameRowNumber = aTable.Rows.Count + 1;
            int firstComponentRowNumber = groupNameRowNumber + 1;
            int groupNamePageNumber = CommonUtils.GetCurrentPage(DocType.ItemsList, groupNameRowNumber);
            int firstComponentPageNumber = CommonUtils.GetCurrentPage(DocType.ItemsList, firstComponentRowNumber);
            if (firstComponentPageNumber > groupNamePageNumber)
            {
                AddEmptyRowsToEndPage(aTable, DocType.ItemsList, groupNameRowNumber);
            }

            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString { Value = aGroupName, TextAlignment = TextAlignment.LEFT };
            aTable.Rows.Add(row);
        }

        ///// <summary>
        ///// добавить в таблицу данных стандартные документы на поставку при наличии перед перечнем компонентов
        ///// </summary>
        ///// <param name="aGroupName">имя группы</param>
        ///// <param name="aComponents">список компонентов</param>
        ///// <param name="aTable">таблица данных</param>
        ///// <param name="aStandardDic">словарь со стандартными документами на поставку</param>
        ///// <returns>true - стандартные документы добавлены </returns>
        //private bool AddStandardDocsToTable(string aGroupName, DataTable aTable,
        //                                    Dictionary<string, Tuple<string, Component, uint>> aComponentsDic,
        //                                    Dictionary<string, Dictionary<string, List<string>>> aStandardDic)
        //{
        //    bool isApplied = false;
        //    DataRow row;
        //    bool applied = false;
        //    Dictionary<string, List<string>> standards;
        //    if (aStandardDic.TryGetValue(aGroupName, out standards))
        //    {
        //        foreach (var item in standards) 
        //        {
        //            if (item.Value.Count() > MIN_ITEMS_FOR_COMBINE_BY_STANDARD) {
        //                if (!applied) {
        //                    applied = true;
        //                    //AddEmptyRow(aTable);
        //                }

        //                row = aTable.NewRow();
        //                var index = item.Value.First();
        //                string name = $"{GetGroupNameByCount(aGroupName, false)} {aComponentsDic[index].Item2.GetProperty(Constants.ComponentType)} {item.Key}";
        //                row[Constants.ColumnName] = name;
        //                aTable.Rows.Add(row);
        //                isApplied = true;
        //            }
        //        }
        //    }

        //    return isApplied;
        //}


        ///// <summary>
        ///// получить количество компонентов
        ///// </summary>
        ///// <param name="aCountStr"></param>
        ///// <returns></returns>
        //private int GetComponentCount(string aCountStr) {
        //    int count = 1;
        //    if (!string.IsNullOrEmpty(aCountStr)) {
        //        if (!Int32.TryParse(aCountStr, out count)) {
        //            count = 1;
        //            //throw new Exception($"Не удалось распарсить значение свойства \"Количество на изд.\" для компонента с именем {component_name}");
        //        }
        //    }

        //    return count;
        //}

        private uint GetCountByDesignators(string aFirstDesigantor, string aLastDesigantor)
        {            
            var digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            int indFirst = aFirstDesigantor.IndexOfAny(digits);
            int indLast = aLastDesigantor.IndexOfAny(digits);
            var firstValstr = aFirstDesigantor.Substring(indFirst);
            var lastValstr = aLastDesigantor.Substring(indLast);
            uint firstVal = UInt32.Parse(firstValstr);
            uint lastVal = UInt32.Parse(lastValstr);
            return (uint)(lastVal - firstVal + 1);
        }


        /// <summary>
        /// составить строку для столбца "Поз. обозначение"
        /// </summary>
        /// <param name="aDesignators">список позиционных обозначений всех индентичных элементов</param>
        /// <returns></returns>
        private string MakeComponentDesignatorsString(List<string> aDesignators) {
            string designator = string.Empty;
            if (aDesignators.Count() == 1)
                designator = aDesignators.First();
            else if (aDesignators.Count() == 2)
                designator = $"{aDesignators.First()},{aDesignators.Last()}";
            else                            
                designator = $"{aDesignators.First()}-{aDesignators.Last()}";            

            return designator;
        }

        /// <summary>
        /// составить строку для столбца "Поз. обозначение"
        /// </summary>
        /// <param name="aDesignators">список позиционных обозначений всех индентичных элементов</param>
        /// <returns></returns>
        private string GetDesignator(string aFirstDesignator, string aLastDesignator, uint count)
        {            
            if (count == 1)
                return aFirstDesignator;
            else if (count == 2)
                return $"{aFirstDesignator},{aLastDesignator}";
            else
            {
                return $"{aFirstDesignator}-{aLastDesignator}";
            }
        }

        /// <summary>
        /// удаление пустых строк в конце таблицы данных
        /// </summary>
        /// <param name="table">The table.</param>
        private void RemoveLastEmptyRows(DataTable table)
        {
            if (table.Rows.Count > 1)
            {
                bool empty_str = false;
                int last_index = table.Rows.Count -1;
                do
                {
                    empty_str = false;
                    var arr = table.Rows[last_index].ItemArray;
                    if (string.IsNullOrEmpty(arr[1].ToString()) &&
                        string.IsNullOrEmpty(arr[2].ToString()) &&
                        string.IsNullOrEmpty(arr[3].ToString()) &&
                        string.IsNullOrEmpty(arr[4].ToString()))
                    {
                        empty_str = true;                        
                        table.Rows.RemoveAt(last_index);
                        last_index = table.Rows.Count - 1;
                    }                     
                }
                while (empty_str);
            }
        }

        ///// <summary>
        ///// Checks the equals next subgroup.
        ///// </summary>
        ///// <param name="aIndex">a index.</param>
        ///// <param name="aSubGroupName">Name of a sub group.</param>
        ///// <param name="aComponentsPairs">a components pairs.</param>
        ///// <returns></returns>
        //private bool CheckEqualsNextSubgroup(int aIndex, string aSubGroupName, KeyValuePair<string, Tuple<string, Component, uint>>[] aComponentsPairs)
        //{
        //    if (aIndex + 1 != aComponentsPairs.Length)
        //    {
        //        var nextcomponent = aComponentsPairs[aIndex + 1].Value;
        //        string nextSubGroupName = nextcomponent.Item2.GetProperty(Constants.SubGroupNameSp);
        //        return !string.Equals(aSubGroupName, nextSubGroupName);
        //    }

        //    return false;
        //}

        ///// <summary>
        ///// Determines whether this instance [can output standard docs] the specified a designator.
        ///// </summary>
        ///// <param name="aDesignator">a designator.</param>
        ///// <returns>
        /////   <c>true</c> if this instance [can output standard docs] the specified a designator; otherwise, <c>false</c>.
        ///// </returns>
        //private bool CanOutputStandardDocs(string aDesignator)
        //{
        //    if (string.IsNullOrEmpty(aDesignator))
        //        return false;

        //    int index = aDesignator.IndexOfAny(new char[] { '0','1', '2', '3', '4', '5', '6', '7', '8', '9'});
        //    string symbols = aDesignator.Substring(0, index).ToUpper();

        //    if (string.Equals(symbols, "R") ||
        //        string.Equals(symbols, "C") ||
        //        string.Equals(symbols, "L") ||
        //        string.Equals(symbols, "XP")||
        //        string.Equals(symbols, "XS"))
        //        return true;

        //    return false;
        //}

        /// <summary>
        /// Gets the group name by count.
        /// </summary>
        /// <param name="aGroupName">Name of a group.</param>
        /// <param name="aSingle">if set to <c>true</c> [a single].</param>
        /// <returns></returns>
        private string GetGroupNameByCount(string aGroupName, bool aSingle)
        {
            if (aGroupName.Contains(@"\"))
            {
                string[] split = aGroupName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 2)
                {
                    return aSingle ? split[0] : split[1];
                }
            } 
            return aGroupName;
        }

        /// <summary>
        /// Makes the group names dic.
        /// </summary>
        /// <param name="aSortedComponents">a sorted components.</param>
        /// <returns> dictionary with key = desigantor of first component of group, value = Tuple of group name and count components in group </returns>
        private IDictionary<string, Tuple<string,int>>
        MakeGroupNamesDic(KeyValuePair<string, Tuple<string, Component, uint>>[] aSortedComponents)
        {
            var groupNamesDic = new Dictionary<string, Tuple<string, int>>();
            if (aSortedComponents.Length > 1)
            {                
                string lastGroupName = aSortedComponents[0].Value.Item2.GetProperty(Constants.SubGroupNameSp);
                string lastDesignatorType = GetDesignatorType(aSortedComponents[0].Key);
                string firstDesignator = aSortedComponents[0].Key;
                int countSubGroupCanges = 0;
                int countComponents = 1;
                for (int i = 1; i < aSortedComponents.Length; i++)
                {
                    var component = aSortedComponents[i].Value.Item2;
                    string currDesignatorType = GetDesignatorType(aSortedComponents[i].Key);
                    string currGroupName = component.GetProperty(Constants.SubGroupNameSp);
                    countComponents++;

                    // если происходит смена типа позиционного обозначения, то создадим новую группу
                    if (!string.Equals(lastDesignatorType, currDesignatorType))
                    {
                        groupNamesDic.Add(firstDesignator, new Tuple<string, int>(countSubGroupCanges > 0 ? "" : lastGroupName, 
                                                                                  countComponents - 1));
                        firstDesignator = aSortedComponents[i].Key;
                        countSubGroupCanges = 0;
                        countComponents = 1;
                        lastGroupName = currGroupName;
                        lastDesignatorType = currDesignatorType;
                    }

                    // увеличим количество смен имени группы при одном и том же  типе позиционного обозначения
                    if (!string.Equals(lastGroupName, currGroupName))
                    {
                        countSubGroupCanges++;
                        lastGroupName = currGroupName;
                    }
                }

                groupNamesDic.Add(firstDesignator, new Tuple<string, int>(countSubGroupCanges > 0 ? "" : lastGroupName, countComponents));
            }

            return groupNamesDic;
        }

        private string GetDesignatorType(string aFullDesignator)
        {
            int index = aFullDesignator.IndexOfAny(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' });
            return aFullDesignator.Substring(0, index);
        }

    }
}
