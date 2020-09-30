using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.DataPreparation;
using GostDOC.Interfaces;
using GostDOC.Models;
using Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    class ExportSp : IExcelExport
    {
        private PrepareManager _prepareManager = PrepareManager.Instance;
        private DocManager _docManager = DocManager.Instance;
        private ProjectWrapper _project = new ProjectWrapper();
        private int _tableRow = 0;

        public void Export(Application aApp, string aFilePath)
        {
            if (_prepareManager.PrepareDataTable(DocType.Specification, _docManager.Project.Configurations))
            {
                var tbl = _prepareManager.GetDataTable(DocType.Specification);

                var wb = aApp.Workbooks.Open(Utils.GetTemplatePath(Common.Constants.SpecificationTemplateName));

                FillFirstSheet(aApp, tbl);

                wb.SaveAs(aFilePath);
            }
        }

        public void FillFirstSheet(Application aApp, System.Data.DataTable tbl)
        {
            var firstSheet = aApp.Sheets["1"];

            int row = 2;
            while (row < 27 && _tableRow < tbl.Rows.Count)
            {
                firstSheet.Cells[row, 4] = tbl.GetTableValue(_tableRow, 1);
                firstSheet.Cells[row, 6] = tbl.GetTableValue(_tableRow, 2);
                firstSheet.Cells[row, 7] = tbl.GetTableValue(_tableRow, 3);
                firstSheet.Cells[row, 9] = tbl.GetTableValue(_tableRow, 4);
                firstSheet.Cells[row, 14] = tbl.GetTableValue(_tableRow, 5);
                firstSheet.Cells[row, 20] = tbl.GetTableValue<int>(_tableRow, 6);
                firstSheet.Cells[row, 22] = tbl.GetTableValue(_tableRow, 7);

                row++;
                _tableRow++;
            }
        }
    }
}
