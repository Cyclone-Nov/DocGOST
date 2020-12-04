using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

        public static void SetFormattedValue(this Excel._Worksheet ws, int r1, int c1, BasePreparer.FormattedString txt)
        {
            if (txt != null)
            {
                if (txt.IsOverlined)
                {
                    ws.Cells[r1, c1] = "\u035E" + txt.Value;
                }
                else
                {
                    ws.Cells[r1, c1] = txt.Value;
                }

                ws.Cells[r1, c1].Font.Bold = txt.IsBold;

                if (txt.IsUnderlined)
                {
                    ws.Cells[r1, c1].Font.Underline = Excel.XlUnderlineStyle.xlUnderlineStyleSingle;
                }

                switch (txt.TextAlignment)
                {
                    case iText.Layout.Properties.TextAlignment.CENTER:
                        ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        break;
                    case iText.Layout.Properties.TextAlignment.LEFT:
                        ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                        break;
                    case iText.Layout.Properties.TextAlignment.RIGHT:
                        ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                        break;
                    case iText.Layout.Properties.TextAlignment.JUSTIFIED:
                        ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignJustify;
                        break;
                    case iText.Layout.Properties.TextAlignment.JUSTIFIED_ALL:
                        ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignFill;
                        break;
                }
            }
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
                return val.ToString();
            }
            return string.Empty;
        }

        public static BasePreparer.FormattedString GetTableValueFS(this DataTable tbl, int row, int col)
        {
            var val = tbl.Rows[row].ItemArray[col];
            if (val != System.DBNull.Value)
            {
                return val as BasePreparer.FormattedString;
            }
            return null;
        }        
    }


    static class DataTableUtils
    {
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
