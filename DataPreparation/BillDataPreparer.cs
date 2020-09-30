﻿using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GostDOC.Common;
using GostDOC.Models;

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
            var data = mainConfig.Specification;
            string schemaDesignation = GetSchemaDesignation(mainConfig);

            // из остальных конфигураций получаем список словарей с соответсвующими компонентами
            var otherConfigsElements = MakeComponentDesignatorsDictionaryOtherConfigs(aConfigs);

            // работаем по основной конфигурации
            // нужны только компоненты из раздела "Прочие изделия"
            Group others;
            if (data.TryGetValue(Constants.GroupOthers, out others))
            {
                DataTable table = CreateTable("ElementListData");
                if (others.Components.Count() > 0 || others.SubGroups.Count() > 0)
                {
                    // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                    var mainсomponents = others.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));

                    AddEmptyRow(table);
                    //FillDataTable(table, "", mainсomponents, otherConfigsElements, schemaDesignation);

                    foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key))
                    {
                        // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                        var сomponents = subgroup.Value.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                        //FillDataTable(table, subgroup.Value.Name, сomponents, otherConfigsElements, schemaDesignation);
                    }
                }

                return table;
            }
        
            return null;           
        }

        /// <summary>
        /// создание таблицы данных
        /// </summary>
        /// <param name="aDataTableName"></param>
        /// <returns></returns>
        protected override DataTable CreateTable(string aDataTableName)
        {
            DataTable table = new DataTable(aDataTableName);
            

            return table;
        }


        /// <summary>
        /// добавить пустую строку в таблицу данных
        /// </summary>
        /// <param name="aTable"></param>
        private void AddEmptyRow(DataTable aTable) 
        {
            DataRow row = aTable.NewRow();
            
            aTable.Rows.Add(row);
        }

    }
}
