using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using GostDOC.Common;

namespace GostDOC.Models
{
    class XmlManager
    {
        private XDocument _document;
        private HashSet<string> _billGroups = new HashSet<string>();
        private HashSet<string> _specificationGroups = new HashSet<string>();

        public IList<GraphValues> GeneralGraphValues { get; } = new List<GraphValues>();
        public IEnumerable<string> BillGroups => _billGroups;
        public IEnumerable<string> SpecificationGroups => _specificationGroups;

        public XmlManager()
        {
        }

        public bool LoadData(string[] aFiles, string aMainFile)
        {            
            if (aFiles.Length > 1)
            {
                // Merge files to one
                return MergeFiles(aFiles, aMainFile);
            }
            else
            {
                // Load file
                _document = XDocument.Load(aFiles[0]);
            }

            UpdateGroups();
            UpdateGraphValues();

            return _document != null;
        }

        private void UpdateGraphValues()
        {
            GeneralGraphValues.Clear();
            GeneralGraphValues.Add(new GraphValues() { Num = "1а", Name = "Наименование изделия", Text = _document.ReadElementValue("Наименование")});
            GeneralGraphValues.Add(new GraphValues() { Num = "1б", Name = "Наименование документа", Text = _document.ReadElementValue("Наименование_PCB") });
            GeneralGraphValues.Add(new GraphValues() { Num = "2", Name = "Обозначение документа", Text = _document.ReadElementValue("Обозначение") });
            GeneralGraphValues.Add(new GraphValues() { Num = "4а", Name = "Литера", Text = _document.ReadElementValue("Литера") });
            GeneralGraphValues.Add(new GraphValues() { Num = "4б", Name = "Литера2", Text = _document.ReadElementValue("Литера2") });
            GeneralGraphValues.Add(new GraphValues() { Num = "4в", Name = "Литера3", Text = _document.ReadElementValue("Литера3") });
            GeneralGraphValues.Add(new GraphValues() { Num = "9", Name = "Наименование организации", Text = _document.ReadElementValue("?") });
            GeneralGraphValues.Add(new GraphValues() { Num = "10", Name = "Разраб., Пров., Н. контр., Утв.)", Text = _document.ReadElementValue("п_Доп_графа") });
            GeneralGraphValues.Add(new GraphValues() { Num = "11а", Name = "Фамилия разраб.", Text = _document.ReadElementValue("п_Разраб") });
            GeneralGraphValues.Add(new GraphValues() { Num = "11б", Name = "Фамилия разраб.Р.", Text = _document.ReadElementValue("п_Разраб_P") });
            GeneralGraphValues.Add(new GraphValues() { Num = "11в", Name = "Фамилия пров.", Text = _document.ReadElementValue("п_Пров") });
            GeneralGraphValues.Add(new GraphValues() { Num = "11г", Name = "Фамилия пров.Р.", Text = _document.ReadElementValue("п_Пров_P") });
            GeneralGraphValues.Add(new GraphValues() { Num = "11д", Name = "Фамилия н.контр.", Text = _document.ReadElementValue("п_Н_контр") });
            GeneralGraphValues.Add(new GraphValues() { Num = "11е", Name = "Фамилия утв.", Text = _document.ReadElementValue("п_Утв") });
            GeneralGraphValues.Add(new GraphValues() { Num = "14", Name = "Порядковый номер изм.", Text = _document.ReadElementValue("Порядковый_номер_изменения") });
            GeneralGraphValues.Add(new GraphValues() { Num = "15", Name = "Лист (нов.зам.)", Text = _document.ReadElementValue("?") });
            GeneralGraphValues.Add(new GraphValues() { Num = "16", Name = "Номер документа изм.", Text = _document.ReadElementValue("Номер_документа_изменение") });
            GeneralGraphValues.Add(new GraphValues() { Num = "18", Name = "Дата изменения", Text = _document.ReadElementValue("Дата_изменения") });
        }

        private void UpdateGroups()
        {
            _billGroups.Clear();
            _billGroups.Add("Стандартные изделия");
            _billGroups.Add("Прочие изделия");
            _billGroups.Add("Материалы");
            _billGroups.AddRange(_document.ReadElementValues("Group"));

            _specificationGroups.Clear();
            _specificationGroups.Add("Документация");
            _specificationGroups.Add("Комплексы");
            _specificationGroups.Add("Сборочные единицы");
            _specificationGroups.Add("Детали");
            _specificationGroups.Add("Стандартные изделия");
            _specificationGroups.Add("Прочие изделия");
            _specificationGroups.Add("Материалы");
            _specificationGroups.Add("Комплекты");
            _specificationGroups.AddRange(_document.ReadElementValues("Group"));
        }

        private bool MergeFiles(string[] aFiles, string aMainFile)
        {
            // Find main file
            List<string> otherFiles = new List<string>();
            foreach (var file in aFiles)
            {
                if (file.EndsWith(aMainFile))
                {
                    _document = XDocument.Load(file);
                }
                else
                {
                    otherFiles.Add(file);
                }
            }

            // Find components tag
            var references = _document.FindElement("references");
            if (references == null)
            {
                return false;
            }

            // Read all components and write them to main file
            foreach (var file in otherFiles)
            {
                var doc = XDocument.Load(file);
                foreach (var component in doc.FindElements("component"))
                {
                    references.Add(component);
                }
            }

            return true;
        }
    }
}
