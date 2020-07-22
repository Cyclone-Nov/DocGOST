using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Models
{
    class DocManager
    {
        #region Singleton
        private static readonly Lazy<DocManager> _instance = new Lazy<DocManager>(() => new DocManager(), true);
        public static DocManager Instance => _instance.Value;
        DocManager() { }
        #endregion

        public bool LoadData(string[] aFiles)
        {
            return false;
        }
    }
}
