using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GostDOC.Common;
using GostDOC.Models;
using GostDOC.PDF;

namespace GostDOC.DataPreparation
{
    internal class BillDataPreparer : BasePreparer
    {
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

            AddColumn(Constants.ColumnName, "Наименование", typeof(string));
            AddColumn(Constants.ColumnProductCode, "Код продукции", typeof(string));
            AddColumn(Constants.ColumnDeliveryDocSign, "Обозначение документа на поставку", typeof(string));
            AddColumn(Constants.ColumnSupplier, "Поставщик", typeof(string));
            AddColumn(Constants.ColumnEntry, "Куда входит (обозначение)", typeof(string));
            AddColumn(Constants.ColumnQuantityDevice, "Количество на изделие", typeof(Int32));
            AddColumn(Constants.ColumnQuantityComplex, "Количество в комплекты", typeof(Int32));
            AddColumn(Constants.ColumnQuantityRegul, "Количество на регулир.", typeof(Int32));
            AddColumn(Constants.ColumnQuantityTotal, "Количество всего", typeof(Int32));
            AddColumn(Constants.ColumnFootnote, "Примечание", typeof(string));
            AddColumn(Constants.ColumnTextFormat, "Форматирование текста", typeof(string));        

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
            row[Constants.ColumnName] = "Переменные данные";
            row[Constants.ColumnProductCode] = "исполнений";            
            //row[Constants.ColumnDeliveryDocSign] = ;
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
            var mainData = mainConfig.Bill;

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
                        if (!aMainConfig.Bill.ContainsKey(group.Key))
                        {
                            aMainConfig.Bill.Add(group.Key, new Group());
                            aMainConfig.Bill[group.Key].Name = group.Key;
                        }
                        aMainConfig.Bill[group.Key].Components.Add(component);
                    }
                    else
                    {
                        if(!deltaMainConfig.Bill.ContainsKey(group.Key))
                        {   
                            deltaMainConfig.Bill.Add(group.Key, new Group());                            
                        }
                        deltaMainConfig.Bill[group.Key].Components.Add(component);
                        //otherConfigs[Constants.MAIN_CONFIG_INDEX].Bill[group.Key].Components.Add(component);
                    }
                }

                foreach (var subgroup in group.Value.SubGroups.OrderBy(key2 => key2.Key))
                {
                    foreach (var component in subgroup.Value.Components)
                    {
                        if (CheckComponent(otherConfigs, component, group.Key, subgroup.Key))
                        {
                            if (!aMainConfig.Bill.ContainsKey(group.Key))
                            {
                                aMainConfig.Bill.Add(group.Key, new Group());
                            }
                            if (!aMainConfig.Bill[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                aMainConfig.Bill[group.Key].SubGroups.Add(subgroup.Key, new Group());
                            }
                            aMainConfig.Bill[group.Key].SubGroups[subgroup.Key].Components.Add(component);
                        } else
                        {
                            if (!deltaMainConfig.Bill.ContainsKey(group.Key))
                            {
                                deltaMainConfig.Bill.Add(group.Key, new Group());
                            }
                            if (!deltaMainConfig.Bill[group.Key].SubGroups.ContainsKey(subgroup.Key))
                            {
                                deltaMainConfig.Bill[group.Key].SubGroups.Add(subgroup.Key, new Group());
                            }
                            deltaMainConfig.Bill[group.Key].SubGroups[subgroup.Key].Components.Add(component);                            
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
                //row[Constants.ColumnProductCode] = ;
                //row[Constants.ColumnDeliveryDocSign] = ;
                aTable.Rows.Add(row);
                AddEmptyRow(aTable);
            }            

            var data = aConfig.Bill;

            foreach (var group in data.OrderBy(key => key.Key))
            {
                if (group.Value.Components.Count() > 0 || group.Value.SubGroups.Count() > 0)
                {
                    var mainсomponents = group.Value.Components;
                    FillDataTable(aTable, group.Key, mainсomponents);

                    foreach (var subgroup in group.Value.SubGroups.OrderBy(key => key.Key))
                    {
                        var сomponents = subgroup.Value.Components;
                        FillDataTable(aTable, subgroup.Key, сomponents);
                    }
                }
            }
        }

        /// <summary>
        /// добавить пустую строку в таблицу данных
        /// </summary>
        /// <param name="aTable"></param>
        private void AddEmptyRow(DataTable aTable) 
        {
            DataRow row = aTable.NewRow();

            row[Constants.ColumnName] = string.Empty;
            row[Constants.ColumnProductCode] = string.Empty;
            row[Constants.ColumnDeliveryDocSign] = string.Empty;
            row[Constants.ColumnSupplier] = string.Empty;
            row[Constants.ColumnEntry] = string.Empty;
            row[Constants.ColumnQuantityDevice] = 0;
            row[Constants.ColumnQuantityComplex] = 0;
            row[Constants.ColumnQuantityRegul] = 0;
            row[Constants.ColumnQuantityTotal] = 0;
            row[Constants.ColumnFootnote] = string.Empty;
            row[Constants.ColumnTextFormat] = string.Empty;
            
            aTable.Rows.Add(row);
        }

        /// <summary>
        /// добавить имя группы в таблицу
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        private bool AddGroupName(DataTable aTable, string aGroupName) {
            if (string.IsNullOrEmpty(aGroupName)) return false;
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = aGroupName;
            row[Constants.ColumnTextFormat] = "1";
            aTable.Rows.Add(row);
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

            // Cортировка компонентов по значению свойства "Позиционное обозначение"
            Models.Component[] sortComponents = SortFactory.GetSort(SortType.DesignatorID).Sort(aComponents.ToList()).ToArray();            

            // записываем наименование группы, если есть
            if(AddGroupName(aTable, aGroupName))
                AddEmptyRow(aTable);

            //записываем таблицу данных объединяя подряд идущие компоненты с одинаковым наименованием    
            DataRow row;
            Component prevComponent = null;
            uint prevCnt = 0;
            uint compCount = 0;
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

                    // если текущий компонент совпадает с предыдущим то в строку запишем только количества
                    if (string.Equals(prevName, name) && string.Equals(prevProductCode, productCode) && string.Equals(prevComponentDoc, componentDoc))
                    {
                        row = aTable.NewRow();
                        row[Constants.ColumnEntry] = component.GetProperty(Constants.ComponentWhereIncluded);
                        UInt32.TryParse(component.GetProperty(Constants.ComponentCountDev), out uint cnt_dev_n);
                        if (cnt_dev_n == 0) cnt_dev_n = component.Count;
                        row[Constants.ColumnQuantityDevice] = cnt_dev_n;
                        UInt32.TryParse(component.GetProperty(Constants.ComponentCountSet), out uint cnt_comp_n);
                        row[Constants.ColumnQuantityComplex] = cnt_comp_n;
                        UInt32.TryParse(component.GetProperty(Constants.ComponentCountReg), out uint cnt_reg_n);
                        row[Constants.ColumnQuantityRegul] = cnt_reg_n;
                        row[Constants.ColumnQuantityTotal] = cnt_dev_n + cnt_comp_n + cnt_reg_n;
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
                            row[Constants.ColumnQuantityTotal] = compCount;                        
                            aTable.Rows.Add(row);
                        }
                        
                        continue;
                    }
                    else if (compCount > 0)  // запишем сумму по предыдущим одинаковым компонентам и обнулим так как произошел переход
                    {
                        row = aTable.NewRow();
                        row[Constants.ColumnQuantityTotal] = compCount;                        
                        aTable.Rows.Add(row);
                        compCount = 0;
                        continue;
                    }
                }

                // вчисляем длины полей и переносим на следующую строку при необходимости 
                // разобьем наименование на несколько строк исходя из длины текста
                string[] namearr = PdfUtils.SplitStringByWidth(60, name).ToArray();       
                var supplier = component.GetProperty(Constants.ComponentSupplier);                 
                string[] supplierarr = PdfUtils.SplitStringByWidth(55, supplier).ToArray();
                var note = component.GetProperty(Constants.ComponentNote);
                string[] notearr = PdfUtils.SplitStringByWidth(24, note).ToArray();

                row = aTable.NewRow();
                row[Constants.ColumnName] = namearr.First();
                row[Constants.ColumnProductCode] = component.GetProperty(Constants.ComponentProductCode);
                row[Constants.ColumnDeliveryDocSign] = component.GetProperty(Constants.ComponentDoc);
                row[Constants.ColumnSupplier] = supplierarr.First();
                row[Constants.ColumnEntry] = component.GetProperty(Constants.ComponentWhereIncluded);
                
                UInt32.TryParse(component.GetProperty(Constants.ComponentCountDev), out uint cnt_dev);
                if (cnt_dev == 0) cnt_dev = component.Count;
                row[Constants.ColumnQuantityDevice] = cnt_dev;
                UInt32.TryParse(component.GetProperty(Constants.ComponentCountSet), out uint cnt_comp);
                row[Constants.ColumnQuantityComplex] = cnt_comp;
                UInt32.TryParse(component.GetProperty(Constants.ComponentCountReg), out uint cnt_reg);
                row[Constants.ColumnQuantityRegul] = cnt_reg;
                prevCnt = cnt_dev + cnt_comp + cnt_reg;
                row[Constants.ColumnQuantityTotal] = prevCnt;
                row[Constants.ColumnFootnote] = notearr.First();            
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
                        row[Constants.ColumnName] = (ln_name > ln) ? namearr[ln] : string.Empty;
                        row[Constants.ColumnSupplier] = (ln_supplier > ln) ? supplierarr[ln] : string.Empty;
                        row[Constants.ColumnFootnote] = (ln_note > ln) ? notearr[ln] : string.Empty;
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
                        (int)arr[6] == 0 &&
                        (int)arr[7] == 0 &&
                        (int)arr[8] == 0 &&
                        (int)arr[9] == 0 &&
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
    }
}

