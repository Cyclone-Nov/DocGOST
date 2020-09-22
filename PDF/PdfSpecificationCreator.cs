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
    class PdfSpecificationCreator : PdfCreator
    {
        public PdfSpecificationCreator() : base(DocType.Specification) {
        }

        public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs) 
        {
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
            doc = new iText.Layout.Document(pdfDoc, pdfDoc.GetDefaultPageSize(), true);
            
            var dataTable = aData;

            
            AddFirstPage(doc, aMainGraphs, dataTable);
            _currentPageNumber = 1;
            _currentPageNumber++;
            AddNextPage(doc, aMainGraphs, dataTable, _currentPageNumber, 0);

            AddPageCountOnFirstPage(doc, _currentPageNumber);

            doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {

            SetPageMargins(aInDoc);

            var mainTable = CreateMainTable(aGraphs, aData, true);
            mainTable.SetFixedPosition(19.3f * mmW(), 79 * mmH()+5.02f, TITLE_BLOCK_WIDTH-0.02f);
            aInDoc.Add(mainTable);
            
            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, Pages = 2}));

            // добавить таблицу с верхней дополнительной графой
            aInDoc.Add(CreateTopAppendGraph(_pageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawMissingLinesFirstPage();

            return 0;
        }

        internal override int AddNextPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNamuber, int aLastProcessedRow) {
            aInDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            SetPageMargins(aInDoc);

            var mainTable = CreateMainTable(aGraphs, aData, false);
            mainTable.SetFixedPosition(19.3f * mmW(), 37 * mmW()+5f, TITLE_BLOCK_WIDTH+2f);
            aInDoc.Add(mainTable);
            
            // добавить таблицу с основной надписью 
            aInDoc.Add(CreateNextTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, CurrentPage = 2}));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawMissingLinesNextPage(2);

            return 0;
        }

        Table CreateMainTable(IDictionary<string, string> aGraphs, DataTable aData, bool firstPage) {
            float[] columnSizes = { 
                6  * mmW(), 
                6  * mmW(), 
                8  * mmW(), 
                70 * mmW(), 
                63 * mmW(), 
                10 * mmW(), 
                22 * mmW()};
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            tbl.SetMargin(0).SetPadding(0);

            Cell CreateCell() => new Cell().SetPadding(0).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            Paragraph CreateParagraph(string text) => new Paragraph(text).SetFont(f1).SetItalic().SetTextAlignment(TextAlignment.CENTER);

            Table AddHeaderCell90(string text) => tbl.AddCell(CreateCell().SetBorder(CreateThickBorder()).SetHeight(15*mmH()).Add(CreateParagraph(text).SetRotationAngle(DegreesToRadians(90))));
            Table AddHeaderCell(string text) => tbl.AddCell(CreateCell().SetBorder(CreateThickBorder()).SetHeight(15*mmH()).Add(CreateParagraph(text).SetFixedLeading(11)));

            AddHeaderCell90("Формат");
            AddHeaderCell90("Зона");
            AddHeaderCell90("Поз.");
            AddHeaderCell("Обозначение");
            AddHeaderCell("Наименование");
            AddHeaderCell90("Кол.");
            AddHeaderCell("Приме-\nчание");

            var rowsNumber = firstPage ? RowNumberOnFirstPage : RowNumberOnNextPage;
            for (int i = 0; i < (rowsNumber-1) * 7; ++i) {
                tbl.AddCell(new Cell().SetHeight(8*mmH()).SetPadding(0).SetBorderLeft(CreateThickBorder())).SetBorderRight(CreateThickBorder());
            }
            for (int i = 0; i < 7; ++i) {
                tbl.AddCell(new Cell().
                        SetHeight(8 * mmH()).
                        SetPadding(0).
                        SetBorderLeft(CreateThickBorder()).
                        SetBorderRight(CreateThickBorder()).
                        SetBorderBottom(CreateThickBorder()));
            }


            return tbl;
        }

        private void SetPageMargins(iText.Layout.Document aDoc) {
            aDoc.SetLeftMargin(8 * mmW());
            aDoc.SetRightMargin(5 * mmW());
            aDoc.SetTopMargin(5 * mmW());
            aDoc.SetBottomMargin(5 * mmW());
        }

        private void DrawMissingLinesFirstPage() {
            // нарисовать недостающую линию
            var fromLeft = 19 * mmW() -1.17f /*+1.65f*/ + TITLE_BLOCK_WIDTH;
            Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()),
                new Rectangle(fromLeft, BOTTOM_MARGIN + TITLE_BLOCK_FIRST_PAGE_WITHOUT_APPEND_HEIGHT_MM * mmH() -2f, 2, 120));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(120)
                .SetRotationAngle(DegreesToRadians(90)));

        }
        private void DrawMissingLinesNextPage(int pageNumber) {
            // нарисовать недостающую линию
            var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH;
            Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetPage(pageNumber)),
                new Rectangle(fromLeft, BOTTOM_MARGIN + (8+7) * mmW()-6f, 2, 60));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(50)
                .SetRotationAngle(DegreesToRadians(90)));

        }

    }
}
