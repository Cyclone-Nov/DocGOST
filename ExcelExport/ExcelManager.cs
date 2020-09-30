using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    class ExcelManager
    {
        public bool CanExport(DocType aDocType)
        {
            return aDocType == DocType.D27 || aDocType == DocType.Specification;
        }

        public void Export(DocType aDocType, string aFilePath)
        {
            Task.Run(() =>
            {
                // Get exporter
                var export = ExcelExportFactory.GetExporter(aDocType);

                if (export != null)
                {
                    // Open excel application
                    var app = new Excel.Application();
                    // Set app visible
                    //app.Visible = true;
                    // Skip dialog messages
                    app.DisplayAlerts = false;
                    // Export
                    export.Export(app, aFilePath);
                    // App quit
                    app.Quit();
                }
            });
        } 
    }
}
