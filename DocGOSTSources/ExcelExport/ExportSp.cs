﻿using System;
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
    class ExportSp : IExcelExport
    {
        private PrepareManager _prepareManager = PrepareManager.Instance;
        private DocManager _docManager = DocManager.Instance;
        private int _tableRow = 0;           
        private System.Data.DataTable _tbl;
        private IDictionary<string, string> _graphs;

        private int Pages
        {
            get
            {
                return CommonUtils.GetCountPage(DocType.Specification, _tbl.Rows.Count);
            }
        }

        public void Export(Excel.Application aApp, string aFilePath)
        {
            _tbl = DataTableUtils.GetDataTable(DocType.Specification);

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
            if (pages > 3)
            {
                aApp.Sheets["ЛРИ"].Cells[37, ExcelColumn.L] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2); // Cells[35, 12]
                aApp.Sheets["ЛРИ"].Cells[39, ExcelColumn.U] = pages + 1; //Cells[37, 19]
            }
            else
            {
                aApp.Sheets["ЛРИ"].Delete();
            }

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
                sheet.Cells[38, ExcelColumn.L] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_1); // Cells[33, 12]                
                sheet.Cells[35, ExcelColumn.L] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2); //Cells[30, 12]   
                                
                (sheet.Cells[39, ExcelColumn.P] as Excel.Range).FormatLitera(Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4)); // Cells[39, 16]                
                (sheet.Cells[39, ExcelColumn.Q] as Excel.Range).FormatLitera(Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4a)); //Cells[39, 17]                
                (sheet.Cells[39, ExcelColumn.R] as Excel.Range).FormatLitera(Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_4b)); //Cells[39, 18]
                                
                sheet.Cells[38, ExcelColumn.H] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11sp_dev); //Cells[33, 8]                
                sheet.Cells[39, ExcelColumn.H] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11sp_chk); //Cells[34, 8]                
                sheet.Cells[41, ExcelColumn.H] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11norm); //Cells[36, 8]                
                sheet.Cells[42, ExcelColumn.H] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm); //Cells[37, 8]

                Excel.Range range = sheet.Cells[1, ExcelColumn.C]; // Cells[1, 3] 
                range.Value2 = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_25); 
                range.Orientation = 90;
            }
            // Set pages count
            sheet.Cells[39, ExcelColumn.V] = Pages; //Cells[34, 20]

            if (Pages > 1)
            {
                sheet.Cells[39, ExcelColumn.T] = 1; //[34, 19]
            }

            // Fill data
            FillRows(sheet, Constants.SpecificationRowsOnFirstPage, true);
        }

        public void FillSheet(Excel._Worksheet sheet)
        {
            // Set page number
            sheet.Cells[39, ExcelColumn.R] = sheet.Name; //Cells[37, 22]
            if (_graphs != null)
            {
                // Set title
                sheet.Cells[38, ExcelColumn.L] = Utils.GetGraphValue(_graphs, Common.Constants.GRAPH_2); // Cells[35, 12]
            }
            // Fill data
            FillRows(sheet, Constants.SpecificationRowsOnNextPage);
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows, bool aFirst = false)
        {
            if (_tbl == null)
                return;

            int row = 2;
            maxRows++;
            while (row <= maxRows && _tableRow < _tbl.Rows.Count)
            {                
                var sign = _tbl.GetTableValueFS(_tableRow, 4);
                var name = _tbl.GetTableValueFS(_tableRow, 5);
                var pos = _tbl.GetTableValueFS(_tableRow, 3);
                int count = _tbl.GetTableValue<int>(_tableRow, 6);
                var note = _tbl.GetTableValueFS(_tableRow, 7);
                if (string.IsNullOrEmpty(name?.Value) && string.IsNullOrEmpty(sign?.Value))
                {
                    count = 0;
                    if (pos != null)
                        pos.Value = string.Empty;
                }
                int rows = sheet.SetFormattedValue(row, (int)ExcelColumn.D, _tbl.GetTableValueFS(_tableRow, 1)); // format
                sheet.SetFormattedValue(row, (int)ExcelColumn.F, _tbl.GetTableValueFS(_tableRow, 2)); // zone
                sheet.SetFormattedValue(row, (int)ExcelColumn.G, pos);
                sheet.SetFormattedValue(row, (int)ExcelColumn.I, sign);
                sheet.SetFormattedValue(row, (int)ExcelColumn.N, name);

                if (count > 0)
                    sheet.Cells[row, aFirst ? (int)ExcelColumn.T : (int)ExcelColumn.P] = count;

                
                sheet.SetFormattedValue(row, aFirst ? (int)ExcelColumn.U : (int)ExcelColumn.Q, note);

                if (rows > 1)
                    maxRows += rows - 1;

                row+= rows;
                _tableRow++;
            }
        }
    }
}
