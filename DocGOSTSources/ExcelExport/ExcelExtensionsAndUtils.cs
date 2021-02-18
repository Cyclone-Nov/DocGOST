using System.Collections.Generic;
using GostDOC.DataPreparation;
using GostDOC.Models;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.ExcelExport
{
    static class ExcelExtensions
    {
        /// <summary>
        /// максимальное количество цветов в палитре Workbook Excel по умолчанию, если палитру не меняли
        /// </summary>
        public static int MAX_EXCEL_PALETTE_COLORS = 56;

        public static dynamic MergeRange(this Excel._Worksheet ws, int r1, int c1, int r2, int c2, int col)
        {
            Excel.Range range = ws.Range[ws.Cells[r1, c1], ws.Cells[r2, c2]];
            range.Merge();
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            range.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            range.Interior.ColorIndex = col % MAX_EXCEL_PALETTE_COLORS;
            range.Font.Bold = true;
            return range;            
        }

        public static dynamic GroupRange(this Excel._Worksheet ws, int r1, int c1, int r2, int c2)
        {
            Excel.Range range = ws.Range[ws.Cells[r1, c1], ws.Cells[r2, c2]];       
            range.Group();
            range.WrapText = true;
            return range;
        }

        public static void SetBoldAlignedText(this Excel._Worksheet ws, int r1, int c1, string txt)
        {
            ws.Cells[r1, c1] = txt;
            ws.Cells[r1, c1].Font.Bold = true;
            ws.Cells[r1, c1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            ws.Cells[r1, c1].VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
        }

        public static int SetFormattedValue(this Excel._Worksheet ws, int r1, int c1, BasePreparer.FormattedString txt)
        {
            var cell = (Excel.Range)ws.Cells[r1, c1];
            int merge_rows_cnt = ((Excel.Range)((Excel.Range)cell.MergeArea).Rows).Count;
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
            return merge_rows_cnt;
        }      
        
        public static dynamic FormatLitera(this Excel.Range range, string txt)
        {
            range.Value2 = txt;
            if (!string.IsNullOrEmpty(txt))
            {
                for (int i = 0; i < txt.Length; i++)
                {
                    if (char.IsDigit(txt[i]))
                    {
                        Excel.Characters ch = range.Characters[i + 1, 1];
                        ch.Font.Size = ch.Font.Size - 3;
                    }
                }
            }
            return range;
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
