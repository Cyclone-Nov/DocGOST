using System;
using System.Data;
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
using GostDOC.Models;
using System.IO;

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

        internal readonly PageSize PageSize;

        /// <summary>
        /// поток, содержащий PDF документ
        /// </summary>
        /// <value>
        /// The main stream.
        /// </value>
        protected MemoryStream MainStream { get; } = new MemoryStream();

        /// <summary>
        /// The PDF document
        /// </summary>
        protected PdfDocument pdfDoc;

        /// <summary>
        /// The document
        /// </summary>
        protected iText.Layout.Document doc;

        /// <summary>
        /// The PDF writer
        /// </summary>
        protected PdfWriter pdfWriter;



        public PdfCreator(DocType aType)
        {            
            Type = aType;           

            f1 = PdfFontFactory.CreateFont(@"Font\\GOST_TYPE_A.ttf", "cp1251", true);
            switch(aType)
            {
                case DocType.Bill:
                    PageSize = new PageSize(PageSize.A3);
                    break;
                case DocType.D27:
                    PageSize = new PageSize(PageSize.A3);
                    break;
                case DocType.ItemsList:
                    PageSize = new PageSize(PageSize.A4);
                    break;
                case DocType.Specification:
                    PageSize = new PageSize(PageSize.A4);
                    break;
                default:
                    PageSize = new PageSize(PageSize.A4);
                    break;
            }

            pdfWriter = new PdfWriter(MainStream);
            pdfDoc = new PdfDocument(pdfWriter);
            pdfDoc.SetDefaultPageSize(PageSize);
            doc = new iText.Layout.Document(pdfDoc, pdfDoc.GetDefaultPageSize(), false);

        }


        public abstract void Create(Project project);

        public byte[] GetData()
        {
            doc.Flush();
            pdfWriter.Flush();
            return MainStream.ToArray();
        }

        

        /// <summary>
        /// добавить к документу лист регистрации изменений
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        internal void AddRegisterList(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs)
        {

        }

        /// <summary>
        /// добавить к документу первую страницу
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal abstract int AddFirstPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData);

        /// <summary>
        /// добавить к документу последующие страницы
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal abstract int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData);


        public static double DegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }


    }
}
