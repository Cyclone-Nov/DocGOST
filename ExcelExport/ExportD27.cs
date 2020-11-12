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
        private const int FirstColor = 15;

        private DocManager _docManager = DocManager.Instance;

        private int _nextColor;
        private List<ComponentGroupD27> _components = new List<ComponentGroupD27>();

        private int _complexColumn = 0;
        private int _complexRow = 0;

        public ExportD27()
        {
            _components.Add(new ComponentGroupD27(Constants.GroupDetails));
            _components.Add(new ComponentGroupD27(Constants.GroupStandard));
            _components.Add(new ComponentGroupD27(Constants.GroupOthers));
            _components.Add(new ComponentGroupD27(Constants.GroupMaterials));
        }

        public void Export(Excel.Application aApp, string aFilePath)
        {
            // Create workbook
            Excel.Workbook wb = aApp.Workbooks.Add();

            Excel._Worksheet ws = aApp.ActiveSheet;
            Excel._Worksheet firstSheet = aApp.ActiveSheet;

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
                // Set common fields
                ws.Cells[1, 1] = "Наименование";
                // Reset sheet counters
                Reset();
                // Process D27
                Process(ws, cfg.Value.D27);
                // Auto fit sheet
                ws.Columns.AutoFit();
                ws.Rows.AutoFit();                
            }

            firstSheet.Select();
            // Save
            wb.SaveAs(aFilePath);
            // Close
            wb.Close(false);
        }

        private void Reset()
        {
            _nextColor = FirstColor;
            _complexColumn = 2;
            _complexRow = 1;

            foreach (var gp in _components)
            {
                gp.Components.Clear();
            }
        }

        private void Process(Excel._Worksheet aWs, Group aGroup)
        {
            ComplexD27 complex = new ComplexD27();
            // Prepare print structure
            ProcessGroup(complex, aGroup);
            // Print headers
            Print(aWs, complex);

            // Sum
            int lastCol = complex.Size.Width + 2;
            aWs.Cells[1, lastCol] = "Итого";
            var range = aWs.MergeRange(1, lastCol, complex.Size.Height, lastCol, 2);
            range.Orientation = 90;

            // Supplier
            int suplier = complex.Size.Width + 3;
            aWs.Cells[1, suplier] = "Поставщик";
            var rangeSup = aWs.MergeRange(1, suplier, complex.Size.Height, suplier, 2);
            rangeSup.Orientation = 90;

            // Print components
            int row = complex.Size.Height;
            foreach (var gp in _components)
            {
                if (gp.Components.Count == 0)
                {
                    continue;
                }

                aWs.SetBoldAlignedText(++row, 1, gp.Name);

                string lastName = string.Empty;
                foreach (var component in gp.Components.OrderBy(x => x.Name))
                {
                    if (lastName != component.Name)
                    {
                        row++;
                        lastName = component.Name;
                    }

                    aWs.Cells[row, 1] = component.Name;
                    aWs.Cells[row, component.Column] = component.Count;
                    aWs.Cells[row, lastCol] = $"=SUM(R{row}C{2}:R{row}C{lastCol - 1})";
                    aWs.Cells[row, suplier] = component.Manufacturer;
                }
            }
            // Merge general name cells
            aWs.MergeRange(1, 1, complex.Size.Height, 1, 2);
        }

        private void Print(Excel._Worksheet aWs, ComplexD27 aSrc)
        {
            _nextColor++;
            if (aSrc.SubComplex?.Count > 0)
            {
                // Set name
                aWs.Cells[aSrc.Row, aSrc.Column] = aSrc.Name;
                // horizontal
                aWs.MergeRange(aSrc.Row, aSrc.Column, aSrc.Row, aSrc.Column + aSrc.Size.Width - 1, _nextColor);
                aWs.GroupRange(aSrc.Row, aSrc.Column + 1, aSrc.Row, aSrc.Column + aSrc.Size.Width - 1);
                // vertical
                aWs.MergeRange(aSrc.Row + 1, aSrc.Column, aSrc.Row + aSrc.Size.Height - 1, aSrc.Column, _nextColor);

                foreach (var complex in aSrc.SubComplex)
                {
                    Print(aWs, complex);
                }
            }
            else
            {
                // Set name
                aWs.Cells[aSrc.Row, aSrc.Column] = aSrc.Name;
                // vertical only
                var range = aWs.MergeRange(aSrc.Row, aSrc.Column, aSrc.Row + aSrc.Size.Height - 1, aSrc.Column, _nextColor);
                range.Orientation = 90;
            }            
        }

        private HeaderSize ProcessGroup(ComplexD27 aDst, Group aSrc)
        {
            aDst.Name = aSrc.Name;
            aDst.Column = _complexColumn;
            aDst.Row = _complexRow;

            foreach (var cmp in aSrc.Components)
            {
                string name = cmp.GetProperty(Constants.ComponentName);
                string doc = cmp.GetProperty(Constants.ComponentDoc);
                string type = cmp.GetProperty(Constants.ComponentType);
                string group = cmp.GetProperty(Constants.SubGroupNameSp);

                // Add component type (radio details)
                if (!string.IsNullOrEmpty(group))
                {
                    string[] split = group.Split(new char[] { '\\' });
                    if (split.Length > 1)
                    {
                        name = split[0] + " " + name;
                    }
                }

                // Add component type (details)
                if (!string.IsNullOrEmpty(type) && !name.Contains(type))
                {
                    name = type + " " + name;
                }

                // Add document
                if (!string.IsNullOrEmpty(doc) && !name.Contains(doc))
                {
                    name += " " + doc;
                }

                ComponentD27 component = new ComponentD27()
                {
                    Name = name,
                    Count = cmp.Count,
                    Column = _complexColumn,
                    Manufacturer = cmp.GetProperty(Constants.ComponentSupplier)
                };

                AddComponent(cmp.GetProperty(Constants.GroupNameSp), component);
            }

            _complexColumn++;
       
            if (aSrc.SubGroups?.Count > 0) 
            {
                // Increment level
                _complexRow++;
                // Create sub complex list
                aDst.SubComplex = new List<ComplexD27>();

                int height = 1;
                foreach (var gp in aSrc.SubGroups)
                {
                    ComplexD27 newComplex = new ComplexD27();
                    var size = ProcessGroup(newComplex, gp.Value);
                    height = Math.Max(height, size.Height);
                    aDst.Size.Width += size.Width;
                    aDst.SubComplex.Add(newComplex);
                }
                // Decrement level
                _complexRow--;

                aDst.Size.Height += height;
            }

            return aDst.Size;
        }

        private void AddComponent(string aGroupName, ComponentD27 aComponent)
        {
            if (!string.IsNullOrEmpty(aGroupName))
            {
                foreach (var gp in _components)
                {
                    if (gp.Name == aGroupName)
                    {
                        // add component
                        gp.Components.Add(aComponent);
                        break;
                    }
                }
            }
        }
    }
}
