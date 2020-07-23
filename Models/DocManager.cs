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
        }
        #endregion
        public XmlManager XmlManager { get; } = new XmlManager();
    }
}
