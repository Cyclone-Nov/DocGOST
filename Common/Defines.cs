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

    public static class Constants
    {
        public static readonly string DefaultGroupName = "Без группы";
        public static readonly string GroupDoc = "Документация";
        public static readonly string GroupComplex = "Комплексы";
        public static readonly string GroupAssemblyUnits = "Сборочные единицы";
        public static readonly string GroupDetails = "Детали";
        public static readonly string GroupStandard = "Стандартные изделия";
        public static readonly string GroupOthers = "Прочие изделия";
        public static readonly string GroupMaterials = "Материалы";
        public static readonly string GroupKits = "Комплекты";

        public static readonly string GroupNameSp = "Раздел СП";
        public static readonly string SubGroupNameSp = "Подраздел СП";
        public static readonly string GroupNameB = "Раздел ВП";
        public static readonly string SubGroupNameB = "Подраздел ВП";

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
        public static readonly string ComponentWhereIncluded = "Куда входит";
        public static readonly string ComponentDesignatiorID = "Позиционное обозначение";

        public static readonly string GraphCommentsSp = "Комментарии СП";
        public static readonly string GraphCommentsB = "Комментарии ВП";

        public static readonly string GostDocType = "GostDoc";
        public static readonly string GostDocTypeB = "GostDocB";
    }
}
