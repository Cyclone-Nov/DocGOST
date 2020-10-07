﻿using System;
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
                        count = (_tbl.Rows.Count - RowCountFirst) % RowCountSecond + 1;
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
                FillSheet(sheet);

                // Fill other sheets
                for (int i = 3; i < pages; i++)
                {
                    // Copy 2nd sheet
                    sheet.Copy(After: aApp.Sheets[i - 1]);
                    // Set name
                    aApp.Sheets[i].Name = i.ToString();
                    // Fill sheet
                    FillSheet(aApp.Sheets[i]);
                }
            }
            else
            {
                // Remove 2nd sheet
                aApp.Sheets["2"].Delete();
            }
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
                sheet.Cells[34, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_1);
                sheet.Cells[31, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
                sheet.Cells[35, 24] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4);
                sheet.Cells[35, 25] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4a);
                sheet.Cells[35, 26] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4b);

                sheet.Cells[34, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_dev);
                sheet.Cells[35, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11bl_chk);
                sheet.Cells[37, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11norm);
                sheet.Cells[38, 12] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm);
            }
            // Set pages count
            sheet.Cells[35, 29] = Pages;
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
                sheet.Cells[35, 17] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2);
            }
            // Fill data
            FillRows(sheet, MaxRowIndexSecond);
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows)
        {
            int row = 3;
            while (row <= maxRows && _tableRow < _tbl.Rows.Count)
            {
                // TODO: Fill table items
                /*
                sheet.Cells[row, 4] = _tbl.GetTableValue(_tableRow, 1);
                sheet.Cells[row, 6] = _tbl.GetTableValue(_tableRow, 2);
                sheet.Cells[row, 7] = _tbl.GetTableValue(_tableRow, 3);
                sheet.Cells[row, 9] = _tbl.GetTableValue(_tableRow, 4);
                sheet.Cells[row, 14] = _tbl.GetTableValue(_tableRow, 5);

                int count = _tbl.GetTableValue<int>(_tableRow, 6);
                if (count > 0)
                    sheet.Cells[row, 20] = count;

                sheet.Cells[row, 22] = _tbl.GetTableValue(_tableRow, 7);
                */

                row++;
                _tableRow++;
            }
        }
    }
}