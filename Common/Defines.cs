﻿using System;
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
        Elements,
        Specification,
        Bill,
        Bill_D27,
        Configuration,
        Group,
        SubGroup
    }

    public enum SortType
    {
        Specification,
        SpecificationOthers,
        Bill,
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
        Specification = NodeType.Specification,
        /// <summary>
        /// Ведомость покупных изделий
        /// </summary>
        Bill = NodeType.Bill,
        /// <summary>
        /// Ведомость Д27
        /// </summary>
        D27 = NodeType.Bill_D27,
        /// <summary>
        /// Перечень элементов
        /// </summary>
        ItemsList = NodeType.Elements,
        /// <summary>
        /// Тип не определен
        /// </summary>
        None = 100
    }

    public enum GraphPageType 
    {
        General
    }

    public enum ComponentType
    {
        Component,
        Document,
        ComponentPCB
    }

    public static class Converters
    {
        /// <summary>
        /// Gets the type of the PDF.
        /// </summary>
        /// <param name="aNodeType">Type of a node.</param>
        /// <returns></returns>
        public static DocType GetPdfType(NodeType aNodeType)
        {
            DocType res = (DocType)aNodeType;
            return res;
        }

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

    public static class Constants
    {
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
        /// <summary>
        /// название графы №9 Наименование организации в структуре xml
        /// </summary>
        public static readonly string GRAPH_9 = "Организация";
        /// <summary>
        /// название графы №10 Должность контролирующего в структуре xml
        /// </summary>
        public static readonly string GRAPH_10 = "Дополнительная графа";
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
        /// <summary>
        /// название графы №18 Дата изменения документа в структуре xml
        /// </summary>
        public static readonly string GRAPH_18 = "Дата изменения";
        /// <summary>
        /// название графы №25 Первичное применение изделия в структуре xml
        /// </summary>
        public static readonly string GRAPH_25 = "Перв. примен";
        

        public static readonly string DefaultGroupName = "Без группы";
        public static readonly string GroupNameDoc = "Документация";
        public static readonly string GroupNameSp = "Раздел СП";
        public static readonly string SubGroupNameSp = "Подраздел СП";
        public static readonly string GroupNameB = "Раздел ВП";
        public static readonly string SubGroupNameB = "Подраздел ВП";
        public static readonly string GroupOthers = "Прочие изделия";


        public static readonly string ComponentName = "Наименование";
        public static readonly string ComponentProductCode = "Код продукции";
        public static readonly string ComponentFormat = "Формат";
        public static readonly string ComponentDoc = "Документ на поставку";
        public static readonly string ComponentSupplier = "Поставщик";
        public static readonly string ComponentCountDev = "Количество на изд.";
        public static readonly string ComponentCountSet = "Количество в комп.";
        public static readonly string ComponentCountReg = "Количество на рег.";
        public static readonly string ComponentNote = "Примечание";
        public static readonly string ComponentSign = "Обозначение";
        public static readonly string ComponentDesignatiorID = "Позиционное обозначение";
    }
}
