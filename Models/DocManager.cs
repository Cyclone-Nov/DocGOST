using System;
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
        private XmlManager _xmlManager { get; } = new XmlManager();

        private PdfManager _pdfManager { get; } = PdfManager.Instance;

        public Project Project { get; private set; } = new Project();

        #region Public
        public bool LoadData(string[] aFiles, string aMainFile)
        {
            return _xmlManager.LoadData(Project, aFiles, aMainFile);
        }

        public bool SaveData(string aFilePath)
        {
            return _xmlManager.SaveData(Project, aFilePath);
        }


        /// <summary>
        /// сохранить изменения в pdf для типа документа aDocType
        /// </summary>
        /// <param name="aDocType">Тип документа</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool SaveChangesInPdf(DocType aDocType)
        {
            throw new NotImplementedException();
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
    }
}