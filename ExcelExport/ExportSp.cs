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
                    if (_tbl.Rows.Count > 26)
                    {
                        count = (_tbl.Rows.Count - 26) % 33 + 1;
                    }
                }
                return count;
            }
        }

        public void Export(Excel.Application aApp, string aFilePath)
        {
            if (_prepareManager.PrepareDataTable(DocType.Specification, _docManager.Project.Configurations))
            {
                _tbl = _prepareManager.GetDataTable(DocType.Specification);

                var wb = aApp.Workbooks.Open(Utils.GetTemplatePath(Constants.SpecificationTemplateName));

                foreach (var cfg in _docManager.Project.Configurations)
                {
                    if (cfg.Value.Graphs.Count > 0)
                    {
                        _graphs = cfg.Value.Graphs;
                        break;
                    }
                }

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
                        sheet.Copy(After: aApp.Sheets[i - 1]);
                        aApp.Sheets[i].Name = i.ToString();
                        FillSheet(aApp.Sheets[i]);
                    }
                }
                else
                {
                    aApp.Sheets["2"].Delete();
                }
                wb.SaveAs(aFilePath);
            }
        }

        private string GetGraphValue(IDictionary<string, string> graphs, string name)
        {
            string result;
            graphs.TryGetValue(name, out result);
            return result;
        }

        public void FillFirstSheet(Excel.Application aApp)
        {
            var sheet = aApp.Sheets["1"];

            if (_graphs != null)
            {
                sheet.Cells[35, 12] = GetGraphValue(_graphs, Common.Constants.GRAPH_1);
                sheet.Cells[32, 12] = GetGraphValue(_graphs, Common.Constants.GRAPH_2);
                sheet.Cells[36, 16] = GetGraphValue(_graphs, Common.Constants.GRAPH_4);
                sheet.Cells[36, 17] = GetGraphValue(_graphs, Common.Constants.GRAPH_4a);
                sheet.Cells[36, 18] = GetGraphValue(_graphs, Common.Constants.GRAPH_4b);

                sheet.Cells[35, 8] = GetGraphValue(_graphs, Common.Constants.GRAPH_11sp_dev);
                sheet.Cells[36, 8] = GetGraphValue(_graphs, Common.Constants.GRAPH_11sp_chk);
                sheet.Cells[38, 8] = GetGraphValue(_graphs, Common.Constants.GRAPH_11norm);
                sheet.Cells[39, 8] = GetGraphValue(_graphs, Common.Constants.GRAPH_11affirm);
            }
            sheet.Cells[36, 22] = Pages;

            FillRows(sheet, 27);
        }

        public void FillSheet(Excel._Worksheet sheet)
        {
            sheet.Cells[37, 22] = sheet.Name;
            if (_graphs != null)
            {
                sheet.Cells[35, 12] = GetGraphValue(_graphs, Common.Constants.GRAPH_2);
            }

            FillRows(sheet, 33);
        }

        private void FillRows(Excel._Worksheet sheet, int maxRows)
        {
            int row = 2;
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
