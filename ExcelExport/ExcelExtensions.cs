using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    static class ExcelExtensions
    {
        public static dynamic MergeRange(this Excel._Worksheet ws, int r1, int c1, int r2, int c2, int col)
        {
            var range = ws.Range[ws.Cells[r1, c1], ws.Cells[r2, c2]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            range.Interior.ColorIndex = col;
            range.Font.Bold = true;
            return range;            
        }

        public static dynamic GroupRange(this Excel._Worksheet ws, int r1, int c1, int r2, int c2)
        {
            var range = ws.Range[ws.Cells[r1, c1], ws.Cells[r2, c2]];
            range.Group();
            return range;
        }
    }
}
