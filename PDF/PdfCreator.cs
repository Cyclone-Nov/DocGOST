using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Org.BouncyCastle.Asn1.Crmf;

using GostDOC.Common;

namespace GostDOC.PDF
{
    public abstract class PdfCreator
    {
        /// <summary>
        /// The save path
        /// </summary>
        public readonly string SavePath;

        public readonly DocType Type;

        internal static PdfFont f1;

        public PdfCreator(string aSavePath, DocType aType)
        {
            SavePath = aSavePath;
            Type = aType;
            f1 = PdfFontFactory.CreateFont("GOST_TYPE_A.ttf", "cp1251", true);
        }


        public abstract void Create();

        /// <summary>
        /// добавить к документу лист регистрации изменений
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        internal void AddRegisterList(PdfDocument aInPdfDoc)
        {

        }

        /// <summary>
        /// добавить к документу первую страницу
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal abstract bool AddFirstPage(PdfDocument aInPdfDoc);

        /// <summary>
        /// добавить к документу последующие страницы
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal abstract bool AddNextPage(PdfDocument aInPdfDoc);


        public static double DegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }


    }
}
