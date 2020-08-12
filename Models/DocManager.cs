using System;
using System.Collections.Generic;
using System.Linq;
using GostDOC.Common;

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

        public Project Project { get; private set; } = new Project();

        #region Public

        public bool LoadData(string aFilePath)
        {
            return XmlManager.LoadData(Project, aFilePath);
        }

        public bool SaveData(string aFilePath)
        {
            return XmlManager.SaveData(Project, aFilePath);
        }
        
        #endregion Public
    }
}