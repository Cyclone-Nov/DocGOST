using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    class ExcelManager
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler ExportComplete;             

        public bool CanExport(DocType aDocType)
        {
            return aDocType == DocType.D27 || aDocType == DocType.Specification || aDocType == DocType.Bill || aDocType == DocType.ItemsList;
        }

        public void Export(DocType aDocType, string aFilePath)
        {
            Task.Run(() =>
            {
                // Open excel application
                Excel.Application app = null;
                try
                {
                    // Get exporter
                    var export = ExcelExportFactory.GetExporter(aDocType);

                    if (export != null)
                    {
                        app = new Excel.Application();
                        // Set app visible
                        //app.Visible = true;
                        // Skip dialog messages
                        app.DisplayAlerts = false;
                        // Export
                        export.Export(app, aFilePath);
                    }
                }
                catch(Exception ex)
                {
                    _log.Error(ex);
                }
                finally
                {
                    // App quit
                    app?.Application.Quit();
                    app?.Quit();

                    ExportComplete?.Invoke(this, new EventArgs());
                }
            });
        } 
    }
}
