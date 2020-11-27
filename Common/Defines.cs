using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GostDOC.Common
{
    public enum NodeType
    {
        Root,
        Configuration,
        Group,
        SubGroup,
        Component
    }

    public enum SortType
    {        
        SpComplex,              // Спецификация: "Комплексы", "Сборочные единицы" и "Детали"
        SpStandard,             // Спецификация: "Стандартные изделия"
        SpOthers,               // Спецификация: "Прочие изделия"
        SpKits,                 // Спецификация: "Комплекты"
        Name,                   // По алфавиту по тегу "Наименование"
        DesignatorID,           // Перечень элементов: сортировка по значению "Позиционное обозначение"
        None
    }

    /// <summary>
    /// тип документа
    /// </summary>
    public enum DocType
    {
        /// <summary>
        /// Спецификация
        /// </summary>
        Specification,
        /// <summary>
        /// Ведомость покупных изделий
        /// </summary>
        Bill,
        /// <summary>
        /// Ведомость Д27
        /// </summary>
        D27,
        /// <summary>
        /// Перечень элементов
        /// </summary>
        ItemsList,
        /// <summary>
        /// Тип не определен
        /// </summary>
        None = 100
    }

    public enum GraphPageType 
    {
        General,
        Specification,
        Bill
    }

    public enum ComponentType
    {
        Component,
        Document,
        ComponentPCB
    }

    public enum ProjectType
    {
        GostDoc,
        GostDocB,
        Other
    }

    public enum OpenFileResult
    {
        Ok,
        FileFormatError,
        Fail
    }

    /// <summary>
    /// Типы НДС
    /// </summary>
    public enum TaxTypes
    {
        /// <summary>
        /// Без НДС
        /// </summary>
        [Description("Нет")]
        Tax0,
        /// <summary>
        /// НДС 10%
        /// </summary>
        [Description("10%")]
        Tax10,
        /// <summary>
        /// НДС 20%
        /// </summary>
        [Description("20%")]
        Tax20
    }

    /// <summary>
    /// Типы приемки
    /// </summary>
    public enum AcceptanceTypes
    {
        /// <summary>
        /// Без приемки
        /// </summary>
        [Description("Нет")]
        No,
        /// <summary>
        /// Приемка ОТК
        /// </summary>
        [Description("ОТК")]
        TCD,
        /// <summary>
        /// Преимка ВП
        /// </summary>
        [Description("ВП")]
        MA
    }


    public static class Converters
    {
        /// <summary>
        /// Получить строку с кодом документа по типу документа
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <returns></returns>
        public static string GetDocumentCode(DocType aDocType)
        {            
            switch(aDocType)
            {
                case DocType.Bill:
                    return @"ВП";
                case DocType.ItemsList:
                    return @"ПЭ3";
                case DocType.Specification:
                case DocType.D27:
                case DocType.None:
                    return string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Получить строку с кодом документа по типу документа
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <returns></returns>
        public static string GetDocumentName(DocType aDocType, bool aFullName = false)
        {
            switch (aDocType)
            {
                case DocType.Bill:
                    return aFullName ? "Ведомость покупных изделий" : "ВП";
                case DocType.ItemsList:
                    return aFullName ? "Перечень элементов": "ПЭ3";
                case DocType.Specification:
                    return aFullName ? "Спецификация" : "СП";
                case DocType.D27:
                    return aFullName ? "Ведомость комплектации" : "ВК";
                case DocType.None:
                    return string.Empty;
            }
            return string.Empty;
        }


        public static Tuple<string, int> SplitDesignatorToStringAndNumber(string s)
        {
            string val = string.Empty;
            int dig = 0;
            for (int i = 0; i<s.Length ;i++)
            {
                if (Char.IsDigit(s[i]))
                {
                    val = s.Substring(0, i);
                    dig = Int32.Parse(s.Substring(i));
                    break;
                }
            }

            return new Tuple<string, int>(val, dig);
        }

        public static float GetPriceWithTax(float aPurePrice, TaxTypes aTax)
        {
            if (aTax == TaxTypes.Tax10)
                return aPurePrice * 1.1f;

            if (aTax == TaxTypes.Tax20)
                return aPurePrice * 1.2f;

            return aPurePrice;
        }

    }

    public static class Constants {

        #region Графы

        /// <summary>
        /// Название проекта
        /// </summary>
        public static readonly string GRAPH_PROJECT = "Проект";

        /// <summary>
        /// индекс основного исполнения (конфигурации) в виде строки
        /// </summary>
        public static readonly string MAIN_CONFIG_INDEX = "-00";

        /// <summary>
        /// название графы №1 (Наименование изделия) основной надписи в структуре xml
        /// </summary>
        public static readonly string GRAPH_1 = "Наименование";
        /// <summary>
        /// название графы №2 Обозначение документа по ГОСТ 2.201 в структуре xml
        /// </summary>
        public static readonly string GRAPH_2 = "Обозначение";
        /// <summary>
        /// название графы №4 Обозначение Литеры А изделия в структуре xml
        /// </summary>
        public static readonly string GRAPH_4 = "Литера";
        /// название графы №4а Обозначение Литеры О изделия в структуре xml
        /// </summary>
        public static readonly string GRAPH_4a = "Литера2";
        /// название графы №4б Обозначение Литеры О1 изделия в структуре xml
        /// </summary>
        public static readonly string GRAPH_4b = "Литера3";
        
        
        public static readonly string GRAPH_5 = "";
        public static readonly string GRAPH_6 = "";
        public static readonly string GRAPH_7 = "";
        public static readonly string GRAPH_8 = "";

        /// <summary>
        /// название графы №9 Наименование организации в структуре xml
        /// </summary>
        public static readonly string GRAPH_9 = "Организация";
        /// <summary>
        /// название графы №10 Должность контролирующего в структуре xml
        /// </summary>
        public static readonly string GRAPH_10 = "Дополнительная графа";

        public static readonly string GRAPH_11 = "";
        public static readonly string GRAPH_12 = "";
        public static readonly string GRAPH_13 = "";

        /// <summary>
        /// название графы №11 Фамилия разработчика для спецификации в структуре xml
        /// </summary>
        public static readonly string GRAPH_11sp_dev = "Разработал конструктор";
        /// <summary>
        /// название графы №11 Фамилия разработчика для ВП в структуре xml
        /// </summary>
        public static readonly string GRAPH_11bl_dev = "Разработал схемотехник";
        /// <summary>
        /// название графы №11 Фамилия проверяющего для спецификации в структуре xml
        /// </summary>
        public static readonly string GRAPH_11sp_chk = "Проверил конструктор";
        /// <summary>
        /// название графы №11 Фамилия разработчика для ВП в структуре xml
        /// </summary>
        public static readonly string GRAPH_11bl_chk = "Проверил схемотехник";
        /// <summary>
        /// название графы №11 Фамилия нормоконтролера в структуре xml
        /// </summary>
        public static readonly string GRAPH_11norm = "Нормоконтроль";
        /// <summary>
        /// название графы №11 Фамилия утверждающего в структуре xml
        /// </summary>
        public static readonly string GRAPH_11affirm = "Утвердил";
        /// <summary>
        /// название графы №11 Фамилия утверждающего в структуре xml
        /// </summary>
        public static readonly string GRAPH_11app = "Дополнительная графа фамилия";
        /// <summary>
        /// название графы №14 Порядковый номер изменения в документе в структуре xml
        /// </summary>
        public static readonly string GRAPH_14 = "Порядковый номер изменения";
        /// <summary>
        /// название графы №15
        /// </summary>
        public static readonly string GRAPH_15 = "";
        /// <summary>
        /// название графы №16 Номер документа изменение в структуре xml
        /// </summary>
        public static readonly string GRAPH_16 = "Номер документа изменение";
        
        
        public static readonly string GRAPH_17 = "";

        /// <summary>
        /// название графы №18 Дата изменения документа в структуре xml
        /// </summary>
        public static readonly string GRAPH_18 = "Дата изменения";
        /// <summary>
        /// название графы №25 Первичное применение изделия в структуре xml
        /// </summary>
        public static readonly string GRAPH_25 = "Первичная применяемость";

        public static readonly string GRAPH_27 = "";
        public static readonly string GRAPH_28 = "";
        public static readonly string GRAPH_29 = "";
        public static readonly string GRAPH_30 = "";

        #endregion


        #region Группы

        /// <summary>
        /// компоненты, не вошедшие ни в одной группу
        /// </summary>
        public static readonly string DefaultGroupName = "Без группы";
        /// <summary>
        /// раздел "Документация" для спецификации
        /// </summary>
        public static readonly string GroupDoc = "Документация";
        /// <summary>
        /// раздел "Комплексы" для спецификации
        /// </summary>
        public static readonly string GroupComplex = "Комплексы";
        /// <summary>
        /// раздел "Сборочные единицы" для спецификации
        /// </summary>
        public static readonly string GroupAssemblyUnits = "Сборочные единицы";
        /// <summary>
        /// раздел "Детали" для спецификации
        /// </summary>
        public static readonly string GroupDetails = "Детали";
        /// <summary>
        /// раздел "Стандартные изделия" для спецификации
        /// </summary>
        public static readonly string GroupStandard = "Стандартные изделия";
        /// <summary>
        /// раздел "Прочие изделия" для спецификации
        /// </summary>
        public static readonly string GroupOthers = "Прочие изделия";
        /// <summary>
        /// раздел "Материалы" для спецификации
        /// </summary>
        public static readonly string GroupMaterials = "Материалы";

        /// <summary>
        /// раздел "Комплекты" для спецификации
        /// </summary>
        public static readonly string GroupKits = "Комплекты";
        /// <summary>
        /// группа для компонентов, не вошедших ни в одному группу (ВП)
        /// </summary>
        public static readonly string GroupMainCompontns = "";

        public static readonly string GroupOthersB = "Прочие";

        #endregion


        public static readonly string SUBGROUPFORSINGLE = "Прочие";

        public static readonly string Settings = "Settings";

        public static readonly string GroupNameSp = "Раздел СП";
        public static readonly string SubGroupNameSp = "Подраздел СП";
        public static readonly string GroupNameB = "Раздел ВП";
        public static readonly string SubGroupNameB = "Подраздел ВП";

        #region Компоненты

        public static readonly string ComponentName = "Наименование";
        public static readonly string ComponentProductCode = "Код продукции";
        public static readonly string ComponentFormat = "Формат";
        public static readonly string ComponentZone = "Зона";
        public static readonly string ComponentDoc = "Документ на поставку";
        public static readonly string ComponentDocCode = "Код документа";
        public static readonly string ComponentType = "Тип";
        public static readonly string ComponentSupplier = "Поставщик";
        public static readonly string ComponentCount = "Количество";
        public static readonly string ComponentCountDev = "Количество на изд.";
        public static readonly string ComponentCountSet = "Количество в комп.";
        public static readonly string ComponentCountReg = "Количество на рег.";
        public static readonly string ComponentNote = "Примечание";
        public static readonly string ComponentPresence = "Наличие компонента";
        public static readonly string ComponentSign = "Обозначение";
        public static readonly string ComponentWhereIncluded = "Куда входит";
        public static readonly string ComponentDesignatorID = "Позиционное обозначение";
        
        #endregion


        #region Столбцы

        /// <summary>
        /// Формат
        /// </summary>
        public static readonly string ColumnFormat = "Format";
        /// <summary>
        /// Позиция
        /// </summary>
        public static readonly string ColumnPosition = "Position";
        /// <summary>
        /// Наименование
        /// </summary>
        public static readonly string ColumnName = "Name";
        /// <summary>
        /// Кол.
        /// </summary>
        public static readonly string ColumnQuantity = "Quantity";
        /// <summary>
        /// Примечание
        /// </summary>
        public static readonly string ColumnFootnote = "Footnote";
        /// <summary>
        /// Обозначение
        /// </summary>
        public static readonly string ColumnSign = "Sign";
        /// <summary>
        /// Зона
        /// </summary>
        public static readonly string ColumnZone = "Zone";

        /// <summary>
        /// Код продукции
        /// </summary>
        public static readonly string ColumnProductCode = "ProductCode";

        /// <summary>
        /// Обозначение документа на поставку
        /// </summary>
        public static readonly string ColumnDeliveryDocSign = "DeliveryDocSign";

        /// <summary>
        /// Поставщик
        /// </summary>
        public static readonly string ColumnSupplier = "Supplier";

        /// <summary>
        /// Куда входит (обозначение)
        /// </summary>
        public static readonly string ColumnEntry = "Entry";

        /// <summary>
        /// Количество на изделие
        /// </summary>
        public static readonly string ColumnQuantityDevice = "QuantityDevice";

        /// <summary>
        /// Количество в комплекты
        /// </summary>
        public static readonly string ColumnQuantityComplex = "QuantityComplex";

        /// <summary>
        /// Количество на регулир.
        /// </summary>
        public static readonly string ColumnQuantityRegul = "QuantityRegul";

        /// <summary>
        /// Количество всего
        /// </summary>
        public static readonly string ColumnQuantityTotal = "QuantityTotal";

        /// <summary>
        /// признак форматирования выводимого текста
        /// Если не пусто, то это заголовок, иначе перенос строки
        /// </summary>
        public static readonly string ColumnTextFormat = "TextFormat";


        #endregion

        #region Размер столбцов        

        #region Ведомость покупных изделий
        /// <summary>
        /// Ширина столбца "№ строки" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn1IncWidth             = 7.0f;
        /// <summary>
        /// Ширина столбца "Наименование" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn2NameWidth            = 60.0f;
        /// <summary>
        /// Ширина столбца "Код продукции" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn3ProductCodeWidth     = 45.0f;
        /// <summary>
        /// Ширина столбца "Обозначение документа на поставку" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn4DeliveryDocSignWidth = 70.0f;
        /// <summary>
        /// Ширина столбца "Поставщик" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn5SupplierWidth        = 55.0f;
        /// <summary>
        /// Ширина столбца "Куда входит (обозначение)" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn6EntryWidth           = 70.0f;
        /// <summary>
        /// Ширина столбца "Количество: на изделие" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn7QuantityDeviceWidth  = 16.0f;
        /// <summary>
        /// Ширина столбца "Количество: в комплекты" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn8QuantityComplexWidth = 16.0f;
        /// <summary>
        /// Ширина столбца "Количество: на регулир." ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn9QuantityRegulWidth   = 16.0f;
        /// <summary>
        /// Ширина столбца "Количество: всего" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn10QuantityTotalWidth  = 16.0f;
        /// <summary>
        /// Ширина столбца "Примечание" ведомости покупных изделий
        /// </summary>
        public static readonly float BillColumn11FootnoteWidth       = 24.0f;
        #endregion Ведомость покупных изделий

        #region Спецификация
        /// <summary>
        /// Ширина столбца "Формат" спецификации
        /// </summary>
        public static readonly float SpecificationColumn1FormatWidth   = 6.0f;
        /// <summary>
        /// Ширина столбца "Зона" спецификации
        /// </summary>
        public static readonly float SpecificationColumn2ZoneWidth     = 6.0f;
        /// <summary>
        /// Ширина столбца "Поз." спецификации
        /// </summary>
        public static readonly float SpecificationColumn3PositionWidth = 8.0f;
        /// <summary>
        /// Ширина столбца "Обозначение" спецификации
        /// </summary>
        public static readonly float SpecificationColumn4SignWidth     = 70.0f;
        /// <summary>
        /// Ширина столбца "Наименование" спецификации
        /// </summary>
        public static readonly float SpecificationColumn5NameWidth     = 63.0f;
        /// <summary>
        /// Ширина столбца "Кол." спецификации
        /// </summary>
        public static readonly float SpecificationColumn6QuantityWidth = 10.0f;
        /// <summary>
        /// Ширина столбца "Примечание" спецификации
        /// </summary>
        public static readonly float SpecificationColumn7FootnoteWidth = 22.0f;
        #endregion Спецификация

        #region Перечень элементов
        /// <summary>
        /// Ширина столбца "Поз. обозначение" перечня элементов
        /// </summary>
        public static readonly float ItemsListColumn1PositionWidth = 20.0f;
        /// <summary>
        /// Ширина столбца "Наименование" перечня элементов
        /// </summary>
        public static readonly float ItemsListColumn2NameWidth     = 110.0f;
        /// <summary>
        /// Ширина столбца "Кол." перечня элементов
        /// </summary>
        public static readonly float ItemsListColumn3QuantityWidth = 10.0f;
        /// <summary>
        /// Ширина столбца "Примечание" перечня элементов
        /// </summary>
        public static readonly float ItemsListColumn4FootnoteWidth = 45.0f;        
        #endregion Перечень элементов

        #endregion


        #region Графы Дракулы

        public static readonly string GraphCommentsSp = "Комментарии СП";
        public static readonly string GraphCommentsB = "Комментарии ВП";
        public static readonly string GraphSign = "Обозначение";
        public static readonly string GraphName = "Наименование";

        #endregion


        public static readonly string DOC_SCHEMA = "Схема";

        public static readonly string GostDocType = "GostDoc";
        public static readonly string GostDocTypeB = "GostDocB";

        public static readonly string NewMaterialMenuItem = "<Добавить материал>";

        #region Файлы

        // Excel
        public static readonly string TemplatesFolder = "Templates";
        public static readonly string SpecificationTemplateName = "specification";
        public static readonly string BillTemplateName = "purchased_items_list";
        // Cfgs
        public static readonly string MaterialGroupsCfg = "MaterialGroups";
        // Xml
        public static readonly string MaterialsXml = "Materials.xml";
        public static readonly string OthersXml = "Others.xml";
        public static readonly string StandardXml = "Standard.xml";

        #endregion Файлы

        #region Шрифты

        public static readonly float SpecificationFontSize = 12.0f;
        public static readonly float BillFontSize = 12.0f;
        public static readonly float ItemListFontSize = 12.0f;

        public static string DesignatiorID { get; internal set; }

        /// <summary>
        /// размер шрифта для вывода символа литеры в соответсвующей графе основной надписи
        /// </summary>
        public static readonly float LiteraFullFontSize = 11.0f;
        /// <summary>
        /// размер шрифта для вывода числа рядом с символом литеры в соответсвующей графе основной надписи
        /// </summary>
        public static readonly float LiteraSmallFontSize = 7.0f;



        #endregion


        #region Количество строк на страницах документоа
        /// <summary>
        /// количество строк на первом листе для ведомости покупных изделий (ВП)
        /// </summary>
        public static readonly int BillRowsOnFirstPage = 25;
        /// <summary>
        /// количество строк на всех листах кроме первого для ведомости покупных изделий (ВП)
        /// </summary>
        public static readonly int BillRowsOnNextPage = 32;
        /// <summary>
        /// количество строк на первом листе для спецификации
        /// </summary>
        public static readonly int SpecificationRowsOnFirstPage = 23;
        /// <summary>
        /// количество строк на всех листах кроме первого для спецификации
        /// </summary>
        public static readonly int SpecificationRowsOnNextPage = 30;
        /// <summary>
        /// количество строк на первом листе для перечня элементов (ПЭ3)
        /// </summary>
        public static readonly int ItemListRowsOnFirstPage = 24;
        /// <summary>
        /// количество строк на всех листах кроме первого для перечня элементов (ПЭ3)
        /// </summary>
        public static readonly int ItemListRowsOnNextPage = 30;
        /// <summary>
        /// количество строк на первом листе для документа по умолчанию
        /// </summary>
        public static readonly int DefaultRowsOnFirstPage = 24;
        /// <summary>
        /// количество строк на всех листах кроме первого для документа по умолчанию
        /// </summary>
        public static readonly int DefaultRowsOnNextPage = 29;

        #endregion Количество строк на страницах документоа

        #region Коды дополнительных параметров для предачи от DataPreparation

        public static readonly string AppParamDocSign = "DocSign";

        #endregion


    }
}
