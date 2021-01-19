using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace GostDOC.Interfaces
{
    interface IExcelExport
    {
        void Export(Excel.Application aApp, string aFilePath);
    }
}
