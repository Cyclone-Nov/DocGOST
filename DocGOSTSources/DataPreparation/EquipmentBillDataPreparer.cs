using System.Data;
using System.Collections.Generic;
using GostDOC.Models;

namespace GostDOC.DataPreparation
{
    internal class EquipmentBillDataPreparer : BasePreparer
    {

        public override string GetDocSign(Configuration aMainConfig)
        {
            return "ВК";
        }

        /// <summary>
        /// формирование таблицы данных
        /// </summary>
        /// <param name="aConfigs"></param>
        /// <returns></returns>    
        public override DataTable CreateDataTable(IDictionary<string, Configuration> aConfigs)
        {            
            return null;
        }

        /// <summary>
        /// создание таблицы данных для документа Перечень элементов
        /// </summary>
        /// <param name="aDataTableName"></param>
        /// <returns></returns>
        protected override DataTable CreateTable(string aDataTableName)
        {
            DataTable table = new DataTable(aDataTableName);    
            return table;
        }
    }
}
