﻿using System;
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
        private XmlManager _xmlManager { get; } = new XmlManager();

        public Project Project { get; private set; } = new Project();

        #region Public
        public bool LoadData(string[] aFiles, string aMainFile)
        {
            return _xmlManager.LoadData(Project, aFiles, aMainFile);
        }
        #endregion Public
    }
}