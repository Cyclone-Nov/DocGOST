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
    internal class BillDataPreparer : BasePreparer
    {
        public override string GetDocSign(Configuration aMainConfig)
        {
            return "ВП";
        }

        /// <summary>
        /// формирование таблицы данных
        /// </summary>
        /// <param name="aConfigs"></param>
        /// <returns></returns>    
        public override DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs)
        {            
            if(aConfigs == null)
            { 
                // todo: add to log
                return null;
            }

            // подговтовим данные для переменных данных для исполнений
            Configuration mainConfig = null;
            var listPreparedConfigs = PrepareConfigs(aConfigs, out mainConfig);

            if (mainConfig == null)
            {
                // todo: add to log
                return null;
            }

            DataTable table = CreateTable("BillData");
            
            // заполнение данных из основного исполнения
            FillConfiguration(table, mainConfig);

            // заполним переменные данные исполнений, если они есть
            if (listPreparedConfigs != null && listPreparedConfigs.Count() > 0)
            {
                AddVariableConfigData(table);
                
                foreach (var config in listPreparedConfigs.OrderBy(key => key.Key))
                {
                    FillConfiguration(table, config.Value, config.Key, false);
                }
            }

            RemoveLastEmptyRows(table);

            // добавим оглавление если надо
            mainConfig.Graphs.TryGetValue(Constants.GRAPH_2, out var sign);
            AddContentToDataTable(table, sign);

            return table;           
        }

        /// <summary>
        /// создание таблицы данных
        /// </summary>
        /// <param name="aDataTableName"></param>
        /// <returns></returns>
        protected override DataTable CreateTable(string aDataTableName)
        {  
            DataTable table = new DataTable(aDataTableName);
            DataColumn column = new DataColumn("id", typeof(Int32));
            column.Unique = true;
            column.AutoIncrement = true;
            column.Caption = "id";
            table.Columns.Add(column);
            table.PrimaryKey = new DataColumn[] {column};

            void AddColumn(string aColumnName, string aCaption, Type aType) =>
                this.AddColumn(table, aColumnName, aCaption, aType);

            AddColumn(Constants.ColumnName, "Наименование", typeof(FormattedString));
            AddColumn(Constants.ColumnProductCode, "Код продукции", typeof(FormattedString));
            AddColumn(Constants.ColumnDeliveryDocSign, "Обозначение документа на поставку", typeof(FormattedString));
            AddColumn(Constants.ColumnSupplier, "Поставщик", typeof(FormattedString));
            AddColumn(Constants.ColumnEntry, "Куда входит (обозначение)", typeof(FormattedString));
            AddColumn(Constants.ColumnQuantityDevice, "Количество на изделие", typeof(Int32));
            AddColumn(Constants.ColumnQuantityComplex, "Количество в комплекты", typeof(Int32));
            AddColumn(Constants.ColumnQuantityRegul, "Количество на регулир.", typeof(Int32));
            AddColumn(Constants.ColumnQuantityTotal, "Количество всего", typeof(FormattedString));
            AddColumn(Constants.ColumnFootnote, "Примечание", typeof(FormattedString));
            AddColumn(Constants.ColumnTextFormat, "Форматирование текста", typeof(FormattedString));        

            return table;
        }

        /// <summary>
        /// добавить в таблицу данны надписи "Переменные данные исполнений"
        /// </summary>
        /// <param name="table">The table.</param>
        private void AddVariableConfigData(DataTable aTable)
        {
            AddEmptyRow(aTable);
            var row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString { Value = Constants.VariableConfigDataSig1, TextAlignment = TextAlignment.RIGHT };
            row[Constants.ColumnProductCode] = new FormattedString { Value = Constants.VariableConfigDataSig2, TextAlignment = TextAlignment.LEFT };            
            aTable.Rows.Add(row);
            AddEmptyRow(aTable);
        }

        /// <summary>
        /// подготовить данные конфигураций к выводу в таблицу данных
        /// </summary>
        /// <param name="aConfigs">a configs.</param>
        /// <param name="aMainConfig">a main configuration.</param>
        /// <returns></returns>
        private IDictionary<string, Configuration> PrepareConfigs(IDictionary<string, Configuration> aConfigs, out Configuration aMainConfig)
        {
            aMainConfig = null;
            if (aConfigs == null || !aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var mainConfig))               
                return null;            
            
            if (aConfigs.Count() == 1)
            {
                aMainConfig = mainConfig;
                return null;
            }

            // если конфигураций несколько            
            aMainConfig = new Configuration() { Graphs = mainConfig.Graphs }; //aMainConfig.Graphs = mainConfig.Graphs;            
            var deltaMainConfig = new Configuration() { Graphs = mainConfig.Graphs }; //deltaMainConfig.Graphs = mainConfig.Graphs;            
            var mainData = mainConfig.Bill;

            // выберем все конфигурации кроме основной в отдельный словарь
            Dictionary<string, Configuration> otherConfigs = 
                    aConfigs.Where(cfg => !string.Equals(cfg.Key, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase)).
                    ToDictionary(key => key.Key, value => value.Value.DeepCopy());

            Dictionary<string, Configuration> otherConfigs2 = new Dictionary<string, Configuration>();
            foreach (var config in aConfigs)            
                if (!string.Equals(config.Key, Constants.MAIN_CONFIG_INDEX, StringComparison.InvariantCultureIgnoreCase))                
                    otherConfigs2.Add(config.Key, config.Value.DeepCopy());
                
            void AddComponentToConfigGroup(IDictionary<string, Group> aConfig, string aGroupName, Component aCmp, bool aIncludeInAll = false)
            {
                if (!aConfig.ContainsKey(aGroupName))
                {
                    aConfig.Add(aGroupName, new Group());
                    if(aIncludeInAll)
                        aConfig[aGroupName].Name = aGroupName;
                }
                aConfig[aGroupName].Components.Add(aCmp);
            }

            void AddComponentToConfigSubGroup(IDictionary<string, Group> aConfig, string aGroupName, string aSubGroupName, Component aCmp)
            {
                if (!aConfig.ContainsKey(aGroupName))                
                    aConfig.Add(aGroupName, new Group());
                    
                if (!aConfig[aGroupName].SubGroups.ContainsKey(aSubGroupName))                
                    aConfig[aGroupName].SubGroups.Add(aSubGroupName, new Group());
                
                aConfig[aGroupName].SubGroups[aSubGroupName].Components.Add(aCmp);
            }

            foreach (var group in mainData.OrderBy(key => key.Key))
            {
                foreach (var component in group.Value.Components)
                {
                    bool inAllConfigs = IsComponentInAllConfigs(otherConfigs, component, group.Key);
                    var config = inAllConfigs ? aMainConfig.Bill : deltaMainConfig.Bill;
                    AddComponentToConfigGroup(config, group.Key, component, inAllConfigs);                    
                }

                foreach (var subgroup in group.Value.SubGroups.OrderBy(subkey => subkey.Key))
                {
                    foreach (var component in subgroup.Value.Components)
                    {                        
                        var config = IsComponentInAllConfigs(otherConfigs, component, group.Key, subgroup.Key) ? aMainConfig.Bill : deltaMainConfig.Bill;
                        AddComponentToConfigSubGroup(config, group.Key, subgroup.Key, component);                        
                    }
                }
            }
                        
            var preparedConfigs = otherConfigs;
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
            AddEmptyRow(aTable);
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
                row[Constants.ColumnName] = new FormattedString { Value = configName }; 
                row[Constants.ColumnTextFormat] = new FormattedString { Value = "1" };                
                aTable.Rows.Add(row);
                AddEmptyRow(aTable);
            }            

            var data = aConfig.Bill;

            foreach (var group in data.OrderBy(key => key.Key).Where(key => !string.Equals(key.Key, Constants.GroupOthersB)))
            {
                if (group.Value.Components.Count() > 0 || group.Value.SubGroups.Count() > 0)
                {
                    var mainсomponents = group.Value.Components;
                    string groupName = GetSubgroupNameByCount(group);
                    FillDataTable(aTable, groupName, mainсomponents);

                    foreach (var subgroup in group.Value.SubGroups.OrderBy(key => key.Key))
                    {
                        var сomponents = subgroup.Value.Components;
                        string subGroupName = GetSubgroupNameByCount(subgroup);
                        FillDataTable(aTable, subGroupName, сomponents);
                    }
                }
            }

            // отдельно запишем прогруппу "Прочие"
            if (data.TryGetValue(Constants.GroupOthersB, out var group_other))
            {
                if (group_other.Components.Count > 0)
                {
                    var mainсomponents = group_other.Components.ToList();
                    FillDataTable(aTable, Constants.GroupOthersB, mainсomponents);                    
                }
            }
        }

        /// <summary>
        /// добавить имя группы в таблицу
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        private bool AddGroupName(DataTable aTable, string aGroupName) {
            if (string.IsNullOrEmpty(aGroupName)) 
                return false;

            string[] aGroupNameArr = PdfUtils.SplitStringByWidth(Constants.BillColumn2NameWidth - 2, aGroupName, new char[] { ' ', '.', '-' }, Constants.BillFontSize).ToArray();            
            DataRow row;
            int ln = aGroupNameArr.Length;

            int groupNameRowNumber = aTable.Rows.Count + ln;
            int firstComponentRowNumber = groupNameRowNumber + 2;
            int groupNamePageNumber = CommonUtils.GetCurrentPage(DocType.Bill, groupNameRowNumber);
            int firstComponentPageNumber = CommonUtils.GetCurrentPage(DocType.Bill, firstComponentRowNumber);
            if (firstComponentPageNumber > groupNamePageNumber)
            {
                AddEmptyRowsToEndPage(aTable, DocType.Bill, groupNameRowNumber - ln + 1);                
            }

            for (int i = 0; i < ln; i++)
            {
                row = aTable.NewRow();
                row[Constants.ColumnName] = new FormattedString { Value = aGroupNameArr[i], IsUnderlined = true };
                row[Constants.ColumnTextFormat] = new FormattedString { Value = "1" };
                aTable.Rows.Add(row);
            }            
            return true;
        }


        /// <summary>
        /// заполнить таблицу данных
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        /// <param name="aComponents"></param>
        /// <param name="aOtherComponents"></param>
        /// <param name="aSchemaDesignation"></param>
        private void FillDataTable(DataTable aTable, string aGroupName, IEnumerable<Models.Component> aComponents) {

            if (!aComponents.Any()) return;
            // записываем компоненты в таблицу данных

            // Cортировка компонентов по значению свойства "Наименование"            
            Models.Component[] sortComponents = SortFactory.GetSort(SortType.Name).Sort(aComponents.ToList()).ToArray();

            // записываем наименование группы, если есть
            if (AddGroupName(aTable, aGroupName))            
                AddEmptyRow(aTable);            

            //записываем таблицу данных объединяя подряд идущие компоненты с одинаковым наименованием    
            DataRow row;
            Component prevComponent = null;
            float prevCnt = 0;
            float compCount = 0;
            for (int i = 0; i < sortComponents.Length; i++)
            {
                var component = sortComponents[i];
                
                var name = component.GetProperty(Constants.ComponentName); 
                // объединим совпадающие компоненты
                if (i > 0)
                { 
                    var prevName = prevComponent.GetProperty(Constants.ComponentName);
                    var prevProductCode = prevComponent.GetProperty(Constants.ComponentProductCode);
                    var prevComponentDoc = prevComponent.GetProperty(Constants.ComponentDoc);

                    var productCode = component.GetProperty(Constants.ComponentProductCode);
                    var componentDoc = component.GetProperty(Constants.ComponentDoc);

                    // если текущий компонент совпадает с предыдущим то в строку запишем только количества и Куда входит
                    if (string.Equals(prevName, name) && string.Equals(prevProductCode, productCode) && string.Equals(prevComponentDoc, componentDoc))
                    {
                        row = aTable.NewRow();
                        row[Constants.ColumnEntry] = new FormattedString { Value = component.GetProperty(Constants.ComponentWhereIncluded) };
                        float.TryParse(component.GetProperty(Constants.ComponentCountDev), out float cnt_dev_n);
                        if (cnt_dev_n == 0) cnt_dev_n = component.Count;
                        row[Constants.ColumnQuantityDevice] = cnt_dev_n;
                        float.TryParse(component.GetProperty(Constants.ComponentCountSet), out float cnt_comp_n);
                        row[Constants.ColumnQuantityComplex] = cnt_comp_n;
                        float.TryParse(component.GetProperty(Constants.ComponentCountReg), out float cnt_reg_n);
                        row[Constants.ColumnQuantityRegul] = cnt_reg_n;
                        row[Constants.ColumnQuantityTotal] = new FormattedString { Value = (cnt_dev_n + cnt_comp_n + cnt_reg_n).ToString() };
                        aTable.Rows.Add(row);

                        if (prevCnt > 0)
                        {
                            compCount = prevCnt;
                            prevCnt = 0;
                        }

                        compCount += cnt_dev_n + cnt_comp_n + cnt_reg_n;                        

                        if (i == sortComponents.Length - 1)
                        {
                            row = aTable.NewRow();
                            row[Constants.ColumnQuantityTotal] = new FormattedString { Value = (compCount).ToString(), IsOverlined = true };
                            aTable.Rows.Add(row);
                        }                        
                        continue;
                    }
                    else if (compCount > 0)  // запишем сумму по предыдущим одинаковым компонентам и обнулим так как произошел переход
                    {
                        row = aTable.NewRow();
                        row[Constants.ColumnQuantityTotal] = new FormattedString { Value = (compCount).ToString(), IsOverlined = true };                        
                        aTable.Rows.Add(row);
                        compCount = 0;                                    
                    }
                }

                // если группа Прочие изделия, то к наименованию прибавим имя подгруппы из тега Подраздел СП
                if (string.Equals(aGroupName, Constants.GroupOthersB, StringComparison.InvariantCultureIgnoreCase))
                {
                    var subgroupSp = GetSubgroupName(component.GetProperty(Constants.SubGroupNameSp), true); 
                    name = ($"{subgroupSp} {name}").TrimStart();
                }

                // вчисляем длины полей и переносим на следующую строку при необходимости 
                // разобьем наименование на несколько строк исходя из длины текста
                string[] namearr = PdfUtils.SplitStringByWidth(Constants.BillColumn2NameWidth - 2, name, new char[] { ' ','.','-'} ,Constants.BillFontSize).ToArray();
                var supplier = component.GetProperty(Constants.ComponentSupplier);                 
                string[] supplierarr = PdfUtils.SplitStringByWidth(Constants.BillColumn5SupplierWidth - 2, supplier, new char[] { ' ', '.', '-' }, Constants.BillFontSize).ToArray();
                var note = component.GetProperty(Constants.ComponentNote);
                string[] notearr = PdfUtils.SplitStringByWidth(Constants.BillColumn11FootnoteWidth - 2, note, new char[] { ' ', '.', '-' }, Constants.BillFontSize).ToArray();

                row = aTable.NewRow();
                row[Constants.ColumnName] = new FormattedString { Value = namearr.First() };
                if (CommonUtils.IsRussianSupplier(supplier))
                {
                    row[Constants.ColumnProductCode] = new FormattedString { Value = component.GetProperty(Constants.ComponentProductCode) };
                }
                row[Constants.ColumnDeliveryDocSign] = new FormattedString { Value = component.GetProperty(Constants.ComponentDoc) };
                row[Constants.ColumnSupplier] = new FormattedString { Value = supplierarr.First() };
                row[Constants.ColumnEntry] = new FormattedString { Value = component.GetProperty(Constants.ComponentWhereIncluded) };

                float.TryParse(component.GetProperty(Constants.ComponentCountDev), out float cnt_dev);
                if (cnt_dev == 0) cnt_dev = component.Count;
                row[Constants.ColumnQuantityDevice] = cnt_dev;
                float.TryParse(component.GetProperty(Constants.ComponentCountSet), out float cnt_comp);
                row[Constants.ColumnQuantityComplex] = cnt_comp;
                float.TryParse(component.GetProperty(Constants.ComponentCountReg), out float cnt_reg);
                row[Constants.ColumnQuantityRegul] = cnt_reg;
                prevCnt = cnt_dev + cnt_comp + cnt_reg;
                row[Constants.ColumnQuantityTotal] = new FormattedString { Value = prevCnt.ToString() };
                row[Constants.ColumnFootnote] = new FormattedString { Value = notearr.First() };            
                aTable.Rows.Add(row);

                int max = Math.Max(namearr.Length, notearr.Length);
                max = Math.Max(max, supplierarr.Length);
                if (max > 1)
                {
                    int ln_name = namearr.Length;
                    int ln_supplier = supplierarr.Length;
                    int ln_note = notearr.Length;

                    for (int ln = 1; ln< max; ln++)
                    {
                        row = aTable.NewRow();
                        row[Constants.ColumnName] = new FormattedString { Value = (ln_name > ln) ? namearr[ln] : string.Empty };
                        row[Constants.ColumnSupplier] = new FormattedString { Value = (ln_supplier > ln) ? supplierarr[ln] : string.Empty };
                        row[Constants.ColumnFootnote] = new FormattedString { Value = (ln_note > ln) ? notearr[ln] : string.Empty };
                        aTable.Rows.Add(row);
                    }
                }

                prevComponent = component;
            }

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
        }


        /// <summary>
        /// определение наличия компонента во всех конфигурациях
        /// </summary>
        /// <param name="aComponent">a component.</param>
        /// <param name="aGroupName">Name of a group.</param>
        /// <param name="aOtherConfigs">a other configs.</param>
        /// <returns></returns>
        private bool IsComponentInAllConfigs(Dictionary<string, Configuration> aOtherConfigs, Component aComponent, string aGroupName, string aSubGroupName = "")
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
                    if (config.Value.Bill.ContainsKey(aGroupName))
                    {   
                        foreach (var othercomp in config.Value.Bill[aGroupName].Components)
                        {
                            if(EquealsBillComponents(othercomp, aComponent))
                            {
                                removeList = config.Value.Bill[aGroupName].Components;
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
                                config.Value.Bill.Remove(aGroupName);
                        }
                    }                        
                }
                else
                {
                    if (config.Value.Bill.ContainsKey(aGroupName))
                    {

                        if (config.Value.Bill[aGroupName].SubGroups.ContainsKey(aSubGroupName))
                        {
                            foreach (var othercomp in config.Value.Bill[aGroupName].SubGroups[aSubGroupName].Components)
                            {
                                if (EquealsBillComponents(othercomp, aComponent))
                                {
                                    removeList = config.Value.Bill[aGroupName].SubGroups[aSubGroupName].Components;
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
                                    config.Value.Bill[aGroupName].SubGroups.Remove(aSubGroupName);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Equealses the bill components.
        /// </summary>
        /// <param name="aFirstComponent">a first component.</param>
        /// <param name="aSecondComponent">a second component.</param>
        /// <returns></returns>
        private bool EquealsBillComponents(Component aFirstComponent, Component aSecondComponent)
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

        /// <summary>
        /// Removes the last empty rows.
        /// </summary>
        /// <param name="table">The table.</param>
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
                        string.IsNullOrEmpty(arr[7].ToString()) &&
                        string.IsNullOrEmpty(arr[8].ToString()) &&
                        string.IsNullOrEmpty(arr[9].ToString()) &&
                        string.IsNullOrEmpty(arr[10].ToString()) &&
                        string.IsNullOrEmpty(arr[11].ToString()))
                    {
                        empty_str = true;
                        table.Rows.RemoveAt(last_index);
                        last_index = table.Rows.Count - 1;
                    }
                }
                while (empty_str);
            }
        }

        /// <summary>
        /// добавить в таблицу данных оглавление если листов более 24
        /// </summary>
        /// <param name="aTable">Таблица данных</param>
        /// <param name="aSign">значение тега Обозначение исходной спецификации</param>
        /// <returns></returns>
        private void AddContentToDataTable(DataTable aTable, string aSign)
        {
            // проверим надо ли добавить оглавление
            int countPages = CommonUtils.GetCountPage(DocType.Bill, aTable.Rows.Count);
            int toComponentsOffset = 2;
            if (countPages > Constants.BillPagesWithoutContent)
            {
                // сделаем оглавление
                var content = MakeContent(aTable, aSign);
                int contentRowsCount = content.Count * 2; // учитываем пустые строки между строками оглавления
                int offsetPages = CommonUtils.GetCurrentPage(DocType.Bill, contentRowsCount);
                int contentRowsOnPages = CommonUtils.GetRowsForPages(DocType.Bill, offsetPages);

                // добавим пустые строки под страницы, занимаемые оглавлением, сразу
                for (int i = 0; i < contentRowsOnPages; i++)
                    AddEmptyRow(aTable, i);

                int offsetRows = contentRowsOnPages;
                
                // запишем оглавление в таблицу данных                                
                int contentRowIndex = 0;
                int endRowLastGroupContent = 0;
                int endPageLastGroupContent = 0;
                foreach (var str in content)
                {
                    contentRowIndex++;
                    
                    // проверим что, между текущей и предыдущей группами нет лишних пустых строк
                    int beginGroupRowNumber = str.Item2 + offsetRows;
                    int beginGroupPage = CommonUtils.GetCurrentPage(DocType.Bill, beginGroupRowNumber);
                    if (endRowLastGroupContent > 0 &&
                        beginGroupRowNumber - endRowLastGroupContent > toComponentsOffset && 
                        endPageLastGroupContent == beginGroupPage)
                    {
                        int delta = beginGroupRowNumber - endRowLastGroupContent - toComponentsOffset;
                        for (int i = 0; i < delta; i++)                        
                            aTable.Rows.RemoveAt(endRowLastGroupContent);
                                                
                        offsetRows -= delta;
                        beginGroupRowNumber = str.Item2 + offsetRows;
                    }
                    int endGroupRowNumber = str.Item3 + offsetRows;
                    endRowLastGroupContent = endGroupRowNumber;
                    int endGroupPage = CommonUtils.GetCurrentPage(DocType.Bill, endGroupRowNumber);
                    endPageLastGroupContent = endGroupPage;

                    // оценим нахождение на разных страницах оглавления и 
                    int firstComponentRowNumber = beginGroupRowNumber + toComponentsOffset;
                    
                    int firstComponentPage = CommonUtils.GetCurrentPage(DocType.Bill, firstComponentRowNumber);                    
                    if (beginGroupPage != firstComponentPage)
                    {
                        int addedEmptyRows = AddEmptyRowsToEndPage(aTable, DocType.Bill, beginGroupRowNumber);
                        offsetRows += addedEmptyRows;
                        beginGroupPage = firstComponentPage;
                    }
                    
                    string pagesRange = (beginGroupPage == endGroupPage) ?
                                            $"Лист {beginGroupPage}" : 
                                            $"Листы {beginGroupPage}-{endGroupPage}";
                    var row = aTable.Rows[contentRowIndex];
                    row[Constants.ColumnName] = new FormattedString { Value = str.Item1 };
                    row[Constants.ColumnDeliveryDocSign] = new FormattedString { Value = pagesRange };
                    contentRowIndex++;
                }
            }
        }

        /// <summary>
        /// Создать оглавление.
        /// Агоритм: проходим по строкам документа, зная что заголовок выделяется пустыми строками ниже и выше.
        /// находим заголовок и запоминаем номер строки - это начало группы
        /// ищем вторую пустую строку при непустом заголовке и берем номер строки - 1 = это последняя строка с данными для данный группы
        /// если встречаем строку с переменными данными то ...
        /// </summary>
        /// <param name="aTable">таблица данных</param>
        /// <param name="aSign">значение тега Обозначение исходной спецификации</param>
        /// <returns>список для созданий оглавления типа List<Tuple<Наименование группы, номер строки начала группы, номер строки конца группы></returns>
        private List<Tuple<string,int,int>> MakeContent(DataTable aTable, string aSign)
        {
            List<Tuple<string, int, int>> content = new List<Tuple<string, int, int>>();

            bool firstEmptyRow = false;            
            string groupName = string.Empty;
            int beginRowNumber = 1;            

            int rowsCount = aTable.Rows.Count;            
            for (int cnt = 0; cnt < rowsCount; cnt++)
            {
                if (IsEmptyRow(aTable.Rows[cnt]) && !string.IsNullOrEmpty(groupName))
                {
                    // если первая пустая строка уже была
                    if (firstEmptyRow )
                    {
                        int endRowNumber = cnt; // последняя строка группы - это последняя непустая строка
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            content.Add(new Tuple<string, int, int>(groupName, beginRowNumber, endRowNumber));
                            groupName = string.Empty;
                            beginRowNumber = 0;
                            firstEmptyRow = false;
                        }
                    } else
                    {                        
                        firstEmptyRow = true;
                    }
                }
                else if (IsGroupName(aTable.Rows[cnt]))
                {
                    beginRowNumber = cnt + 1;
                    groupName = GetValueFormFormattedString(Constants.ColumnName, aTable.Rows[cnt]);
                }
                else if (IsVariableConfigData(aTable.Rows[cnt]))
                {
                    content.Add(new Tuple<string, int, int>($"{Constants.VariableConfigDataSig1} {Constants.VariableConfigDataSig2}", cnt, aTable.Rows.Count));
                } 
                else if (IsVariableConfiguration(aTable.Rows[cnt], aSign))
                {
                    throw new NotImplementedException("оглавление для переменных данных не реализовано");
                    // TODO: доделать оглавление при учете переменных данных исполнения
                }
            }
            
            content.Add(new Tuple<string, int, int>(groupName, beginRowNumber, rowsCount));

            return content;
        }

        /// <summary>
        /// проверка на пустую строку
        /// </summary>
        /// <param name="aRow">a row.</param>
        /// <returns>
        ///   <c>true</c> if [is empty row] [the specified a row]; otherwise, <c>false</c>.
        /// </returns>
        bool IsEmptyRow(DataRow aRow)
        {
            if (string.IsNullOrEmpty(aRow[Constants.ColumnName].ToString()) &&
                string.IsNullOrEmpty(aRow[Constants.ColumnSupplier].ToString()) &&
                string.IsNullOrEmpty(aRow[Constants.ColumnFootnote].ToString()) &&
                string.IsNullOrEmpty(aRow[Constants.ColumnDeliveryDocSign].ToString()) &&
                string.IsNullOrEmpty(aRow[Constants.ColumnQuantityTotal].ToString()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// проверка строки на заголовок
        /// </summary>
        /// <param name="aRow">a row.</param>
        /// <returns>
        ///   <c>true</c> if [is empty row] [the specified a row]; otherwise, <c>false</c>.
        /// </returns>
        bool IsGroupName(DataRow aRow)
        {
            if (!string.IsNullOrEmpty(aRow[Constants.ColumnTextFormat].ToString()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// проверка что строка содержит заголовок переменных данных исполнений
        /// </summary>
        /// <param name="aRow">строка таблицы данных</param>
        /// <returns>
        ///  <c>true</c> если строка содержит заголовок переменных данных исполнений, иначе <c>false</c>.
        /// </returns>
        bool IsVariableConfigData(DataRow aRow)
        {
            var name = GetValueFormFormattedString(Constants.ColumnName, aRow);
            var productCode = GetValueFormFormattedString(Constants.ColumnProductCode, aRow);            

            if (string.Equals(name, Constants.VariableConfigDataSig1) &&
                string.Equals(productCode, Constants.VariableConfigDataSig1))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// проверка строки с наименованием конфигурации для переменных данных исполнений
        /// </summary>
        /// <param name="aRow">строка таблицы данных</param>
        /// <param name="aSign">обозначение главное спецификации</param>
        /// <returns>
        ///  <c>true</c> если строка содержит наименование конфигурации, иначе <c>false</c>.
        /// </returns>
        bool IsVariableConfiguration(DataRow aRow, string aSign)
        {
            var name = GetValueFormFormattedString(Constants.ColumnName, aRow);
            if (!string.IsNullOrEmpty(aSign) && name.Contains(aSign))
            {
                return true;
            }
            return false;
        }
    }
}

