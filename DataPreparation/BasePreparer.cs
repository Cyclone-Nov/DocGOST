using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;

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
    /// формирование таблицы данных
    /// </summary>
    /// <param name="aConfigs">a configs.</param>
    /// <returns></returns>
    public abstract DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs);

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
    protected static string GetSchemaDesignation(Configuration aConfig) {
        string designation = string.Empty;
        Group docs;
        if (aConfig.Specification.TryGetValue(Constants.GroupDoc, out docs)) {
            if (docs.Components.Count() > 0 || docs.SubGroups.Count() > 0) {
                var docсomponents = docs.Components.Where(val =>
                    !string.Equals(val.GetProperty(Constants.ComponentName.ToLower()), Constants.DOC_SCHEMA.ToLower()));
                if (docсomponents.Count() > 0) {
                    // если заканчивается на c3 или э3, то берем ее.                        
                    var shemas = docсomponents.Where(val => (
                        string.Equals(val.GetProperty(Constants.ComponentDocCode), "С3",
                            StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(val.GetProperty(Constants.ComponentDocCode), "Э3",
                            StringComparison.InvariantCultureIgnoreCase))
                    );

                    // в любом случа берем первую
                    if (shemas.Count() > 0) {
                        designation = shemas.First().GetProperty(Constants.ComponentSign);
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
            if (config.Value.Specification.TryGetValue(Constants.GroupOthers, out others)) {
                if (others.Components.Count() > 0 || others.SubGroups.Count() > 0) {
                    // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                    var mainсomponents = others.Components.Where(val =>
                        !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                    foreach (var comp in mainсomponents)
                        dic.Add(comp.GetProperty(Constants.ComponentDesignatiorID), comp);

                    foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key)) {
                        // выбираем только компоненты с заданными значением для свойства "Позиционое обозначение"
                        var сomponents = subgroup.Value.Components.Where(val =>
                            !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                        foreach (var comp in сomponents)
                            dic.Add(comp.GetProperty(Constants.ComponentDesignatiorID), comp);
                    }

                    result.Add(dic);
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
    /// получить имя компонента для столбца "Наименование"
    /// </summary>
    /// <param name="aHasStandardDoc">признак наличия ГОСТ/ТУ символов в документе на поставку</param>
    /// <param name="component">компонент</param>
    /// <returns></returns>
    protected string GetComponentName(bool aHasStandardDoc, Models.Component component) {
        return (aHasStandardDoc)
            ? component.GetProperty(Constants.ComponentName)
            : $"{component.GetProperty(Constants.ComponentName)} {component.GetProperty(Constants.ComponentDoc)}";
    }

    /// <summary>
    /// определение необходимости заменять имя копонента на основе набора правил
    /// </summary>
    /// <param name="aComponent">компонент</param>
    /// <param name="aOtherPerformances">список словарей всех компонентов в других исполнениях</param>
    /// <returns>true - имя компонента необходимо заменить</returns>
    protected bool HaveToChangeComponentName(Component aComponent,
        IEnumerable<Dictionary<string, Component>> aOtherPerformances) {
        string designator = aComponent.GetProperty(Constants.ComponentDesignatiorID);
        // найдем в других исполнениях компонент с таким же позиционным обозначением
        List<Component> same_components = new List<Component>();
        foreach (var performance in aOtherPerformances) {
            if (performance.ContainsKey(designator)) {
                same_components.Add(performance[designator]);
            }
        }

        // если в других исполнениях исходный компонент отличается по наименованию, то его наименование надо заменить
        bool haveToChange = false;
        string name = aComponent.GetProperty(Constants.ComponentName);
        foreach (var comp in same_components) {
            string other_name = aComponent.GetProperty(Constants.ComponentName);
            bool presence = string.Equals(comp.GetProperty(Constants.ComponentPresence), "1");
            // если в других исполнениях имя компонента отличается либо он не представлен, то необходимо заменить имя
            if (!string.Equals(name, other_name) || !presence) {
                haveToChange = true;
                break;
            }
        }

        return haveToChange;
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
        protected Dictionary<string /* документ на поставку по ГОСТ ил ТУ*/, List<int> /* список номеров компонентов, относящися к данному документу */> 
            FindComponentsWithStandardDoc(Models.Component[] aComponents, out int[] aHasStandardDoc) {
            Dictionary<string, List<int>> StandardDic = new Dictionary<string, List<int>>();
            aHasStandardDoc = new int[aComponents.Length];

            for (int i = 0; i < aComponents.Length; i++) {
                string docToSupply = aComponents[i].GetProperty(Constants.ComponentDoc);
                if (string.IsNullOrEmpty(docToSupply)) continue;
                if (string.Equals(docToSupply.Substring(0, 4).ToLower(), "гост") ||
                    string.Equals(docToSupply.Substring(docToSupply.Length - 2, 2).ToLower(), "ту")) {
                    List<int> list;
                    if (StandardDic.TryGetValue(docToSupply, out list)) {
                        if (list.Count < MIN_ITEMS_FOR_COMBINE_BY_STANDARD) {
                            aHasStandardDoc[i] = 1;
                        }
                        else if (list.Count > MIN_ITEMS_FOR_COMBINE_BY_STANDARD) {
                            aHasStandardDoc[i] = 2;
                        }
                        else {
                            aHasStandardDoc[list[0]] = 2;
                            aHasStandardDoc[list[1]] = 2;
                            aHasStandardDoc[list[2]] = 2;
                            aHasStandardDoc[i] = 2;
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


    public struct FormattedString {
        public string Value;
        public bool IsUnderlined;
        public bool IsBold;
    }
}
}