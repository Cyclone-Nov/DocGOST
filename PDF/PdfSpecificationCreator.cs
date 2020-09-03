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

        public override void Create(Project project) {
            if (project.Configurations.Count == 0)
                return;
                        
            Configuration mainConfig = null;
            if (!project.Configurations.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
                return;
                        
            //var dataTable = CreateDataTable(mainConfig.Specification);

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
            doc = new iText.Layout.Document(pdfDoc, pdfDoc.GetDefaultPageSize(), true);
            
            DataTable dataTable = new DataTable();
            int lastProcessedRow = AddFirstPage(doc, mainConfig.Graphs, dataTable);

            doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {

            SetPageMargins(aInDoc);

            var table = CreateMainTable(aGraphs, aData);
            aInDoc.Add(table);
            
            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(PageSize, aGraphs, 0));

            // добавить таблицу с верхней дополнительной графой
            aInDoc.Add(CreateTopAppendGraph(PageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));

            DrawMissingLinesFirstPage();

            return 0;
        }

        internal override int AddNextPage(Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aLastProcessedRow) {
            throw new NotImplementedException();
        }

        Table CreateMainTable(IDictionary<string, string> aGraphs, DataTable aData) {
            float[] columnSizes = { 
                6  * PdfDefines.mmA4, 
                6  * PdfDefines.mmA4, 
                8  * PdfDefines.mmA4, 
                70 * PdfDefines.mmA4, 
                63 * PdfDefines.mmA4, 
                10 * PdfDefines.mmA4, 
                22 * PdfDefines.mmA4};
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            tbl.SetMargin(0).SetPadding(0);

            Cell CreateCell() => new Cell().SetPadding(0).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            Paragraph CreateParagraph(string text) => new Paragraph(text).SetFont(f1).SetItalic().SetTextAlignment(TextAlignment.CENTER);

            Table AddHeaderCell90(string text) => tbl.AddCell(CreateCell().SetBorder(CreateThickBorder()).SetHeight(15*PdfDefines.mmA4h).Add(CreateParagraph(text).SetRotationAngle(DegreesToRadians(90))));
            Table AddHeaderCell(string text) => tbl.AddCell(CreateCell().SetBorder(CreateThickBorder()).SetHeight(15*PdfDefines.mmA4h).Add(CreateParagraph(text).SetFixedLeading(11)));

            AddHeaderCell90("Формат");
            AddHeaderCell90("Зона");
            AddHeaderCell90("Поз.");
            AddHeaderCell("Обозначение");
            AddHeaderCell("Наименование");
            AddHeaderCell90("Кол.");
            AddHeaderCell("Приме-\nчание");

            for (int i = 0; i < 23 * 7; ++i) {
                tbl.AddCell(new Cell().SetHeight(8*PdfDefines.mmA4h).SetPadding(0).SetBorderLeft(CreateThickBorder())).SetBorderRight(CreateThickBorder());
            }
            for (int i = 0; i < 7; ++i) {
                tbl.AddCell(new Cell().
                        SetHeight(8 * PdfDefines.mmA4h).
                        SetPadding(0).
                        SetBorderLeft(CreateThickBorder()).
                        SetBorderRight(CreateThickBorder()).
                        SetBorderBottom(CreateThickBorder()));
            }

            tbl.SetFixedPosition(19.3f * PdfDefines.mmA4, 79 * PdfDefines.mmA4+5f, TITLE_BLOCK_WIDTH+2f);

            return tbl;
        }


        private void SetPageMargins(iText.Layout.Document aDoc)
        {
            aDoc.SetLeftMargin(8 * PdfDefines.mmA4);
            aDoc.SetRightMargin(5 * PdfDefines.mmA4);
            aDoc.SetTopMargin(5 * PdfDefines.mmA4);
            aDoc.SetBottomMargin(5 * PdfDefines.mmA4);
        }

        private void DrawMissingLinesFirstPage() {

            // нарисовать недостающую линию
            var fromLeft = 19.3f * PdfDefines.mmA4 + TITLE_BLOCK_WIDTH;
            Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()),
                new Rectangle(fromLeft, BOTTOM_MARGIN + (15 + 5 + 5 + 15 + 8 + 14) * PdfDefines.mmA4 + 10f, 2, 30));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(20)
                .SetRotationAngle(DegreesToRadians(90)));

        }
        private void DrawMissingLinesNextPage() {

            // нарисовать недостающую линию
            var fromLeft = 19.3f * PdfDefines.mmA4 + TITLE_BLOCK_WIDTH;
            Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()),
                new Rectangle(fromLeft, BOTTOM_MARGIN + (15 + 5 + 5 + 15 + 8 + 14) * PdfDefines.mmA4 + 2f, 2, 30));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(15)
                .SetRotationAngle(DegreesToRadians(90)));

        }

    }
}
