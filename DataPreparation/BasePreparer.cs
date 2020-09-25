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
    /// <summary>
    /// базовый класс для всех классов подготовки данных перед экспортом
    /// </summary>
    public abstract class BasePreparer
    {
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
        protected static string GetSchemaDesignation(Configuration aConfig)
        {
            string designation = string.Empty;
            Group docs;
            if (aConfig.Specification.TryGetValue(Constants.GroupDoc, out docs))
            {
                if (docs.Components.Count() > 0 || docs.SubGroups.Count() > 0)
                { 
                    var docсomponents = docs.Components.Where(val => !string.Equals(val.GetProperty(Constants.ComponentName.ToLower()), Constants.DOC_SCHEMA.ToLower()));
                    if(docсomponents.Count() > 0)
                    {
                        // если заканчивается на c3 или э3, то берем ее.                        
                        var shemas = docсomponents.Where(val => (
                                string.Equals(val.GetProperty(Constants.ComponentDocCode), "С3", StringComparison.InvariantCultureIgnoreCase) ||
                                string.Equals(val.GetProperty(Constants.ComponentDocCode), "Э3", StringComparison.InvariantCultureIgnoreCase))
                            );

                        // в любом случа берем первую
                        if(shemas.Count() > 0)
                        {
                            designation = shemas.First().GetProperty(Constants.ComponentSign);
                        }
                        else
                        {
                            designation = docсomponents.First().GetProperty(Constants.ComponentSign);
                        }                       
                    }
                    else
                    {
                        // log: в исходном xml файле документов не найдено (раздел Документация пуст)
                    }
                }
                else
                {
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
            // ваыбираем все конфигурации кроме базовой
            var configs = aConfigs.Where(val => !string.Equals(val.Key, Constants.MAIN_CONFIG_INDEX));

            foreach (var config in configs)
            {
                Dictionary<string, Component> dic = new Dictionary<string, Component>();
                Group others;
                if (config.Value.Specification.TryGetValue(Constants.GroupOthers, out others))
                {
                    if(others.Components.Count() > 0 || others.SubGroups.Count() > 0)
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
                        result.Add(dic);
                    }
                }
            }        
            return result;
        }


        protected void AddStringColumnToTable(DataTable table, string aColumnName, string aCaption) {
            var column = new DataColumn(aColumnName, System.Type.GetType("System.String"));
            column.Unique = false;
            column.Caption = aCaption;
            column.AllowDBNull = true;
            table.Columns.Add(column);
        }

    }
}
