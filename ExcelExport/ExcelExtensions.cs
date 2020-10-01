using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.DataPreparation;
using GostDOC.Models;
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

        public static void SetBoldAlignedText(this Excel._Worksheet ws, int r1, int c1, string txt)
        {
            ws.Cells[r1, c1] = txt;
            ws.Cells[r1, c1].Font.Bold = true;
            ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            ws.Cells[r1, c1].VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
        }
    }

    static class DataTableExtensions
    {
        public static T GetTableValue<T>(this DataTable tbl, int row, int col)
        {
            var val = tbl.Rows[row].ItemArray[col];
            if (val != System.DBNull.Value)
            {
                return (T)val;
            }
            return default(T);
        }

        public static string GetTableValue(this DataTable tbl, int row, int col)
        {
            var val = tbl.Rows[row].ItemArray[col];
            if (val != System.DBNull.Value)
            {
                return ((BasePreparer.FormattedString)val).Value;
            }
            return string.Empty;
        }

        public static DataTable GetDataTable(DocType aDocType)
        {
            if (PrepareManager.Instance.PrepareDataTable(aDocType, DocManager.Instance.Project.Configurations))
            {
                return PrepareManager.Instance.GetDataTable(aDocType);
            }
            return null;
        }
    }

    static class ExcelUtils
    {
        public static IDictionary<string, string> GetGraphs()
        {
            foreach (var cfg in DocManager.Instance.Project.Configurations)
            {
                if (cfg.Value.Graphs.Count > 0)
                {
                    return cfg.Value.Graphs;                    
                }
            }
            return null;
        }
    }
}
