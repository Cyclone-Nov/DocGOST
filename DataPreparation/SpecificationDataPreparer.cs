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

        class DataToFillTable {
            public DataTable Table;
            public string GroupName;
            public IEnumerable<Models.Component> Components;
            public IEnumerable<Dictionary<string, Component>> OtherComponents;
            public string SchemaDesignation;
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
            if (!aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
                return null;        
            var data = mainConfig.Specification;
            string schemaDesignation = GetSchemaDesignation(mainConfig);

            // из остальных конфигураций получаем список словарей с соответсвующими компонентами
            var otherConfigsElements = MakeComponentDesignatorsDictionaryOtherConfigs(aConfigs);

            DataTable table = CreateTable("SpecificationData");
            
//            if (data.TryGetValue(Constants.GroupDoc, out var documents)) {
//                dtf.GroupName = "";
//                dtf.Components = documents.Components;
//                FillDataTable(dtf);
//            }

            DataToFillTable dtf = new DataToFillTable{Table = table, OtherComponents = otherConfigsElements, SchemaDesignation = schemaDesignation};
            void Fill(string groupName) {
                if (data.TryGetValue(groupName, out var someGroup)) {
                    dtf.Components = someGroup.Components;
                    dtf.GroupName = groupName;
                    FillDataTable(dtf);
                }
            }

            Fill(Constants.GroupDoc);
            Fill(Constants.GroupComplex);
            Fill(Constants.GroupAssemblyUnits);
            Fill(Constants.GroupDetails);
            Fill(Constants.GroupStandard);
            Fill(Constants.GroupOthers);
            Fill(Constants.GroupMaterials);
            Fill(Constants.GroupKits);

            return table;
            //return null;
        }


        /// <summary>
        /// добавить имя группы в таблицу
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        private void AddGroupName(DataTable aTable, string aGroupName) 
        {
            if (string.IsNullOrEmpty(aGroupName)) return;
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = new FormattedString {Value = aGroupName, IsUnderlined = true, TextAlignment = TextAlignment.CENTER};
            aTable.Rows.Add(row);
        }

        void FillDataTable(DataToFillTable dataToFill) {
            var aComponents = dataToFill.Components;
            var aSchemaDesignation = dataToFill.SchemaDesignation;
            var aOtherComponents = dataToFill.OtherComponents;
            if (!aComponents.Any()) return;

            var sortType = SortType.None;

            if (dataToFill.GroupName == Constants.GroupDoc) {
                sortType = SortType.None;
            } else if (dataToFill.GroupName == Constants.GroupComplex || dataToFill.GroupName == Constants.GroupAssemblyUnits || dataToFill.GroupName == Constants.GroupDetails) {
                sortType = SortType.SpComplex;
            } else if (dataToFill.GroupName == Constants.GroupStandard) {
                sortType = SortType.SpStandard;
            } else if (dataToFill.GroupName == Constants.GroupOthers) {
                sortType = SortType.SpOthers;
            } else if (dataToFill.GroupName == Constants.GroupKits) {
                sortType = SortType.SpKits;
            }

            var sort = SortFactory.GetSort(sortType);
            Models.Component[] sortComponents = sort.Sort(aComponents.ToList()).ToArray();
            // для признаков составления наименования для данного компонента
            int[] HasStandardDoc;

            string change_name = $"см. табл. {aSchemaDesignation}";

            // ищем компоненты с наличием ТУ/ГОСТ в свойстве "Документ на поставку" и запоминаем номера компонентов с совпадающим значением                
            Dictionary<string /* GOST/TY string*/, List<int> /* array indexes */> StandardDic = FindComponentsWithStandardDoc(sortComponents, out HasStandardDoc);

            if (dataToFill.GroupName != Constants.GroupDoc) {
                // записываем наименование группы
                AddGroupName(dataToFill.Table, dataToFill.GroupName);
            }
            
            //записываем таблицу данных объединяя подряд идущие компоненты с одинаковым наименованием    
            DataRow row;
            for (int i = 0; i < sortComponents.Length;)
            {
                var component = sortComponents[i];
                string component_name = GetComponentName(HasStandardDoc[i] == 2, component);
                int component_count = 1; // always only one! GetComponentCount(component.GetProperty(Constants.ComponentCountDev));
                bool haveToChangeName = string.Equals(component.GetProperty(Constants.ComponentPresence),"0") || HaveToChangeComponentName(component, aOtherComponents);                
                List<string> component_designators = new List<string>{ component.GetProperty(Constants.ComponentDesignatiorID) };

                bool same;
                int j = i + 1;
                if (j < sortComponents.Length && !haveToChangeName) 
                {
                    do 
                    {
                        var componentNext = sortComponents[j];
                        string componentNext_name = GetComponentName(HasStandardDoc[j] == 2, componentNext);

                        if (string.Equals(component_name, componentNext_name))
                        {
                            same = true;
                            component_count++;
                            j++;
                            component_designators.Add(componentNext.GetProperty(Constants.ComponentDesignatiorID));
                        }
                        else
                            same = false;
                    } while (same && j < sortComponents.Length);
                }

                i = j;


                var name = (haveToChangeName) ? change_name : component_name;
                string[] namearr = PdfUtils.SplitStringByWidth(110, name).ToArray();       
                var note = component.GetProperty(Constants.ComponentNote);
                string[] notearr = PdfUtils.SplitStringByWidth(45, note).ToArray();

                row = dataToFill.Table.NewRow();
                row[Constants.ColumnFormat] = new FormattedString{Value = component.GetProperty(Constants.ComponentFormat)};
                row[Constants.ColumnZone] = new FormattedString{Value = component.GetProperty(Constants.ComponentZone)};
                row[Constants.ColumnPosition] = null;

                string designation = component.GetProperty(Constants.ComponentSign);
                if (dataToFill.GroupName == Constants.GroupDoc) {
                    designation += component.GetProperty(Constants.ComponentDocCode);
                }
                row[Constants.ColumnSign] = new FormattedString{Value = designation};

                row[Constants.ColumnName] = new FormattedString{Value = namearr.First()};
                row[Constants.ColumnQuantity] = component_count;
                row[Constants.ColumnFootnote]= new FormattedString{Value = notearr.First()};
                dataToFill.Table.Rows.Add(row);

                int max = Math.Max(namearr.Length, notearr.Length);
                if (max > 1)
                {
                    int ln_name = namearr.Length;
                    int ln_note = notearr.Length;

                    for (int ln = 1; ln< max; ln++)
                    {
                        row = dataToFill.Table.NewRow();
                        row[Constants.ColumnName] = (ln_name > ln) ? new FormattedString{Value= namearr[ln]} : null;
                        row[Constants.ColumnFootnote] = (ln_note > ln) ? new FormattedString{Value= notearr[ln]} : null;
                        dataToFill.Table.Rows.Add(row);
                    }
                }                
            }

            AddEmptyRow(dataToFill.Table);
            dataToFill.Table.AcceptChanges();

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

        private new void AddEmptyRow(DataTable aTable) 
        {
            DataRow row = aTable.NewRow();
            // TODO
//            row[Constants.ColumnName] = string.Empty;
//            row[Constants.ColumnPosition] = string.Empty;
//            row[Constants.ColumnQuantity] = 0;
//            row[Constants.ColumnFootnote] = string.Empty;
            aTable.Rows.Add(row);
        }

    }
}
