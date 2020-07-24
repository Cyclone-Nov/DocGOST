using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace PdfExample
{
    class Program
    {

        public static double DegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static readonly string DEST = "simple_table.pdf";
        static void Main(string[] args)
        {
            FileInfo file = new FileInfo(DEST);
            file.Directory.Create();

            //new SimpleTable().ManipulatePdf(DEST);
            ManipulatePdf(DEST); 
        }

        private static void ManipulatePdf(String dest)
        {
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(dest));
            Document doc = new Document(pdfDoc);
            pdfDoc.SetDefaultPageSize(PageSize.A4);
            var mmA4 = PageSize.A4.GetWidth() / 210;
            
            doc.SetLeftMargin(9 * mmA4);
            doc.SetRightMargin(9 * mmA4);
            doc.SetTopMargin(5 * mmA4);
            doc.SetBottomMargin(5 * mmA4);

            var A4Width = PageSize.A4.GetWidth();
            var A4Height = PageSize.A4.GetHeight();

            // var page = pdfDoc.AddNewPage();
            // var pdfCanvas = new PdfCanvas(page);
            // Rectangle rectangle = new Rectangle(mmA4* 10, mmA4 * 10, A4Width, mmA4 * 40);
            // Canvas canvas = new Canvas(pdfCanvas, rectangle);
            // canvas.Add(table);
            // PdfFont f1 = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN, true);

            PdfFont times = PdfFontFactory.CreateFont("times.ttf", "cp1251", true);
            PdfFont gostBu = PdfFontFactory.CreateFont("GOST_BU.ttf", "cp1251", true);
            PdfFont f1 = PdfFontFactory.CreateFont("GOST_TYPE_A.ttf", "cp1251", true);

            float[] columnWidths = { .2f, .4f, 1, 6, .2f, 2 };

            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();//.SetHeight(A4Height);
            
            iText.Layout.Style italicHeaderStyle = new Style();
            italicHeaderStyle.SetFont(f1).SetItalic().SetFontSize(14);
            iText.Layout.Style verticalCellStyle = new Style();
            verticalCellStyle.SetFont(f1).SetItalic().SetFontSize(14).SetRotationAngle(DegreesToRadians(90));

            void ApplyVerticalCell(Cell c, string text)
            {
                c.Add(new Paragraph(text).
                    SetFont(f1).SetFontSize(14).
                    SetRotationAngle(DegreesToRadians(90)).
                    SetFixedLeading(10).
                    SetPadding(0).
                    SetMargin(0).
                    SetItalic().
                    SetTextAlignment(TextAlignment.CENTER));
                c.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                c.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                c.SetBorder(new SolidBorder(2));
//                c.SetPaddings(0, 0, 0, 0).SetMargin(10);
            }

            var cell = new Cell(5, 1);
            ApplyVerticalCell(cell, "Перв. примен.");
            cell.SetWidth(5*mmA4);
            table.AddCell(cell);

            cell = new Cell(5, 1);
            ApplyVerticalCell(cell, "ПАКБ.436122.800");
            cell.SetWidth(9*mmA4);
            table.AddCell(cell);

            void ApplyForCell(Cell c)
            {
                c.SetTextAlignment(TextAlignment.CENTER);
                c.SetHeight(mmA4 * 15);
                c.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                c.AddStyle(italicHeaderStyle);
                c.SetBorder(new SolidBorder(2));
            }

            cell = new Cell();
            cell.Add(new Paragraph("Поз.").SetFont(f1));
            cell.Add(new Paragraph("обозна-").SetFont(f1).SetFixedLeading(5));
            cell.Add(new Paragraph("чение").SetFont(f1));//.SetPaddings(0,0,0,0));
            ApplyForCell(cell);
            table.AddCell(cell);

            cell = new Cell().Add(new Paragraph("Наименование").SetFont(f1));
            ApplyForCell(cell);
            table.AddCell(cell);

            cell = new Cell().Add(new Paragraph("Кол.").SetFont(f1));
            ApplyForCell(cell);
            table.AddCell(cell);

            cell = new Cell().Add(new Paragraph("Примечание").SetFont(f1));
            ApplyForCell(cell);
            table.AddCell(cell);

            void AddEmptyCells(int numberOfCells, float height)
            {
                for (int i = 0; i < numberOfCells; ++i)
                {
                    table.AddCell(
                        new Cell().
                            SetHeight(height).
                            SetBorderLeft(new SolidBorder(2)).
                            SetBorderRight(new SolidBorder(2)));
                }
            }

            AddEmptyCells(16, 6*mmA4);
            
            cell = new Cell(7,1);
            ApplyVerticalCell(cell, "Справ. №");
            table.AddCell(cell);
            ApplyVerticalCell(cell, "");
            cell = new Cell(7,1);
            table.AddCell(cell);
            
            AddEmptyCells(28, 6*mmA4);

            cell = new Cell(3, 1).SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER);
            table.AddCell(cell);
            cell = new Cell(3, 1).SetBorderLeft(Border.NO_BORDER);
            table.AddCell(cell);
            
            AddEmptyCells(12, 6*mmA4);

            cell = new Cell(4,1);
            ApplyVerticalCell(cell, "Подп. и дата");
            table.AddCell(cell);
            cell = new Cell(4,1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);
            
            AddEmptyCells(16, 6*mmA4);
            
            cell = new Cell(3,1);
            ApplyVerticalCell(cell, "Инв. № дубл");
            table.AddCell(cell);
            cell = new Cell(3,1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);
            
            AddEmptyCells(12, 6*mmA4);
            
            cell = new Cell(3,1);
            ApplyVerticalCell(cell, "Взам. инв. №");
//            cell.SetHeight(3 * 6 * mmA4);
            table.AddCell(cell);
            cell = new Cell(3,1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);
            
            AddEmptyCells(12, 6*mmA4);

            doc.Add(table);

            doc.Close();
        }
    }
}
