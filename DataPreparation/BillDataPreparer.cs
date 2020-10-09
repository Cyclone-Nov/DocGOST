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
            
             // выбираем основную конфигурацию
            Configuration mainConfig = null;
            if (!aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
                return null;        
            
            var data = mainConfig.Bill;            

            DataTable table = CreateTable("BillData");
            foreach (var group in data.OrderBy(key => key.Key))
            {
                if (group.Value.Components.Count() > 0 || group.Value.SubGroups.Count() > 0)
                {
                    // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                    var mainсomponents = group.Value.Components;

                    //AddEmptyRow(table);
                    FillDataTable(table, group.Key, mainсomponents);

                    foreach (var subgroup in group.Value.SubGroups.OrderBy(key => key.Key))
                    {
                        // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                        var сomponents = subgroup.Value.Components;
                        FillDataTable(table, subgroup.Value.Name, сomponents);
                    }
                }
            }

            // для каждой следующей конфигурации надо получить только те компоненты, которы не встречаются в первой 
            //var data_deltas_list = GetDataDelta(aConfigs);

            // затем по остальным



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
        private void FillDataTable(
                DataTable aTable, 
                string aGroupName, 
                IEnumerable<Models.Component> aComponents) {

            if (!aComponents.Any()) return;
            // записываем компоненты в таблицу данных

            // Cортировка компонентов по значению свойства "Позиционное обозначение"
            Models.Component[] sortComponents = SortFactory.GetSort(SortType.DesignatorID).Sort(aComponents.ToList()).ToArray();            

            // записываем наименование группы, если есть
            if(AddGroupName(aTable, aGroupName))
                AddEmptyRow(aTable);

            //записываем таблицу данных объединяя подряд идущие компоненты с одинаковым наименованием    
            DataRow row;
            for (int i = 0; i < sortComponents.Length; i++)
            {
                var component = sortComponents[i];
                               
                // вчисляем длины полей и переносим на следующуй строку при необходимости 
                // разобьем наименование на несколько строк исходя из длины текста
                var name = component.GetProperty(Constants.ComponentName); 
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
                row[Constants.ColumnQuantityTotal] = cnt_dev + cnt_comp + cnt_reg;
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
            }

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
        }

    }
}

