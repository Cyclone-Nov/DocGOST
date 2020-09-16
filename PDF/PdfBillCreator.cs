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
            pdfDoc.SetDefaultPageSize(_pageSize);
            doc = new Document(pdfDoc, pdfDoc.GetDefaultPageSize().Rotate(), true);
            
            AddFirstPage(doc, graphs, dataTable);
            _currentPageNumber = 1;
            _currentPageNumber++;
            AddNextPage(doc, graphs, dataTable, _currentPageNumber, 0);

            doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {
            
            SetPageMargins(aInDoc);

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(_pageSize, aGraphs, 0));


            Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()),
                new Rectangle(APPEND_GRAPHS_LEFT, BOTTOM_MARGIN, PdfDefines.A3Width, 2));
            canvas.Add(
                new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth((228) * PdfDefines.mmA4));


            return 0;
        }

        internal override int AddNextPage(Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNamuber, int aLastProcessedRow) {
            return 0;
        }
    }
}
