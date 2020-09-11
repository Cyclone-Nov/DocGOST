﻿using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using GostDOC.Common;
using GostDOC.PDF;

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

        #region Public

        public bool LoadData(string aFilePath, DocType aDocType)
        {
            return XmlManager.LoadData(Project, aFilePath, aDocType);
        }

        public bool SaveData(string aFilePath)
        {
            return XmlManager.SaveData(Project, aFilePath);
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
        /// <returns></returns>        
        public bool PreparePDF(DocType aDocType)
        {
            DataTable dataTable = _prepareDataManager.GetDataTable(aDocType);
            IDictionary<string, string> mainConfigGraphs = null;
            if (GetMainConfigurationGraphs(out mainConfigGraphs))
            {
                return _pdfManager.PreparePDF(aDocType, dataTable, mainConfigGraphs);
            }            
            return false;
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
        /// Получить полное имя файл pdf для выбранного типа документа
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string GetPdfFileName(DocType aDocType)
        {
            return _pdfManager.GetFileName(aDocType);
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
            Configuration mainConfig = null;
            if (Project.Configurations.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
            {
                mainGraphs = mainConfig.Graphs;
                return true;
            }
            return false;
        }
    }
}