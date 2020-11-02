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
        private const int MinRowIndex = 3;
        private const int MaxRowIndexFirst = 26;
        private const int MaxRowIndexSecond = 32;
        private const string BillPostfix = "ВП";

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
            _tbl = DataTableExtensions.GetDataTable(DocType.Bill);

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
            aApp.Sheets["ЛРИ"].Cells[34, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
            aApp.Sheets["ЛРИ"].Cells[36, 19] = pages + 1;

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
                sheet.Cells[34, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_1);
                sheet.Cells[31, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2) + BillPostfix;
                sheet.Cells[35, 24] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4);
                sheet.Cells[35, 25] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4a);
                sheet.Cells[35, 26] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4b);

                sheet.Cells[34, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_dev);
                sheet.Cells[35, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_chk);
                sheet.Cells[37, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11norm);
                sheet.Cells[38, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm);
            }
            // Set pages count
            sheet.Cells[35, 29] = Pages + 1;
            // Fill data
            FillRows(sheet, MaxRowIndexFirst);
        }

        public void FillSheet(Excel._Worksheet sheet)
        {
            // Set page number
            sheet.Cells[37, 30] = sheet.Name;
            
            if (_graphs != null)
            {
                // Set title
                sheet.Cells[35, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2) + BillPostfix;
            }
            // Fill data
            FillRows(sheet, MaxRowIndexSecond);
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows)
        {
            if (_tbl == null)
                return;

            int row = 3;
            while (row <= maxRows && _tableRow < _tbl.Rows.Count)
            {                
                sheet.Cells[row, 4] = _tbl.GetTableValue(_tableRow, 1); // Наименование
                sheet.Cells[row, 5] = _tbl.GetTableValue(_tableRow, 2);// Код продукции
                sheet.Cells[row, 6] = _tbl.GetTableValue(_tableRow, 3);// Обозначение документа на поставку
                sheet.Cells[row, 7] = _tbl.GetTableValue(_tableRow, 4);// Поставщик
                sheet.Cells[row, 14] = _tbl.GetTableValue(_tableRow, 5);// Куда входит (обозначение)
                sheet.Cells[row, 19] = _tbl.GetTableValue<int>(_tableRow, 6);// Количество на изделие
                sheet.Cells[row, 21] = _tbl.GetTableValue<int>(_tableRow, 7);// Количество в комплекты
                sheet.Cells[row, 23] = _tbl.GetTableValue<int>(_tableRow, 8);// Количество на регулир.
                sheet.Cells[row, 25] = _tbl.GetTableValue<int>(_tableRow, 9);// Количество всего
                sheet.Cells[row, 28] = _tbl.GetTableValue(_tableRow, 10);// Примечание
                
                row++;
                _tableRow++;
            }
        }
    }
}
