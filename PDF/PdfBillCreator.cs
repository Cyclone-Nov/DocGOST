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
using iText.Layout.Properties;
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
            pdfDoc.SetDefaultPageSize(_pageSize);
            doc = new Document(pdfDoc, pdfDoc.GetDefaultPageSize().Rotate(), true);
            
           int lastProcessedRow = AddFirstPage(doc, graphs, dataTable);
        
            _currentPageNumber = 1;
            while (lastProcessedRow > 0) {
                _currentPageNumber++;
                lastProcessedRow = AddNextPage(doc, graphs, dataTable, _currentPageNumber, lastProcessedRow);
            }
        
            if (pdfDoc.GetNumberOfPages() > MAX_PAGES_WITHOUT_CHANGELIST) {
                _currentPageNumber++;
                AddRegisterList(doc, graphs, _currentPageNumber);
            }

            AddPageCountOnFirstPage(doc, _currentPageNumber);

            doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {
            SetPageMargins(aInDoc);
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));
            aInDoc.Add(CreateFirstTitleBlock(_pageSize, aGraphs, 0));
            aInDoc.Add(CreateTable(null, true, 0, out var lpr));
            DrawLines(pdfDoc.GetFirstPage());
            return lpr;
        }


        internal override int AddNextPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNumber, int aStartRow) {
            aInDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            SetPageMargins(aInDoc);

            int lastNextProcessedRow;
            var dataTable = CreateTable(aData, false, aStartRow, out lastNextProcessedRow);
            dataTable.SetFixedPosition(19.3f * PdfDefines.mmA4, BOTTOM_MARGIN + 16 * PdfDefines.mmA4, TITLE_BLOCK_WIDTH + 2f);
            aInDoc.Add(dataTable);

            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));
            aInDoc.Add(CreateNextTitleBlock(_pageSize, aGraphs, aPageNumber));
            DrawLines(pdfDoc.GetPage(2));
            return lastNextProcessedRow;
        }

        Table CreateTable(DataTable aData, bool firstPage, int aStartRow, out int outLastProcessedRow) {
            float[] columnSizes = {
                60 * PdfDefines.mmA3h, 
                45 * PdfDefines.mmA3h, 
                70 * PdfDefines.mmA3h, 
                55 * PdfDefines.mmA3h,
                70 * PdfDefines.mmA3h,
                16 * PdfDefines.mmA3h,
                16 * PdfDefines.mmA3h,
                16 * PdfDefines.mmA3h,
                16 * PdfDefines.mmA3h,
                24 * PdfDefines.mmA3h,
            };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            tbl.SetMargin(0).SetPadding(0).SetFont(f1).SetFontSize(12).SetItalic().SetTextAlignment(TextAlignment.CENTER);

            Cell CreateCell(int rowspan=1, int colspan=1) => new Cell(rowspan, colspan).SetPadding(0).SetMargin(0).SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder());

            void AddMainHeaderCell(string text) => tbl.AddCell(CreateCell(2,1).SetBorder(CreateThickBorder()).Add(new Paragraph("Наименование")));

            AddMainHeaderCell("Наименование");
            AddMainHeaderCell("Код продукции");
            AddMainHeaderCell("Обозначение документа на поставку");
            AddMainHeaderCell("Поставщик");
            AddMainHeaderCell("Куда входит (обозначение)");

            tbl.AddCell(CreateCell(1, 4).SetBorder(CreateThickBorder()).SetHeight(9*PdfDefines.mmA3).Add(new Paragraph("Количество")));
            AddMainHeaderCell("Примечание");
            
            void AddSecondaryHeaderCell(string text) => tbl.AddCell(CreateCell().SetBorder(CreateThickBorder()).SetHeight(18*PdfDefines.mmA3).Add(new Paragraph(text)));

            AddSecondaryHeaderCell("на изделие");
            AddSecondaryHeaderCell("в комплекте");
            AddSecondaryHeaderCell("на регулир");
            AddSecondaryHeaderCell("всего");


            var rowNumber = firstPage ? RowNumberOnFirstPage : RowNumberOnNextPage;
            for (int i = 0; i < rowNumber*10; ++i) {
                tbl.AddCell(CreateCell().SetHeight(5*PdfDefines.mmA3));
            }


            //tbl.SetFixedPosition(APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f, 78 * PdfDefines.mmA3, (60+45+70+55+70+16*4+24)*PdfDefines.mmA3 ) ;
            tbl.SetFixedPosition(APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f, 78 * PdfDefines.mmA3, 280*PdfDefines.mmA3h-0.5f) ;

            outLastProcessedRow = 0;
            return tbl;
        }


        void DrawLines(PdfPage aPage) {
            var pageWidth = aPage.GetPageSize().GetWidth();

            Canvas canvas = new Canvas(new PdfCanvas(aPage),
                new Rectangle(APPEND_GRAPHS_LEFT, BOTTOM_MARGIN, PdfDefines.A3Width, 2));
            canvas.Add(
                new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth((228) * PdfDefines.mmA3));

            var leftVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;
            var x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f;
            var y = BOTTOM_MARGIN;
            canvas = new Canvas(new PdfCanvas(aPage), new Rectangle(x, y, 2, leftVertLineHeight));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(leftVertLineHeight).SetRotationAngle(DegreesToRadians(90)));

            var upperHorizLineWidth = pageWidth - (x+RIGHT_MARGIN) + 4;
            y = BOTTOM_MARGIN + leftVertLineHeight;
            canvas = new Canvas(new PdfCanvas(aPage), new Rectangle(x, y, upperHorizLineWidth, 2));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(upperHorizLineWidth));

            var rightVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;//+ upperHorizLineWidth;
            x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f + upperHorizLineWidth;
            y = BOTTOM_MARGIN+2f;
            canvas = new Canvas(new PdfCanvas(aPage), new Rectangle(x, y, 2, rightVertLineHeight));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(rightVertLineHeight).SetRotationAngle(DegreesToRadians(90)));
        }
    }
}
