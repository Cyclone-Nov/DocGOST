﻿using System;
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
        public void Export(DocType aDocType, string aFilePath)
        {
            // Open excel application
            var app = new Excel.Application();
            // Set app visible
            // app.Visible = true;
            // Skip dialog messages
            app.DisplayAlerts = false;
            // Create workbook
            var wb = app.Workbooks.Add();
            // Get exporter
            var export = ExcelExportFactory.GetExporter(aDocType);
            // Export
            export?.Export(app, aFilePath);
            // Save
            wb.SaveAs(aFilePath);
            // App quit
            app.Quit();
        } 
    }
}