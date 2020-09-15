using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using Document = iText.Layout.Document;

namespace GostDOC.PDF
{
    class PdfBillCreator : PdfCreator
    {
        protected new readonly float LEFT_MARGIN = 5 * PdfDefines.mmA3;
        protected new readonly float RIGHT_MARGIN = 5 * PdfDefines.mmA3;

        public PdfBillCreator() : base(DocType.Bill) {
        }

        public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs) 
        {
            var dataTable = aData;
            var graphs = aMainGraphs;

            if (pdfWriter != null)
            {
                doc.Close();
                doc = null;
                pdfDoc.Close();
                pdfDoc = null;
                pdfWriter.Close();
                pdfWriter.Dispose();
                pdfWriter = null;
                MainStream.Dispose();
                MainStream = null;                
            }

            f1 = PdfFontFactory.CreateFont(@"Font\\GOST_TYPE_A.ttf", "cp1251", true);
            MainStream = new MemoryStream();
            pdfWriter = new PdfWriter(MainStream);
            pdfDoc = new PdfDocument(pdfWriter);
            pdfDoc.SetDefaultPageSize(PageSize);
            doc = new Document(pdfDoc, pdfDoc.GetDefaultPageSize().Rotate(), true);
            
            AddFirstPage(doc, graphs, dataTable);
            AddNextPage(doc, graphs, dataTable, 0);

            doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {
            
            SetPageMargins(aInDoc);

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));

            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(PageSize, aGraphs, 0));


            DrawLines();


            return 0;
        }

        void DrawLines() {
            var pageWidth = pdfDoc.GetFirstPage().GetPageSize().GetWidth();
            var pageHeight = pdfDoc.GetFirstPage().GetPageSize().GetHeight();

            Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()),
                new Rectangle(APPEND_GRAPHS_LEFT, BOTTOM_MARGIN, PdfDefines.A3Width, 2));
            canvas.Add(
                new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth((228) * PdfDefines.mmA3));

            var leftVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;
            var x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f;
            var y = BOTTOM_MARGIN;
            canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()), new Rectangle(x, y, 2, leftVertLineHeight));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(leftVertLineHeight).SetRotationAngle(DegreesToRadians(90)));

            var upperHorizLineWidth = pageWidth - (x+RIGHT_MARGIN) + 4;
            y = BOTTOM_MARGIN + leftVertLineHeight;
            canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()), new Rectangle(x, y, upperHorizLineWidth, 2));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(upperHorizLineWidth));

            var rightVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;//+ upperHorizLineWidth;
            x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f + upperHorizLineWidth;
            y = BOTTOM_MARGIN+2f;
            canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()), new Rectangle(x, y, 2, rightVertLineHeight));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(rightVertLineHeight).SetRotationAngle(DegreesToRadians(90)));
        }

        internal override int AddNextPage(Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aLastProcessedRow) {
            return 0;
        }
    }
}
