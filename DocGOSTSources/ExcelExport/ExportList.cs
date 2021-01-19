using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.DataPreparation;
using GostDOC.Interfaces;
using GostDOC.Models;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    class ExportList : IExcelExport
    {
        private const int MinRowIndex = 2;
        private const int MaxRowIndexFirst = 24;
        private const int MaxRowIndexSecond = 30;

        private const int RowCountFirst = MaxRowIndexFirst - MinRowIndex + 1;
        private const int RowCountSecond = MaxRowIndexSecond - MinRowIndex + 1;

        private PrepareManager _prepareManager = PrepareManager.Instance;
        private DocManager _docManager = DocManager.Instance;
        private int _tableRow = 0;           
        private System.Data.DataTable _tbl;
        private IDictionary<string, string> _graphs;

        private int Pages
        {
            get
            {
                int count = 1;
                if (_tbl != null)
                {
                    if (_tbl.Rows.Count > RowCountFirst)
                    {
                        count += (_tbl.Rows.Count - RowCountFirst) / RowCountSecond + 1;
                    }
                }
                return count;
            }
        }

        public void Export(Excel.Application aApp, string aFilePath)
        {
            _tbl = DataTableUtils.GetDataTable(DocType.ItemsList);

            var wb = aApp.Workbooks.Open(Utils.GetTemplatePath(Constants.ItemsListTemplateName));

            _graphs = ExcelUtils.GetGraphs();

            // Fill 1st sheet
            FillFirstSheet(aApp);

            int pages = Pages;
            if (pages > 1)
            {
                // Create 2nd sheet
                var sheet = aApp.Sheets["2"];

                for (int i = 3; i <= pages; i++)
                {
                    // Copy 2nd sheet
                    sheet.Copy(After: aApp.Sheets[i - 1]);
                    // Set name
                    aApp.Sheets[i].Name = i.ToString();
                }

                // Fill 2nd sheet
                FillSheet(sheet);

                // Fill other sheets
                for (int i = 3; i <= pages; i++)
                {
                    // Fill sheet
                    FillSheet(aApp.Sheets[i]);
                }
            }
            else
            {
                // Remove 2nd sheet
                aApp.Sheets["2"].Delete();
            }

            // Update ЛРИ
            aApp.Sheets["ЛРИ"].Cells[37, ExcelColumn.L] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
            aApp.Sheets["ЛРИ"].Cells[39, ExcelColumn.T] = pages + 1;

            // Select 1st sheet
            aApp.Sheets["1"].Select();
            // Save
            wb.SaveAs(aFilePath);
            // Close
            wb.Close(false);
        }

        public void FillFirstSheet(Excel.Application aApp)
        {
            var sheet = aApp.Sheets["1"];

            if (_graphs != null)
            {
                // Fill main title
                sheet.Cells[40, ExcelColumn.J] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_1); // [33, 10]
                sheet.Cells[37, ExcelColumn.J] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2); // [30, 10]                
                sheet.Cells[41, ExcelColumn.N] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4); // [34, 14]
                sheet.Cells[41, ExcelColumn.O] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4a); // [34, 16]
                sheet.Cells[41, ExcelColumn.P] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4b); // [34, 17]                
                sheet.Cells[1, ExcelColumn.C] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_25);// [34, 17]

                sheet.Cells[42, ExcelColumn.D] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_10); // [33, 6]
                sheet.Cells[42, ExcelColumn.F] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11app); // [33, 6]

                sheet.Cells[40, ExcelColumn.F] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_dev); // [33, 6]
                sheet.Cells[41, ExcelColumn.F] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_chk); // [34, 6]
                sheet.Cells[43, ExcelColumn.F] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11norm);   // [36, 6]
                sheet.Cells[44, ExcelColumn.F] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm); // [37, 6]
            }

            // Set pages count            
            sheet.Cells[41, ExcelColumn.R] = Pages > 2 ? Pages + 1 : Pages; //[34, 19]

            if (Pages > 1)
            {
                sheet.Cells[41, ExcelColumn.Q] = 1; //[34, 19]
            }
                        
            // Fill data
            FillRows(sheet, MaxRowIndexFirst, true);
        }

        public void FillSheet(Excel._Worksheet sheet)
        {
            // Set page number
            sheet.Cells[41, ExcelColumn.N] = sheet.Name; //[37, 16]
            if (_graphs != null)
            {
                // Set title
                sheet.Cells[39, ExcelColumn.J] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2); //[34, 10]
            }
            // Fill data
            FillRows(sheet, MaxRowIndexSecond);
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows, bool aFirst = false)
        {
            if (_tbl == null)
                return;

            int row = 4;// MinRowIndex;
            maxRows += 3;
            while (row <= maxRows && _tableRow < _tbl.Rows.Count)
            {
                int rows = sheet.SetFormattedValue(row, (int)ExcelColumn.D, _tbl.GetTableValueFS(_tableRow, 1));
                //sheet.Cells[row, 4] = _tbl.GetTableValue(_tableRow, 1);                
                sheet.SetFormattedValue(row, (int)ExcelColumn.G, _tbl.GetTableValueFS(_tableRow, 2));
                //sheet.Cells[row, 7] = _tbl.GetTableValue(_tableRow, 2);

                int count = _tbl.GetTableValue<int>(_tableRow, 3);
                if (count > 0)
                {
                    sheet.Cells[row, (int)ExcelColumn.L] = count;
                }

                sheet.SetFormattedValue(row, (int)ExcelColumn.M, _tbl.GetTableValueFS(_tableRow, 4));
                //sheet.Cells[row, 13] = _tbl.GetTableValue(_tableRow, 4);

                if (rows > 1)
                    maxRows += rows - 1;

                row += rows;
                _tableRow++;
            }
        }
    }
}
