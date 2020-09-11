using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.DataPreparation
{
    internal class ElementListDataPreparer : BasePreparer
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
            var data = mainConfig.Specification;
        
            // 
            string schema_desigantion = GetSchemaDesignation(mainConfig);

            // из остальных конфигураций получаем список словарей с соответсвующими компонентами
            var otherConfigsElements = MakeComponentDesignatorsDictionaryOtherConfigs(aConfigs);

            // работаем по основной конфигурации
            // нужны только компоненты из раздела "Прочие изделия"
            Group others;
            if (data.TryGetValue(Constants.GroupOthers, out others))
            {
                DataTable table = CreateTable("ElementListData");
                // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                var mainсomponents = others.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));

                AddEmptyRow(table);
                FillDataTable(table, "", mainсomponents, otherConfigsElements, schema_desigantion);

                foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key))
                {
                    // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                    var сomponents = subgroup.Value.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                    FillDataTable(table, subgroup.Value.Name, сomponents, otherConfigsElements, schema_desigantion);
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
        private void FillDataTable(DataTable aTable, string aGroupName, IEnumerable<Models.Component> aComponents, 
                                   IEnumerable<Dictionary<string, Component>> aOtherComponents, string aSchemaDesignation)
        {
            // записываем компоненты в таблицу данных
            if (aComponents.Count() > 0) 
            {
                //Cортировка компонентов по значению свойства "Позиционное обозначение"
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

                    row = aTable.NewRow();
                    row[Constants.ColumnPosition] = component_designator;
                    row[Constants.ColumnName] = (haveToChangeName) ? change_name : component_name;
                    row[Constants.ColumnQuantity] = component_count;
                    row[Constants.ColumnFootnote] = component.GetProperty(Constants.ComponentNote);
                    aTable.Rows.Add(row);
                }

                AddEmptyRow(aTable);
                aTable.AcceptChanges();
            }
        }


        /// <summary>
        /// получить строку обозначения из документа "Схема"
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns></returns>
        private string GetSchemaDesignation(Configuration aConfig)
        {
            string designation = string.Empty;
            Group docs;
            if (aConfig.Specification.TryGetValue(Constants.GroupDoc, out docs))
            {
                var docсomponents = docs.Components.Where(val => !string.Equals(val.GetProperty(Constants.ComponentName.ToLower()), Constants.DOC_SCHEMA.ToLower()));
                if(docсomponents.Count() > 0)
                {
                   if(docсomponents.Count() > 1)
                   {
                        // TODO: чего делать если несколько схем на документ?
                        throw new Exception("В файле спецификации указано несколько схем для перечня элементов...не смог выбрать (((");
                   }
                   else
                   {
                        designation = docсomponents.First().GetProperty(Constants.ComponentSign);
                   }
                }

            }
            return designation;
        }

        /// <summary>
        /// создание таблицы данных для документа Перечень элементов
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

            column = new DataColumn(Constants.ColumnPosition, System.Type.GetType("System.String"));
            column.Unique = false;
            column.Caption = "Поз. обозначение";
            column.AllowDBNull = true;
            table.Columns.Add(column);

            column = new DataColumn(Constants.ColumnName, System.Type.GetType("System.String"));
            column.Unique = false;
            column.Caption = "Наименование";
            column.AllowDBNull = true;
            table.Columns.Add(column);

            column = new DataColumn(Constants.ColumnQuantity, System.Type.GetType("System.Int32"));
            column.Unique = false;
            column.Caption = "Кол.";
            column.AllowDBNull = true;
            table.Columns.Add(column);

            column = new DataColumn(Constants.ColumnFootnote, System.Type.GetType("System.String"));
            column.Unique = false;
            column.Caption = "Примечание";
            column.AllowDBNull = true;
            table.Columns.Add(column);

            return table;
        }



        /// <summary>
        /// создание списка словарей всех компонентов из прочих элементов с установленным значением "Позицинное обозначение" для всех конфигураций кроме базовой
        /// </summary>
        /// <param name="aConfigs">список всез конфигураций</param>
        /// <returns>список словарей элементов</returns>
        private IEnumerable<Dictionary<string, Component>> 
        MakeComponentDesignatorsDictionaryOtherConfigs(IDictionary<string, Configuration> aConfigs)
        {
            var result = new List<Dictionary<string, Component>>();
            // ваыбираем все конфигурации кроме базовой
            var configs = aConfigs.Where(val => !string.Equals(val.Key, Constants.MAIN_CONFIG_INDEX));

            foreach (var config in configs)
            {
                Dictionary<string, Component> dic = new Dictionary<string, Component>();
                Group others;
                if (config.Value.Specification.TryGetValue(Constants.GroupOthers, out others))
                {                
                    // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                    var mainсomponents = others.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                    foreach(var comp in mainсomponents)
                        dic.Add(comp.GetProperty(Constants.ComponentDesignatiorID), comp);                
             
                    foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key))
                    {
                        // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                        var сomponents = subgroup.Value.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                        foreach(var comp in сomponents)
                            dic.Add(comp.GetProperty(Constants.ComponentDesignatiorID), comp);                    
                    }
                }
                result.Add(dic);
            }        
            return result;
        }

        /// <summary>
        /// поиск компонент с наличием ТУ/ГОСТ в свойстве "Документ на поставку", заполнение словаря с индексами найденных компонент для
        /// значения "Документ на поставку" и сохранение номера компонентов с совпадающим значением                
        /// </summary>
        /// <param name="aComponents">отсортированный массив компонентов</param>
        /// <param name="aHasStandardDoc">массив компонентов с отметками о наличии стандартных документов и объединения в группы:
        /// 0 - компонент не имеет документа на поставку по ТУ или ГОСТ, 
        /// 1 - компонент имеет документ на поставку по ГОСТ ил ТУ, но он один на весь документ, 
        /// 2 - компонент имеет документ на поставку по ГОСТ ил ТУ и их достаточно чтобы объединить в группу</param>
        /// <returns></returns>
        private Dictionary<string /* документ на поставку по ГОСТ ил ТУ*/, List<int> /* список номеров компонентов, относящися к данному документу */> 
        FindComponentsWithStandardDoc(Models.Component[] aComponents, out int[] aHasStandardDoc)
        {
            Dictionary<string, List<int>> StandardDic = new Dictionary<string, List<int>>();
            aHasStandardDoc = new int[aComponents.Length];

            for (int i = 0; i < aComponents.Length; i++) {
                string docToSupply = aComponents[i].GetProperty(Constants.ComponentDoc);
                if (string.IsNullOrEmpty(docToSupply)) continue;
                if (string.Equals(docToSupply.Substring(0, 4).ToLower(), "гост") ||
                    string.Equals(docToSupply.Substring(docToSupply.Length - 2, 2).ToLower(), "ту")) 
                {
                    List<int> list;
                    if (StandardDic.TryGetValue(docToSupply, out list)) 
                    {
                        if (list.Count > 2) 
                        {
                            aHasStandardDoc[list.First()] = 2;
                            aHasStandardDoc[list.First() + 1] = 2;
                            aHasStandardDoc[list.First() + 2] = 2;
                            aHasStandardDoc[i] = 2;
                        }
                        else
                        {   
                            aHasStandardDoc[i] = 1;
                        }
                        list.Add(i);
                    }
                    else {
                        list = new List<int> {i};
                        aHasStandardDoc[i] = 1;
                        StandardDic.Add(docToSupply, list);
                    }
                }
            }

            return StandardDic;
        }

        /// <summary>
        /// определение необходимости заменять имя копонента на основе набора правил
        /// </summary>
        /// <param name="aComponent">компонент</param>
        /// <param name="aOtherPerformances">список словарей всех компонентов в других исполнениях</param>
        /// <returns>true - имя компонента необходимо заменить</returns>
        private bool HaveToChangeComponentName(Component aComponent, IEnumerable<Dictionary<string, Component>> aOtherPerformances)
        {
            string designator = aComponent.GetProperty(Constants.ComponentDesignatiorID);
            // найдем в других исполнениях компонент с таким же позиционным обозначением
            List<Component> same_components = new List<Component>();
            foreach (var performance in aOtherPerformances)
            {            
                if (performance.ContainsKey(designator))
                {
                    same_components.Add(performance[designator]);
                }
            }

            // если в других исполнениях исходный компонент отличается по наименованию, то его наименование надо заменить
            bool haveToChange = false;
            string name = aComponent.GetProperty(Constants.ComponentName);
            foreach (var comp in same_components)
            {
                string other_name = aComponent.GetProperty(Constants.ComponentName);
                bool presence = string.Equals(comp.GetProperty(Constants.ComponentPresence), "1");
                // если в других исполнениях имя компонента отличается либо он не представлен, то необходимо заменить имя
                if (!string.Equals(name, other_name) || !presence)
                {
                   haveToChange = true;
                   break;
                }
            }

            return haveToChange;
        }

        /// <summary>
        /// добавить пустую строку в таблицу данных
        /// </summary>
        /// <param name="aTable"></param>
        private void AddEmptyRow(DataTable aTable) 
        {
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
        private void AddGroupName(DataTable aTable, string aGroupName) 
        {
            if (!string.IsNullOrEmpty(aGroupName)) {
                DataRow row = aTable.NewRow();
                row[Constants.ColumnName] = aGroupName;
                aTable.Rows.Add(row);
            }
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
                                            Dictionary<string, List<int>> aStandardDic) 
        {
            bool isApplied = false;
            DataRow row;
            bool applied = false;
            foreach (var item in aStandardDic) {
                if (item.Value.Count() > 3) {
                    if (!applied)
                    {                    
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
        /// получить имя компонента для столбца "Наименование"
        /// </summary>
        /// <param name="aHasStandardDoc">признак наличия ГОСТ/ТУ символов в документе на поставку</param>
        /// <param name="component">компонент</param>
        /// <returns></returns>
        private string GetComponentName(bool aHasStandardDoc, Models.Component component)
        {
            return (aHasStandardDoc)
                ? $"{component.GetProperty(Constants.ComponentName)} {component.GetProperty(Constants.ComponentDoc)}"
                : component.GetProperty(Constants.ComponentName);
        }

        /// <summary>
        /// получить количество компонентов
        /// </summary>
        /// <param name="aCountStr"></param>
        /// <returns></returns>
        private int GetComponentCount(string aCountStr)
        {
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
        private string MakeComponentDesignatorsString(List<string> aDesignators) 
        {
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
