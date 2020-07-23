using System;
using System.Collections.Generic;

namespace GostDOC.Models
{
    class DocManager
    {
        #region Singleton
        private static readonly Lazy<DocManager> _instance = new Lazy<DocManager>(() => new DocManager(), true);
        public static DocManager Instance => _instance.Value;
        DocManager() 
        {
            FillDefaultGroups();
            FillGraphValues();
        }
        #endregion

        public IList<GraphValues> GeneralGraphValues { get; } = new List<GraphValues>();
        public IList<string> BillGroups { get; } = new List<string>();
        public IList<string> SpecificationGroups { get; } = new List<string>();
        public bool LoadData(string[] aFiles)
        {
            return true;
        }

        private void FillGraphValues()
        {
            GeneralGraphValues.Add(new GraphValues() { Num = "1а", Name = "Наименование изделия", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "1б", Name = "Наименование документа", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "2", Name = "Обозначение документа", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "4а", Name = "Литера", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "4б", Name = "Литера2", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "4в", Name = "Литера3", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "9", Name = "Наименование организации", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "10", Name = "Разраб., Пров., Н. контр., Утв.)", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "11а", Name = "Фамилия разраб.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "11б", Name = "Фамилия пров.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "11б", Name = "Фамилия пров.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "11в", Name = "Фамилия доп. подписанта", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "11г", Name = "Фамилия н.контр.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "11д", Name = "Фамилия утв.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "14", Name = "Порядковый номер изм.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "15", Name = "Лист (нов.зам.)", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "16", Name = "Номер документа изм.", Text = "" });
            GeneralGraphValues.Add(new GraphValues() { Num = "18", Name = "Дата изменения", Text = "" });
        }            
        private void FillDefaultGroups()
        {
            BillGroups.Add("Стандартные изделия");
            BillGroups.Add("Прочие изделия");
            BillGroups.Add("Материалы");

            SpecificationGroups.Add("Документация");
            SpecificationGroups.Add("Комплексы");
            SpecificationGroups.Add("Сборочные единицы");
            SpecificationGroups.Add("Детали");
            SpecificationGroups.Add("Стандартные изделия");
            SpecificationGroups.Add("Прочие изделия");
            SpecificationGroups.Add("Материалы");
            SpecificationGroups.Add("Комплекты");
        }
    }
}
