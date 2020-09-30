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
internal class ElementListDataPreparer : BasePreparer {

    /// <summary>
    /// формирование таблицы данных
    /// </summary>
    /// <param name="aConfigs"></param>
    /// <returns></returns>    
    public override DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs) {
        // выбираем основную конфигурацию
        Configuration mainConfig = null;
        if (!aConfigs.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
            return null;
        var data = mainConfig.Specification;
        string schemaDesignation = GetSchemaDesignation(mainConfig);

        // из остальных конфигураций получаем список словарей с соответсвующими компонентами
        var otherConfigsElements = MakeComponentDesignatorsDictionaryOtherConfigs(aConfigs);

        // работаем по основной конфигурации
        // нужны только компоненты из раздела "Прочие изделия"
        Group others;
        if (data.TryGetValue(Constants.GroupOthers, out others)) {
            DataTable table = CreateTable("ElementListData");
            if (others.Components.Count() > 0 || others.SubGroups.Count() > 0) {
                // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                var mainсomponents = others.Components.Where(val =>
                    !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));

                AddEmptyRow(table);
                FillDataTable(table, "", mainсomponents, otherConfigsElements, schemaDesignation);

                foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key)) {
                    // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                    var сomponents = subgroup.Value.Components.Where(val =>
                        !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                    FillDataTable(table, subgroup.Value.Name, сomponents, otherConfigsElements, schemaDesignation);
                }
            }

            return table;
        }

        return null;
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
                IEnumerable<Models.Component> aComponents, 
                IEnumerable<Dictionary<string, Component>> aOtherComponents, string aSchemaDesignation) {

            if (!aComponents.Any()) return;
            // записываем компоненты в таблицу данных

            // Cортировка компонентов по значению свойства "Позиционное обозначение"
            Models.Component[] sortComponents = SortFactory.GetSort(SortType.DesignatorID).Sort(aComponents.ToList()).ToArray();
            // для признаков составления наименования для данного компонента
            int[] HasStandardDoc;

            string change_name = $"см. табл. {aSchemaDesignation}";

            //ищем компоненты с наличием ТУ/ГОСТ в свойстве "Документ на поставку" и запоминаем номера компонентов с совпадающим значением                
            Dictionary<string /* GOST/TY string*/, List<int> /* array indexes */> StandardDic =
                FindComponentsWithStandardDoc(sortComponents, out HasStandardDoc);

            // записываем наименование группы, если есть
            AddGroupName(aTable, aGroupName);

            // записываем строки с гост/ту в начале таблицы, если они есть для нескольких компонентов
            if (!AddStandardDocsToTable(aGroupName, sortComponents, aTable, StandardDic)) {
                AddEmptyRow(aTable);
            }

            //записываем таблицу данных объединяя подряд идущие компоненты с одинаковым наименованием    
            DataRow row;
            for (int i = 0; i < sortComponents.Length;)
            {
                var component = sortComponents[i];
                string component_name = GetComponentName(HasStandardDoc[i] == 2, component);
                int component_count = 1; // always only one! GetComponentCount(component.GetProperty(Constants.ComponentCountDev));
                bool haveToChangeName = string.Equals(component.GetProperty(Constants.ComponentPresence),"0") ||
                                        HaveToChangeComponentName(component, aOtherComponents);                
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

                string component_designator = MakeComponentDesignatorsString(component_designators);

                // вчисляем длины полей и переносим на следующуй строку при необходимости 
                // разобьем наименование на несколько строк исходя из длины текста
                var name = (haveToChangeName) ? change_name : component_name;
                string[] namearr = PdfUtils.SplitStringByWidth(110, name).ToArray();       
                var note = component.GetProperty(Constants.ComponentNote);
                string[] notearr = PdfUtils.SplitStringByWidth(45, note).ToArray();

                row = aTable.NewRow();
                row[Constants.ColumnPosition] = component_designator;
                row[Constants.ColumnName] = namearr.First();
                row[Constants.ColumnQuantity] = component_count;
                row[Constants.ColumnFootnote] = notearr.First();
                aTable.Rows.Add(row);

                int max = Math.Max(namearr.Length, notearr.Length);
                if (max > 1)
                {
                    int ln_name = namearr.Length;
                    int ln_note = notearr.Length;

                    for (int ln = 1; ln< max; ln++)
                    {
                        row = aTable.NewRow();
                        row[Constants.ColumnName] = (ln_name > ln) ? namearr[ln] : string.Empty;
                        row[Constants.ColumnFootnote] = (ln_note > ln) ? notearr[ln] : string.Empty;
                        aTable.Rows.Add(row);
                    }
                }

            }

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
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

            AddColumn(Constants.ColumnPosition, "Поз. обозначение", typeof(string));
            AddColumn(Constants.ColumnName, "Наименование", typeof(string));
            AddColumn(Constants.ColumnQuantity, "Кол.", typeof(Int32));
            AddColumn(Constants.ColumnFootnote, "Примечание", typeof(string));

            return table;
        }


      
              /// <summary>
        /// добавить пустую строку в таблицу данных
        /// </summary>
        /// <param name="aTable"></param>
        private new void AddEmptyRow(DataTable aTable) {
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = string.Empty;
            row[Constants.ColumnPosition] = string.Empty;
            row[Constants.ColumnQuantity] = 0;
            row[Constants.ColumnFootnote] = string.Empty;
            aTable.Rows.Add(row);
        }

        /// <summary>
        /// добавить имя группы в таблицу
        /// </summary>
        /// <param name="aTable"></param>
        /// <param name="aGroupName"></param>
        private void AddGroupName(DataTable aTable, string aGroupName) {
            if (string.IsNullOrEmpty(aGroupName)) return;
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = aGroupName;
            aTable.Rows.Add(row);
        }

        /// <summary>
        /// добавить в таблицу данных стандартные документы на поставку при наличии перед перечнем компонентов
        /// </summary>
        /// <param name="aGroupName">имя группы</param>
        /// <param name="aComponents">список компонентов</param>
        /// <param name="aTable">таблица данных</param>
        /// <param name="aStandardDic">словарь со стандартными документами на поставку</param>
        /// <returns>true - стандартные документы добавлены </returns>
        private bool AddStandardDocsToTable(string aGroupName, Models.Component[] aComponents, DataTable aTable,
            Dictionary<string, List<int>> aStandardDic) {
            bool isApplied = false;
            DataRow row;
            bool applied = false;
            foreach (var item in aStandardDic) {
                if (item.Value.Count() > MIN_ITEMS_FOR_COMBINE_BY_STANDARD) {
                    if (!applied) {
                        applied = true;
                        AddEmptyRow(aTable);
                    }

                    row = aTable.NewRow();
                    var index = item.Value.First();
                    string name = $"{aGroupName} {aComponents[index].GetProperty(Constants.ComponentType)} {item.Key}";
                    row[Constants.ColumnName] = name;
                    aTable.Rows.Add(row);
                    isApplied = true;
                }
            }

            return isApplied;
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
                designator = $"{aDesignators.First()} - {aDesignators.Last()}";

            return designator;
        }
}
}
