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
    class ExportD27 : IExcelExport
    {
        private DocManager _docManager = DocManager.Instance;
        private int _nextColor = 15;

        public void Export(Excel.Application aApp, string aFilePath)
        {
            Excel._Worksheet ws = aApp.ActiveSheet;

            bool first = true;
            foreach (var cfg in _docManager.Project.Configurations)
            {
                if (first)
                {
                    first = false;
                }
                else
                { 
                    ws = aApp.Sheets.Add(After: aApp.Sheets[aApp.Sheets.Count]);
                }
                // Set sheet name
                ws.Name = cfg.Key;
                // Get max level
                int maxLevelHeight = GetMaxLevelHeight(cfg.Value.D27, 1);
                // Set common fields
                ws.Cells[1, 1] = "Наименование";
                ws.MergeRange(1, 1, maxLevelHeight, 1, 2);

                ws.Cells[maxLevelHeight + 1, 1] = "Сборочные единицы";
                ws.Cells[maxLevelHeight + 1, 1].Interior.ColorIndex = _nextColor;
                ws.Cells[maxLevelHeight + 1, 1].Font.Bold = true;
                ws.Cells[maxLevelHeight + 1, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                ws.Cells[maxLevelHeight, 2] = "Ед. измерения";
                ws.Cells[maxLevelHeight, 2].Orientation = 90;
                ws.MergeRange(1, 2, maxLevelHeight, 2, _nextColor);                

                // Process main group
                ProcessGroup(ws, 1, 3, maxLevelHeight + 2, cfg.Value.D27);
            }

            ws.Columns.AutoFit();
            ws.Rows.AutoFit();
        }

        private int ProcessGroup(Excel._Worksheet aWs, int aHeaderRow, int aHeaderColumn, int aDataRow, Group aGroup)
        {
            _nextColor++;

            int dataRow = aDataRow;
            foreach (var cmp in aGroup.Components)
            {
                aWs.Cells[dataRow, 1] = cmp.GetProperty(Constants.ComponentName);
                aWs.Cells[dataRow, aHeaderColumn] = cmp.Count;
                dataRow++;
            }

            int maxLevelHeight = GetMaxLevelHeight(aGroup, 1);
            if (aGroup.SubGroups != null)
            {
                // Set name
                aWs.Cells[aHeaderRow, aHeaderColumn] = aGroup.Name;
                // horizontal
                aWs.MergeRange(aHeaderRow, aHeaderColumn, aHeaderRow, aHeaderColumn + aGroup.SubGroups.Count, _nextColor);
                aWs.GroupRange(aHeaderRow, aHeaderColumn + 1, aHeaderRow, aHeaderColumn + aGroup.SubGroups.Count);
                // vertical
                aWs.MergeRange(aHeaderRow + 1, aHeaderColumn, aHeaderRow + maxLevelHeight - 1, aHeaderColumn, _nextColor);

                int row = aHeaderRow + 1;
                int col = aHeaderColumn + 1;
                foreach (var kvp in aGroup.SubGroups)
                {
                    // Recursevly process subgroups
                    dataRow = ProcessGroup(aWs, row, col, dataRow, kvp.Value);
                    col++;
                }
            }
            else
            {
                // Set name
                aWs.Cells[aHeaderRow + maxLevelHeight - 1, aHeaderColumn] = aGroup.Name;
                // vertical only
                var range = aWs.MergeRange(aHeaderRow, aHeaderColumn, aHeaderRow + maxLevelHeight - 1, aHeaderColumn, _nextColor);
                range.Orientation = 90;
            }
            return dataRow;
        }

        private int GetMaxLevelHeight(Group aGroup, int aLevel)
        {
            int max = aLevel;
            if (aGroup.SubGroups != null)
            {
                max++;
                foreach (var gp in aGroup.SubGroups)
                {
                    max = Math.Max(GetMaxLevelHeight(gp.Value, max), max);
                }
            }
            return max;
        }
    }
}
