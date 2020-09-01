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
        /// количетсов строк в таблице данных на первой странице документа
        /// </summary>
        protected readonly int CountStringsOnFirstPage;
        /// <summary>
        /// количетсов строк в таблице данных на остальных страницах документа
        /// </summary>
        protected readonly int CountStringsOnNextPage;

        /// <summary>
        /// количество строк в таблице данных на листе регистрации изменений
        /// </summary>
        protected readonly int CountStringsOnChangelist = 31;

        /// <summary>
        /// поток, содержащий PDF документ
        /// </summary>
        /// <value>
        /// The main stream.
        /// </value>
        protected MemoryStream MainStream;

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
            switch(aType)
            {
                case DocType.Bill:
                case DocType.D27:
                    {
                        PageSize = new PageSize(PageSize.A3);
                        CountStringsOnFirstPage = 24;
                        CountStringsOnNextPage = 29;
                    }
                    break;
                case DocType.Specification:
                case DocType.ItemsList:
                    {
                        PageSize = new PageSize(PageSize.A4);
                        CountStringsOnFirstPage = 24;
                        CountStringsOnNextPage = 31;
                    }
                    break;                
                default:
                    {
                        PageSize = new PageSize(PageSize.A4);
                        CountStringsOnFirstPage = 26;
                        CountStringsOnNextPage = 33;
                    }
                    break;
            }
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
        internal abstract int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aLastProcessedRow);


        public static double DegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }


    }
}
