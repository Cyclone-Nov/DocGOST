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
    protected Dictionary<string, string> appliedParams = new Dictionary<string, string>();

    /// <summary>
    /// получить обозначение документа
    /// </summary>
    public Dictionary<string, string> GetAppliedParams()
    {
        return appliedParams;
    }

    /// <summary>
    /// формирование таблицы данных
    /// </summary>
    /// <param name="aConfigs">a configs.</param>
    /// <returns></returns>
    public abstract DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs);

    /// <summary>
    /// Gets the document sign.
    /// </summary>
    /// <param name="aMainConfig">a main configuration.</param>
    /// <returns></returns>
    public abstract string GetDocSign(Configuration aMainConfig);

    /// <summary>
    /// создать таблицу данных
    /// </summary>
    /// <param name="aDataTableName">Name of a data table.</param>
    /// <returns></returns>
    protected abstract DataTable CreateTable(string aDataTableName);

    /// <summary>
    /// получить строку обозначения из документа "Схема"
    /// </summary>
    /// <param name="aConfig"></param>
    /// <returns></returns>
    protected static string GetSchemaDesignation(Configuration aConfig, out string outDocCode) {
        string designation = string.Empty;
        Group docs;
        outDocCode = "ПЭ3";
        if (aConfig.Specification.TryGetValue(Constants.GroupDoc, out docs)) {
            if (docs.Components.Count() > 0 || docs.SubGroups.Count() > 0) {
                var docсomponents = docs.Components.Where(val =>
                    val.GetProperty(Constants.ComponentName).ToLower().Contains(Constants.DOC_SCHEMA.ToLower()));
                if (docсomponents.Count() > 0) {
                    // если заканчивается на c3 или э3, то берем ее.
                    //var shemas = docсomponents.Where(val => (
                    //    val.GetProperty(Constants.ComponentDocCode).EndsWith("С3") ||
                    //    val.GetProperty(Constants.ComponentDocCode).EndsWith("Э3"))
                    //);

                    
                    // в любом случае берем первую
                    if (docсomponents.Count() > 0) {
                        designation = docсomponents.First().GetProperty(Constants.ComponentSign);
                        outDocCode = "П"+ docсomponents.First().GetProperty(Constants.ComponentDocCode);
                    }
                    else {
                        designation = docсomponents.First().GetProperty(Constants.ComponentSign);
                    }
                }
                else {
                    // log: в исходном xml файле документов не найдено (раздел Документация пуст)
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
    MakeComponentDesignatorsDictionaryOtherConfigs(IDictionary<string, Configuration> aConfigs) {
        var result = new List<Dictionary<string, Component>>();
        // ваыбираем все конфигурации кроме базовой
        var configs = aConfigs.Where(val => !string.Equals(val.Key, Constants.MAIN_CONFIG_INDEX));

        foreach (var config in configs) {
            Dictionary<string, Component> dic = new Dictionary<string, Component>();
            Group others;
            Group units;
            if (config.Value.Specification.TryGetValue(Constants.GroupOthers, out others)) {
                if (others.Components.Count() > 0 || others.SubGroups.Count() > 0) {
                    // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                    var mainсomponents = others.Components.Where(val =>
                        !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));
                    foreach (var comp in mainсomponents)
                        dic.Add(comp.GetProperty(Constants.ComponentDesignatorID), comp);

                    foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key)) {
                        // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                        var сomponents = subgroup.Value.Components.Where(val =>
                            !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));
                        foreach (var comp in сomponents)
                            dic.Add(comp.GetProperty(Constants.ComponentDesignatorID), comp);
                    }

                    result.Add(dic);
                }

                if (config.Value.Specification.TryGetValue(Constants.GroupAssemblyUnits, out units))
                {
                    if (units.Components.Count() > 0 || units.SubGroups.Count() > 0)
                    {
                        // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                        var mainсomponents = units.Components.Where(val =>
                            !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));
                        foreach (var comp in mainсomponents)
                            dic.Add(comp.GetProperty(Constants.ComponentDesignatorID), comp);

                        foreach (var subgroup in units.SubGroups.OrderBy(key => key.Key))
                        {
                            // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                            var сomponents = subgroup.Value.Components.Where(val =>
                                !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatorID)));
                            foreach (var comp in сomponents)
                                dic.Add(comp.GetProperty(Constants.ComponentDesignatorID), comp);
                        }

                        result.Add(dic);
                    }
                }
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
        var subgroupNamesArr = aSubgroup.Key.Split(new char[] { '\\' });
        string subGroupName = string.Empty;
        if (subgroupNamesArr.Length == 2 && aSubgroup.Value.Components.Count > 1)
            subGroupName = subgroupNamesArr[1];
        else
            subGroupName = subgroupNamesArr[0];

        return subGroupName;
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
    /// <param name="aComponent">компонент</param>
    /// <param name="aOtherInstances">список словарей всех компонентов в других исполнениях</param>
    /// <returns>true - имя компонента необходимо заменить</returns>
    protected bool DifferNameInOtherConfigs(Component aComponent,
                                             IEnumerable<Dictionary<string, Component>> aOtherConfigurations) 
    {
        string designator = aComponent.GetProperty(Constants.ComponentDesignatorID);
        // найдем в других исполнениях компонент с таким же позиционным обозначением
        List<Component> same_components = new List<Component>();
        foreach (var instance in aOtherConfigurations) {
            if (instance.ContainsKey(designator)) 
                same_components.Add(instance[designator]);            
        }

        // если в других исполнениях исходный компонент отличается по наименованию либо не представлен, то выведем true
        string name = aComponent.GetProperty(Constants.ComponentName);
        foreach (var comp in same_components) {
            string other_name = aComponent.GetProperty(Constants.ComponentName);
            bool presence = string.Equals(comp.GetProperty(Constants.ComponentPresence), "1");            
            if (!string.Equals(name, other_name) || !presence) {
                return true;
            }
        }

        return false;
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


    public class FormattedString {

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