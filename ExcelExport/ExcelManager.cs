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
        public void Export(DocType aDocType, string aFilePath)
        {
            // Open excel application
            var app = new Excel.Application();
            
            app.Visible = true;
            var wb = app.Workbooks.Add();

            // Get current sheet
            Excel._Worksheet ws = (Excel.Worksheet)app.ActiveSheet;

            // TODO: Fill excel file
            switch (aDocType)
            {
                case DocType.Bill:
                case DocType.D27:
                case DocType.Specification:
                case DocType.ItemsList:
                    break;
            }

            // Save file
            ws.SaveAs(aFilePath);
            // App quit
            app.Quit();
        } 
    }
}
