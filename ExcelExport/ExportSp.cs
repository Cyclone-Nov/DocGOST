using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.DataPreparation;
using GostDOC.Interfaces;
using GostDOC.Models;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    class ExportSp : IExcelExport
    {
        private const int MinRowIndex = 2;
        private const int MaxRowIndexFirst = 25;
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
            _tbl = DataTableExtensions.GetDataTable(DocType.Specification);

            var wb = aApp.Workbooks.Open(Utils.GetTemplatePath(Constants.SpecificationTemplateName));

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
            aApp.Sheets["ЛРИ"].Cells[35, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
            aApp.Sheets["ЛРИ"].Cells[37, 19] = pages + 1;

            // Select 1st sheet
            aApp.Sheets["1"].Select();
            // Save
            wb.SaveAs(aFilePath);
        }

        public void FillFirstSheet(Excel.Application aApp)
        {
            var sheet = aApp.Sheets["1"];

            if (_graphs != null)
            {
                // Fill main title
                sheet.Cells[35, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_1);
                sheet.Cells[32, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
                sheet.Cells[36, 16] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4);
                sheet.Cells[36, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4a);
                sheet.Cells[36, 18] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4b);

                sheet.Cells[35, 8] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11sp_dev);
                sheet.Cells[36, 8] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11sp_chk);
                sheet.Cells[38, 8] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11norm);
                sheet.Cells[39, 8] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm);
            }
            // Set pages count
            sheet.Cells[36, 22] = Pages + 1;
            // Fill data
            FillRows(sheet, MaxRowIndexFirst);
        }

        public void FillSheet(Excel._Worksheet sheet)
        {
            // Set page number
            sheet.Cells[37, 22] = sheet.Name;
            if (_graphs != null)
            {
                // Set title
                sheet.Cells[35, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
            }
            // Fill data
            FillRows(sheet, MaxRowIndexSecond);
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows)
        {
            if (_tbl == null)
                return;

            int row = MinRowIndex;
            while (row <= maxRows && _tableRow < _tbl.Rows.Count)
            { 
                sheet.Cells[row, 4] = _tbl.GetTableValue(_tableRow, 1);
                sheet.Cells[row, 6] = _tbl.GetTableValue(_tableRow, 2);
                sheet.Cells[row, 7] = _tbl.GetTableValue(_tableRow, 3);
                sheet.Cells[row, 9] = _tbl.GetTableValue(_tableRow, 4);
                sheet.Cells[row, 14] = _tbl.GetTableValue(_tableRow, 5);

                int count = _tbl.GetTableValue<int>(_tableRow, 6);
                if (count > 0)
                    sheet.Cells[row, 20] = count;
               
                sheet.Cells[row, 22] = _tbl.GetTableValue(_tableRow, 7);

                row++;
                _tableRow++;
            }
        }
    }
}
