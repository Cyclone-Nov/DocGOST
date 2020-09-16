using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Geom;


namespace GostDOC.PDF
{
    public static class PdfDefines
    {

        public static float A4Width = PageSize.A4.GetWidth();
        public static float A4Height = PageSize.A4.GetHeight();
        public static float A3Width = PageSize.A3.GetWidth();
        public static float A3Height = PageSize.A3.GetHeight();
        public static readonly float mmA4 = PageSize.A4.GetWidth() / 210;
        public static readonly float mmA4h = PageSize.A4.GetHeight() / 297;
        public static readonly float mmA3 = PageSize.A3.GetWidth() / 210;
        public static readonly float mmA3h = PageSize.A3.GetHeight() / 297;
        public static readonly float ROW_HEIGHT = 6 * mmA4;
        public static readonly float INNER_TABLE_ROW_HEIGHT = ROW_HEIGHT * 0.6f;
    }
}
