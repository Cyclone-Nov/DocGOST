using System.Data;
using GostDOC.Common;
using GostDOC.DataPreparation;
using GostDOC.Models;

namespace GostDOC.ExcelExport
{
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
}
