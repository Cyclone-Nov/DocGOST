using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Interfaces;
using GostDOC.Models;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    class ExportBill : IExcelExport
    {
        private const string BillPostfix = "ВП";

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
                    if (_tbl.Rows.Count > Constants.BillRowsOnFirstPage)
                    {
                        count += (_tbl.Rows.Count - Constants.BillRowsOnFirstPage) / Constants.BillRowsOnNextPage + 1;
                    }
                }
                return count;
            }
        }

        public void Export(Excel.Application aApp, string aFilePath)
        {
            _tbl = DataTableUtils.GetDataTable(DocType.Bill);

            // Open template file
            var wb = aApp.Workbooks.Open(Utils.GetTemplatePath(Constants.BillTemplateName));

            _graphs = ExcelUtils.GetGraphs();

            // Fill 1st sheet
            FillFirstSheet(aApp);

            int pages = Pages;
            if (pages > 1)
            {
                // Fill 2nd sheet
                var sheet = aApp.Sheets["2"];

                for (int i = 3; i <= pages; i++)
                {
                    // Copy 2nd sheet
                    sheet.Copy(After: aApp.Sheets[i - 1]);
                    // Set name
                    aApp.Sheets[i].Name = i.ToString();
                }

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
            aApp.Sheets["ЛРИ"].Cells[39, ExcelColumn.U] = pages + 1;

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
                sheet.Cells[41, ExcelColumn.O] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_1);
                string sign = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2) + BillPostfix;
                sheet.Cells[38, ExcelColumn.O] = sign;
                sheet.Cells[42, ExcelColumn.T] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4);
                sheet.Cells[42, ExcelColumn.U] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4a);
                sheet.Cells[42, ExcelColumn.W] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4b);

                sheet.Cells[1, ExcelColumn.C] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_25);// [34, 17]

                sheet.Cells[43, ExcelColumn.I] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_10); // [33, 6]
                sheet.Cells[43, ExcelColumn.K] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11app); // [33, 6]

                sheet.Cells[41, ExcelColumn.K] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_dev);
                sheet.Cells[42, ExcelColumn.K] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_chk);
                sheet.Cells[44, ExcelColumn.K] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11norm);
                sheet.Cells[45, ExcelColumn.K] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm);

                // Set approved sign
                sheet.Cells[36, ExcelColumn.I] = $"Утвержден {sign}-ЛУ";
            }
            // Set pages count
            sheet.Cells[42, ExcelColumn.AA] = Pages > 2 ? Pages + 1 : Pages;

            if (Pages > 1)            
                sheet.Cells[42, ExcelColumn.Y] = 1; //[34, 19]

            

            // Fill data
            FillRows(sheet, Constants.BillRowsOnFirstPage, true);
        }

        public void FillSheet(Excel._Worksheet sheet)
        {
            // Set page number
            sheet.Cells[44, ExcelColumn.W] = sheet.Name;
            
            if (_graphs != null)
            {
                // Set title
                sheet.Cells[42, ExcelColumn.O] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2) + BillPostfix;
            }
            // Fill data
            FillRows(sheet, Constants.BillRowsOnNextPage);
        }

        private void FillCount(Excel._Worksheet sheet, int row, int col, int tableIndex)
        {
            var val = _tbl.GetTableValue<int>(_tableRow, tableIndex);
            if (val != 0)
            {
                sheet.Cells[row, col] = val; 
            }
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows, bool aFirstPage = false)
        {
            if (_tbl == null)
                return;

            int row = 3;
            maxRows += 2;
            while (row <= maxRows && _tableRow < _tbl.Rows.Count)
            {
                int rows = sheet.SetFormattedValue(row, (int)ExcelColumn.E, _tbl.GetTableValueFS(_tableRow, 1));  // Наименование
                sheet.SetFormattedValue(row, (int)ExcelColumn.F, _tbl.GetTableValueFS(_tableRow, 2));  // Код продукции
                sheet.SetFormattedValue(row, (int)ExcelColumn.G, _tbl.GetTableValueFS(_tableRow, 3));  // Обозначение документа на поставку
                sheet.SetFormattedValue(row, (int)ExcelColumn.H, _tbl.GetTableValueFS(_tableRow, 4));  // Поставщик
                sheet.SetFormattedValue(row, (int)ExcelColumn.L, _tbl.GetTableValueFS(_tableRow, 5)); // Куда входит (обозначение)

                FillCount(sheet, row, (int)ExcelColumn.Q, 6); // Количество на изделие
                FillCount(sheet, row, (int)ExcelColumn.R, 7); // Количество в комплекты
                FillCount(sheet, row, (int)ExcelColumn.S, 8); // Количество на регулир.

                sheet.SetFormattedValue(row, aFirstPage ? (int)ExcelColumn.X : (int)ExcelColumn.U, _tbl.GetTableValueFS(_tableRow, 9)); // Количество всего
                sheet.SetFormattedValue(row, aFirstPage ? (int)ExcelColumn.Z : (int)ExcelColumn.V, _tbl.GetTableValueFS(_tableRow, 10)); // Примечание

                if (rows > 1)
                    maxRows += rows - 1;

                row += rows;
                _tableRow++;
            }
        }
    }
}
