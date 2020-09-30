using System;
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
        SubGroup
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

    }

    public static class Constants {
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
        public static readonly string GRAPH_25 = "Перв. примен";

        public static readonly string GRAPH_27 = "";
        public static readonly string GRAPH_28 = "";
        public static readonly string GRAPH_29 = "";
        public static readonly string GRAPH_30 = "";


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

        public static readonly string Settings = "Settings";


        public static readonly string GroupNameSp = "Раздел СП";
        public static readonly string SubGroupNameSp = "Подраздел СП";
        public static readonly string GroupNameB = "Раздел ВП";
        public static readonly string SubGroupNameB = "Подраздел ВП";

        public static readonly string ComponentName = "Наименование";
        public static readonly string ComponentProductCode = "Код продукции";
        public static readonly string ComponentFormat = "Формат";
        public static readonly string ComponentZone = "Зона";
        public static readonly string ComponentDoc = "Документ на поставку";
        public static readonly string ComponentDocCode = "Код документа";
        public static readonly string ComponentType = "Тип";
        public static readonly string ComponentSupplier = "Поставщик";
        public static readonly string ComponentCountDev = "Количество на изд.";
        public static readonly string ComponentCountSet = "Количество в комп.";
        public static readonly string ComponentCountReg = "Количество на рег.";
        public static readonly string ComponentNote = "Примечание";
        public static readonly string ComponentPresence = "Наличие компонента";
        public static readonly string ComponentSign = "Обозначение";
        public static readonly string ComponentWhereIncluded = "Куда входит";
        public static readonly string ComponentDesignatiorID = "Позиционное обозначение";

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
        public static readonly string ColumnDesignation = "Designation";
        /// <summary>
        /// Зона
        /// </summary>
        public static readonly string ColumnZone = "Zone";

        public static readonly string GraphCommentsSp = "Комментарии СП";
        public static readonly string GraphCommentsB = "Комментарии ВП";
        public static readonly string GraphSign = "Обозначение";
        public static readonly string GraphName = "Наименование";

        public static readonly string DOC_SCHEMA = "Схема";

        public static readonly string GostDocType = "GostDoc";
        public static readonly string GostDocTypeB = "GostDocB";

        public static readonly string NewMaterialMenuItem = "<Добавить материал>";

        public static readonly string TemplatesFolder = "Templates";
        public static readonly string SpecificationTemplateName = "specification";
    }
}
