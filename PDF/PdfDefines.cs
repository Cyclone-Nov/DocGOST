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
        public static readonly float mmA4 = PageSize.A4.GetWidth() / 210;
        public static readonly float ROW_HEIGHT = 6 * mmA4;
        public static readonly float INNER_TABLE_ROW_HEIGHT = ROW_HEIGHT * 0.6f;
    }
}
