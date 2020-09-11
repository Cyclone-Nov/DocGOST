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

    }
}
