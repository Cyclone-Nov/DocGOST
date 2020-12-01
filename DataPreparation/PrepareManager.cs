using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

using GostDOC.Common;
using GostDOC.DataPreparation;

namespace GostDOC.Models
{

    /// <summary>
    /// менеджер для подготовки данных перед экспоротом
    /// </summary>
    public class PrepareManager
    {

        #region Singleton
        private static readonly Lazy<PrepareManager> _instance = new Lazy<PrepareManager>(() => new PrepareManager(), true);
        public static PrepareManager Instance => _instance.Value;
        #endregion

        private BasePreparer ElementListDataPreparer = null;
        /// <summary>
        /// таблица данных для перечня элементов
        /// </summary>
        private DataTable ElementListDataTable;

        private BasePreparer BillDataPreparer = null;
        /// <summary>
        /// таблица данных для ведомости покупных изделий
        /// </summary>
        private DataTable BillDataTable;

        private BasePreparer SpecificationDataPreparer = null;
        /// <summary>
        /// таблица данных для спецификации
        /// </summary>
        private DataTable SpecificationDataTable;

        private BasePreparer EquipmentBillDataPreparer = null;
        /// <summary>
        /// таблица данных для ведомости комплектации
        /// </summary>
        private DataTable EquipmentBillDataTable;

        PrepareManager()
        {       
            ElementListDataPreparer = new ElementListDataPreparer();
            BillDataPreparer = new BillDataPreparer();
            SpecificationDataPreparer = new SpecificationDataPreparer();
            EquipmentBillDataPreparer = new EquipmentBillDataPreparer();
        }
        

        /// <summary>
        /// подготовить таблицу данных
        /// </summary>
        /// <param name="aType">тип документа для которого необходимо сформировать таблицу данных</param>
        /// <param name="aConfigs">исходные данные</param>        
        /// <returns>true - подготовка данных прошла успешно</returns>
        public bool PrepareDataTable(DocType aType, IDictionary<string, Configuration> aConfigs)
        {            
            if (aConfigs.Count() == 0)
                return false;

            switch (aType)
            {
                case DocType.Bill:
                    BillDataTable = BillDataPreparer.CreateDataTable(aConfigs);
                    break;

                case DocType.D27:
                    EquipmentBillDataTable = EquipmentBillDataPreparer.CreateDataTable(aConfigs);
                    break;

                case DocType.ItemsList:
                    ElementListDataTable = ElementListDataPreparer.CreateDataTable(aConfigs);
                    break;

                case DocType.Specification:
                    SpecificationDataTable = SpecificationDataPreparer.CreateDataTable(aConfigs);
                    break;

                case DocType.None:
                default:
                    throw new Exception($"Алгоритм подготовки данных для типа документа {aType} не определен. Обратитесь к разработчикам ПО");
            }

            return true;
        }

        /// <summary>
        /// Получить сформированную ранее таблицу данных для выбранного типа документа
        /// </summary>
        /// <param name="aType">тип документа</param>
        /// <returns>таблица данных для указанного типа документа</returns>
        /// <exception cref="Exception">Таблица данных для типа документа {aType}</exception>
        public DataTable GetDataTable(DocType aType)
        {
            switch(aType)
            {
                case DocType.Bill:
                    return BillDataTable;

                case DocType.Specification:
                    return SpecificationDataTable;

                case DocType.ItemsList:
                    return ElementListDataTable;

                case DocType.D27:
                    return EquipmentBillDataTable;

                default:
                    throw new Exception($"Таблица данных для типа документа {aType} не определена");

            }
        }

        /// <summary>
        /// Получить сформированную ранее таблицу данных для выбранного типа документа
        /// </summary>
        /// <param name="aType">тип документа</param>
        /// <returns>таблица данных для указанного типа документа</returns>
        /// <exception cref="Exception">Таблица данных для типа документа {aType}</exception>
        public Dictionary<string, string> GetAppliedParams(DocType aType)
        {
            switch (aType)
            {
                case DocType.Bill:
                    return BillDataPreparer.GetAppliedParams();

                case DocType.Specification:
                    return SpecificationDataPreparer.GetAppliedParams();

                case DocType.ItemsList:
                    return ElementListDataPreparer.GetAppliedParams();

                case DocType.D27:
                    return EquipmentBillDataPreparer.GetAppliedParams();

                default:
                    throw new Exception($"Таблица данных для типа документа {aType} не определена");

            }
        }

        public string GetDocSign(DocType aDocType, Configuration mainConfig)
        {
            switch (aDocType)
            {
                case DocType.Bill:
                    return BillDataPreparer.GetDocSign(mainConfig);

                case DocType.Specification:
                    return SpecificationDataPreparer.GetDocSign(mainConfig);

                case DocType.ItemsList:
                    return ElementListDataPreparer.GetDocSign(mainConfig);

                case DocType.D27:
                    return EquipmentBillDataPreparer.GetDocSign(mainConfig);

                default:
                    return string.Empty;
            }
        }

    }
}
