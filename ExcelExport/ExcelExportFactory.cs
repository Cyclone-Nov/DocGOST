using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Interfaces;

namespace GostDOC.ExcelExport
{
    static class ExcelExportFactory
    {
        public static IExcelExport GetExporter(DocType aDocType)
        {
            switch (aDocType)
            {
                case DocType.D27:
                    return new ExportD27();
                case DocType.Bill:
                    return new ExportBill();
                case DocType.Specification:
                    return new ExportSp();
            }
            return null;
        }
    }
}
