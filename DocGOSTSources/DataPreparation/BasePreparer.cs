using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;
using iText.Layout.Properties;

namespace GostDOC.DataPreparation {
/// <summary>
/// базовый класс для всех классов подготовки данных перед экспортом
/// </summary>
public abstract class BasePreparer {

    /// <summary>
    /// минимальное количество компонентов, свыше которого необходимо объединить компоненты при одинаковом стандартном документе (для ГОСТ и ТУ)
    /// </summary>
    protected const int MIN_ITEMS_FOR_COMBINE_BY_STANDARD = 3;

    /// <summary>
    /// обозначение документа
    /// </summary>
    protected Dictionary<string, object> appliedParams = new Dictionary<string, object>();

    /// <summary>
    /// получить обозначение документа
    /// </summary>
    public Dictionary<string, object> GetAppliedParams()
    {
        return appliedParams;
    }

    /// <summary>
    /// формирование таблицы данных
    /// </summary>
    /// <param name="aConfigs">словарь конфигураций типа IDictionary<string ConfigName, Configuration Config>, где ConfigName - имя конфигурации, Config - конфигурация</param>
    /// <returns></returns>
    public abstract DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs);

    /// <summary>
    /// Получить обозначение документа
    /// </summary>
    /// <param name="aMainConfig">основная конфигурация (-00)</param>
    /// <returns></returns>
    public abstract string GetDocSign(Configuration aMainConfig);

    /// <summary>
    /// создать таблицу данных
    /// </summary>
    /// <param name="aDataTableName">имя таблицы данных</param>
    /// <returns></returns>
    protected abstract DataTable CreateTable(string aDataTableName);

    /// <summary>
    /// получить строку обозначения из документа "Схема"
    /// </summary>
    /// <param name="aConfig"></param>
    /// <returns></returns>
    protected static string GetSchemaDesignation(Configuration aConfig, out string outDocCode) 
    {
        string designation = string.Empty;        
        outDocCode = "ПЭ3";
        if (aConfig.Specification.TryGetValue(Constants.GroupDoc, out var docs)) {
            if (docs.Components.Count() > 0 || docs.SubGroups.Count() > 0) 
            {
                var docсomponents = docs.Components.Where(val => val.GetProperty(Constants.ComponentName).ToLower().Contains(Constants.DOC_SCHEMA.ToLower()));                
                // в любом случае берем первую
                if (docсomponents.Count() > 0) {
                    designation = docсomponents.First().GetProperty(Constants.ComponentSign);
                    outDocCode = "П"+ docсomponents.First().GetProperty(Constants.ComponentDocCode);
                }
                else {
                    designation = "ПАКБ.ХХХХХХ.ХХХЭ3";
                }
            }
            else {
                // log: в исходном xml файле документов не найдено (раздел Документация пуст)
            }
        }
        return designation;
    }


    /// <summary>
    /// создание списка словарей всех компонентов из прочих элементов с установленным значением "Позицинное обозначение" для всех конфигураций кроме базовой
    /// </summary>
    /// <param name="aConfigs">список всез конфигураций</param>
    /// <returns>список словарей элементов</returns>
    protected IEnumerable<Dictionary<string, Component>>
    MakeComponentDesignatorsDictionaryOtherConfigs(IDictionary<string, Configuration> aConfigs)
    {
        var result = new List<Dictionary<string, Component>>();
        void AddSelectedComponents(Group aGroup, Dictionary<string, Component> aDic)
        {
            if (aGroup.Components.Count() > 0 || aGroup.SubGroups.Count() > 0)
            {
                // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                var mainсomponents = aGroup.Components.Where(val =>
                    !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));

                foreach (var comp in mainсomponents)
                { 
                    var desigantor = comp.GetProperty(Constants.ComponentDesignatorID);
                    if (!aDic.ContainsKey(desigantor))
                        aDic.Add(desigantor, comp);
                }

                foreach (var subgroup in aGroup.SubGroups.OrderBy(key => key.Key))
                {
                    // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                    var сomponents = subgroup.Value.Components.Where(val =>
                        !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));
                    foreach (var comp in сomponents)                    
                    {
                        //aDic.Add(comp.GetProperty(Constants.ComponentDesignatorID), comp);
                        var desigantor = comp.GetProperty(Constants.ComponentDesignatorID);
                        if (!aDic.ContainsKey(desigantor))
                            aDic.Add(desigantor, comp);
                    }   
                }

                result.Add(aDic);
            }
        }

        // ваыбираем все конфигурации кроме базовой
        var configs = aConfigs.Where(val => !string.Equals(val.Key, Constants.MAIN_CONFIG_INDEX));
        foreach (var config in configs) 
        {
            Dictionary<string, Component> dic = new Dictionary<string, Component>();                        
            if (config.Value.Specification.TryGetValue(Constants.GroupOthers, out var others))
            {
                AddSelectedComponents(others, dic);
                if (config.Value.Specification.TryGetValue(Constants.GroupAssemblyUnits, out var units))                
                    AddSelectedComponents(units, dic);                
            }
        }

        return result;
    }


    protected void AddColumn(DataTable aTable, string aColumnName, string aCaption, Type aType) {
        var c = new DataColumn(aColumnName, aType) {
            Unique = false, Caption = aCaption, AllowDBNull = true
        };
        aTable.Columns.Add(c);
    }

    /// <summary>
    /// получить имя группы  или подгруппы из ключа вида "Единственное число\\Множественное число"
    /// с учетом кол-ва элементов
    /// </summary>
    /// <param name="aSubgroup">a subgroup.</param>
    /// <returns></returns>
    protected string GetSubgroupNameByCount(KeyValuePair<string, Group> aSubgroup)
    {        
        var subgroupNamesArr = aSubgroup.Key.Split(new char[] { '\\', '/' });
        return (subgroupNamesArr.Length == 2 && aSubgroup.Value.Components.Count > 1) ? subgroupNamesArr[1] : subgroupNamesArr[0];
    }

    /// <summary>
    /// получить имя группы или подгруппы из строки вида "Единственное число\\Множественное число"    
    /// </summary>
    /// <param name="aSubgroupName">строка с именем группы или подгруппы</param>
    /// <param name="aFirst"><c>true</c> - выдать идинственное число, иначе множественное</param>
    /// <returns>возвращеет единственное или множественное число название группы/подгруппы</returns>
    protected string GetSubgroupName(string aSubgroupName, bool aFirst)
    {        
        var subgroupNamesArr = aSubgroupName.Split(new char[] { '\\', '/' });   
        string subroupName = aSubgroupName;
        if (subgroupNamesArr.Length == 1)
        { 
            subroupName = subgroupNamesArr[0];
        }
        else if(subgroupNamesArr.Length == 2)
        {
            subroupName = aFirst ? subgroupNamesArr[0] : subgroupNamesArr[1];
        }

        return subroupName;
    }

    /// <summary>
    /// получить имя компонента для столбца "Наименование"
    /// </summary>
    /// <param name="aHasStandardDoc">признак наличия ГОСТ/ТУ символов в документе на поставку</param>
    /// <param name="component">компонент</param>
    /// <returns></returns>
    protected string GetComponentName(string aKey, bool aWithStandardDoc, Dictionary<string, int> aHasStandardDoc, Models.Component component)
    {        
        if (aHasStandardDoc.ContainsKey(aKey) && (aHasStandardDoc[aKey] == 1 || !aWithStandardDoc))        
        {
            return $"{component.GetProperty(Constants.ComponentName)} {component.GetProperty(Constants.ComponentDoc)}";
        }
        if (string.Equals(component.GetProperty(Constants.GroupNameSp), Constants.GroupAssemblyUnits))
        {
            return $"{component.GetProperty(Constants.ComponentName)} {component.GetProperty(Constants.ComponentSign)}";
        }        
        return component.GetProperty(Constants.ComponentName);
    }

    /// <summary>
    /// определение необходимости заменять имя копонента на основе набора правил
    /// </summary>
    /// <param name="aDesignator">позиционное обозначение</param>
    /// <param name="aComponent">компонент</param>
    /// <param name="aOtherInstances">список словарей всех компонентов в других исполнениях</param>
    /// <returns>true - имя компонента необходимо заменить</returns>
    protected bool DifferNameInOtherConfigs(string aDesignator, Component aComponent, IEnumerable<Dictionary<string, Component>> aOtherConfigurations) 
    {
        // найдем в других исполнениях компонент с таким же позиционным обозначением
        List<Component> same_components = GetSameComponentsFromOtherConfigs(aDesignator, aOtherConfigurations);

        string component_name = aComponent.GetProperty(Constants.ComponentName);
        string component_sign = aComponent.GetProperty(Constants.ComponentSign);

        // если в других исполнениях исходный компонент отличается по наименованию либо не представлен, то выведем true         
        foreach (var comp in same_components) {
            string other_name = comp.GetProperty(Constants.ComponentName);
            string other_sign = comp.GetProperty(Constants.ComponentSign);

            if (!string.Equals(component_name, other_name) ||
                !string.Equals(component_sign, other_sign)) {
                return true;
            }
        }

        return false;
    }

    
    /// <summary>
    /// признак что компонент не применяется в остальных конфигурациях
    /// </summary>
    /// <param name="aComponent">компонент</param>
    /// <param name="aOtherInstances">список словарей всех компонентов в других исполнениях</param>
    /// <returns>true - компонент не применяется в других испонениях</returns>
    protected bool DisabledInOtherConfigs(string aDesignator, IEnumerable<Dictionary<string, Component>> aOtherConfigurations) 
    {        
        // найдем в других исполнениях компонент с таким же позиционным обозначением
        List<Component> same_components = GetSameComponentsFromOtherConfigs(aDesignator, aOtherConfigurations);
                
        // если компонент задействован хотя бы в одном исполнении, то выводим false
        foreach (var comp in same_components) {            
            if (!IsComponentDisabled(comp)) {
                return false;
            }
        }

        return true;
    }

        /// <summary>
        /// используется ли компонент
        /// </summary>
        /// <param name="aComponent">компонент</param>        
        /// <returns>true - компонент не используется</returns>
        protected bool IsComponentDisabled(Component aComponent)
        {
            return string.Equals(aComponent.GetProperty(Constants.ComponentPresence), "0");
        }

        /// <summary>
        /// получить список с компонентами из других (не основной) исполнений, нимеющих тоже самое позиционное обозначение
        /// </summary>
        /// <param name="aDesignator">позиционное обозначение</param>
        /// <param name="aOtherConfigurations">список с неосновными исполнениями</param>
        /// <returns></returns>
        private List<Component> GetSameComponentsFromOtherConfigs(string aDesignator, IEnumerable<Dictionary<string, Component>> aOtherConfigurations)
    {
        List<Component> same_components = new List<Component>();
        foreach (var instance in aOtherConfigurations) {
            if (instance.ContainsKey(aDesignator)) 
                same_components.Add(instance[aDesignator]);            
        }
        return same_components;
    }

    /// <summary>
    /// поиск компонент с наличием ТУ/ГОСТ в свойстве "Документ на поставку", заполнение словаря с индексами найденных компонент для
    /// значения "Документ на поставку" и сохранение номера компонентов с совпадающим значением                
    /// </summary>
    /// <param name="aComponents">отсортированный массив компонентов</param>
    /// <param name="aHasStandardDoc">массив компонентов с отметками о наличии стандартных документов и объединения в группы:
    /// 1 - компонент имеет документ на поставку по ГОСТ или ТУ, но он один на весь документ, 
    /// 2 - компонент имеет документ на поставку по ГОСТ или ТУ и их достаточно чтобы объединить в группу</param>
    /// <returns></returns>
    protected Dictionary<string /*группа*/, Dictionary<string /*документ на поставку по ГОСТ ил ТУ*/, List<string>> /* список поз. обозначений компонентов, относящися к данному документу */> 
    FindComponentsWithStandardDoc(Dictionary<string, Tuple<string, Component, uint>> aComponentsDic, out Dictionary<string, int> aHasStandardDoc) 
    {
        Dictionary<string, Dictionary<string, List<string>>> StandardDic = new Dictionary<string, Dictionary<string, List<string>>>();
        aHasStandardDoc = new Dictionary<string, int>();

        foreach (var component in aComponentsDic)
        {
            string docToSupply = component.Value.Item2.GetProperty(Constants.ComponentDoc);            
            if (string.IsNullOrEmpty(docToSupply)) 
                continue;
            string groupName = component.Value.Item2.GetProperty(Constants.SubGroupNameSp);
            
            if (docToSupply.StartsWith("гост", StringComparison.InvariantCultureIgnoreCase) ||
                docToSupply.EndsWith("ту", StringComparison.InvariantCultureIgnoreCase)) 
            {
                    Dictionary<string, List<string>> groupStandards;
                    if (StandardDic.TryGetValue(groupName, out groupStandards))
                    {
                        List<string> list; // список позиционных обозначений для объединения элементов по ГОСТ/ТУ
                        if (groupStandards.TryGetValue(docToSupply, out list))
                        {
                            if (list.Count < MIN_ITEMS_FOR_COMBINE_BY_STANDARD)
                            {
                                aHasStandardDoc[component.Key] = 1;
                            } else if (list.Count > MIN_ITEMS_FOR_COMBINE_BY_STANDARD)
                            {
                                aHasStandardDoc[component.Key] = 2;
                            } else
                            {
                                aHasStandardDoc[list[0]] = 2;
                                aHasStandardDoc[list[1]] = 2;
                                aHasStandardDoc[list[2]] = 2;
                                aHasStandardDoc[component.Key] = 2;
                            }

                            list.Add(component.Key);
                        } else
                        {
                            list = new List<string> { component.Key };
                            aHasStandardDoc[component.Key] = 1;
                            groupStandards.Add(docToSupply, list);
                        }
                    }
                    else
                    {   
                        groupStandards = new Dictionary<string, List<string>>{ { docToSupply, new List<string> { component.Key } } };
                        aHasStandardDoc[component.Key] = 1;                        
                        StandardDic.Add(groupName, groupStandards);
                    }
            }
        }

        return StandardDic;
    }

    /// <summary>
    /// добавить пустую строку в таблицу данных (по умолчанию в конец таблицы)
    /// </summary>
    /// <param name="aTable">таблица данных</param>
    /// <param name="aRowIndex">номер позиции, куда надо вставить пустую строку: -1 - надо вставить в конец таблица</param>
    protected void AddEmptyRow(DataTable aTable, int aRowIndex = -1)
    {
        DataRow row = aTable.NewRow();
        if (aRowIndex < 0)
            aTable.Rows.Add(row);
        else
            aTable.Rows.InsertAt(row, aRowIndex);
    }

    /// <summary>
    /// Adds the empty rows to end page.
    /// </summary>
    /// <param name="aTable">a table.</param>
    /// <param name="aType">тип документа</param>
    /// <param name="beginGroupRowNumber">The begin group row number.</param>
    /// <returns></returns>
    protected int AddEmptyRowsToEndPage(DataTable aTable, DocType aType, int aRowNumber)
    {            
        int addedRows = CommonUtils.GetRowsToEndOfPage(aType, aRowNumber);
        for (int i = 0; i < addedRows; i++)
            AddEmptyRow(aTable, aRowNumber - 1 + i);

        return addedRows;
    }

    /// <summary>
    /// Gets the value form formatted string.
    /// </summary>
    /// <param name="columnName">Name of the column.</param>
    /// <param name="aRow">a row.</param>
    /// <returns></returns>
    protected string 
    GetValueFormFormattedString(string columnName, DataRow aRow) =>
                            (aRow[columnName] == DBNull.Value) ? string.Empty : ((BasePreparer.FormattedString)aRow[columnName]).Value;

    /// <summary>
    /// формтированное значение в виде строки
    /// </summary>
    public class FormattedString {

        /// <summary>
        /// строка со значением
        /// </summary>
        public string Value;
        /// <summary>
        /// строка должна быть с нижним подчеркиванием
        /// </summary>
        public bool IsUnderlined;
        /// <summary>
        /// строка должна быть с верхним подчеркиванием
        /// </summary>
        public bool IsOverlined;
        /// <summary>
        /// шрифт должен быть Bold
        /// </summary>
        public bool IsBold;
        /// <summary>
        /// выравнивание строки
        /// </summary>
        public TextAlignment TextAlignment;
    }
}
}