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

    }

    public static class Constants
    {
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
