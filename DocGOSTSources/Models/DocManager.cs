using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using GostDOC.Common;
using GostDOC.PDF;
using GostDOC.Dictionaries;

namespace GostDOC.Models
{
    class DocManager
    {
        #region Singleton
        private static readonly Lazy<DocManager> _instance = new Lazy<DocManager>(() => new DocManager(), true);
        public static DocManager Instance => _instance.Value;
        DocManager()
        {
        }
        #endregion
        public XmlManager XmlManager { get; } = new XmlManager();

        private PdfManager _pdfManager { get; } = PdfManager.Instance;

        private PrepareManager _prepareDataManager { get; } = PrepareManager.Instance;

        public Project Project { get; private set; } = new Project();

        public ProductTypes Materials { get; } = new ProductTypes(ProductTypesDoc.Materials);
        public ProductTypes Others { get; } = new ProductTypes(ProductTypesDoc.Others);
        public ProductTypes Standard { get; } = new ProductTypes(ProductTypesDoc.Standard);
        public DocumentTypes DocumentTypes { get; } = new DocumentTypes();

        #region Public

        public void Load()
        {
            // Load all dictionaries
            Materials.Load(Constants.MaterialsXml);
            Others.Load(Constants.OthersXml);
            Standard.Load(Constants.StandardXml);

            // Load document types
            DocumentTypes.Load();
        }

        public OpenFileResult LoadData(string aFilePath, DocType aDocType)
        {
            return XmlManager.LoadData(Project, aFilePath, aDocType);
        }

        public bool SaveData(string aFilePath)
        {
            return XmlManager.SaveData(Project, aFilePath);
        }

        public void Reset()
        {
            Project.Type = ProjectType.GostDoc;
            XmlManager.Reset();
        }

        /// <summary>
        /// подготовить данных перед экспортом
        /// </summary>
        /// <param name="aDocType">Тип документа</param>
        /// <returns></returns>        
        public bool PrepareData(DocType aDocType)
        {
            return _prepareDataManager.PrepareDataTable(aDocType, Project.Configurations);
        }

        /// <summary>
        /// сохранить изменения в pdf для типа документа aDocType
        /// </summary>
        /// <param name="aDocType">Тип документа</param>
        /// <param name="aFilePath">Тип документа</param>
        /// <returns></returns>        
        public bool SavePDF(DocType aDocType, string aFilePath)
        {
            if (PrepareData(aDocType))
            {                
                if (PreparePDF(aDocType))
                {
                    try
                    {
                        var data = _pdfManager.GetPDFData(aDocType);
                        System.IO.File.WriteAllBytes(aFilePath, data);
                        return true;
                    } catch (Exception ex)
                    {
                        //res = false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// сохранить изменения в pdf для типа документа aDocType
        /// </summary>
        /// <param name="aDocType">Тип документа</param>
        /// <returns></returns>        
        public bool PreparePDF(DocType aDocType)
        {
            DataTable dataTable = _prepareDataManager.GetDataTable(aDocType);
            if (dataTable != null)
            {
                var appParams = _prepareDataManager.GetAppliedParams(aDocType);
                if (GetMainConfigurationGraphs(out var mainConfigGraphs))
                {
                    return _pdfManager.PreparePDF(aDocType, dataTable, mainConfigGraphs, appParams);
                }
            }
            return false;
        }

        public IDictionary<string, object> GetPreparedDataProperties(DocType aDocType)
        {
            return _prepareDataManager.GetAppliedParams(aDocType);
        }

        /// <summary>
        /// Получить данные для PDF
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <returns></returns>
        public byte[] GetPdfData(DocType aDocType)
        {
            return _pdfManager.GetPDFData(aDocType);
        }
               

        /// <summary>
        /// Получить строку с кодом документа по типу документа
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <returns></returns>
        public string GetDocumentName(DocType aDocType, bool aFullName = false)
        {
            if (aFullName)
            {
                switch (aDocType)
                {
                    case DocType.Bill:
                        return "Ведомость покупных изделий";
                    case DocType.ItemsList:
                        return "Перечень элементов";
                    case DocType.Specification:
                        return "Спецификация";
                    case DocType.D27:
                        return "Ведомость комплектации";
                    case DocType.None:
                        return string.Empty;
                }
            }
            else            
                return GetDocSign(aDocType);
            return string.Empty;
        }

        /// <summary>
        /// признак задания значение для тега позиция компонентов (не из раздела Documentation)
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is position exists]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPositionExists(string aConfig = Constants.MAIN_CONFIG_INDEX)
        {
            if (Project.Configurations.TryGetValue(aConfig, out var mainConfig))
            {
                bool ExistPositionInAnyComponent(List<Component> aComponents)
                {
                    foreach (var cmp in aComponents)
                    {
                        string pos = cmp.GetProperty(Constants.ComponentPosition);
                        if (!string.IsNullOrEmpty(pos) && !string.Equals(pos, "0"))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                var data = mainConfig.Specification;
                foreach (var gr in data)
                {
                    if (!string.Equals(gr.Key, Constants.GroupDoc))
                    {
                        if (ExistPositionInAnyComponent(gr.Value.Components))                        
                            return true;                        
                        else
                        {
                            foreach (var subgr in gr.Value.SubGroups.Values)
                            {
                                if (ExistPositionInAnyComponent(subgr.Components))                                
                                    return true;                                
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion Public

        /// <summary>
        /// Получить графы для основной конфигурации
        /// </summary>        
        /// <param name="mainGraphs">словарь графов основного исполнения</param>
        /// <returns>true - словарь графов извлечен успешно</returns>
        private bool GetMainConfigurationGraphs(out IDictionary<string, string> mainGraphs)
        {
            mainGraphs = null;            
            if (Project.Configurations.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var mainConfig))
            {
                mainGraphs = mainConfig.Graphs;
                return true;
            }
            return false;
        }

        /// <summary>
        /// получить обозначение документа
        /// </summary>
        /// <param name="aDocType">тип документа</param>
        /// <returns></returns>
        private string GetDocSign(DocType aDocType)
        {   
            if (!Project.Configurations.TryGetValue(Constants.MAIN_CONFIG_INDEX, out var mainConfig))
                return string.Empty;
            return _prepareDataManager.GetDocSign(aDocType, mainConfig);            
        }

    }
}