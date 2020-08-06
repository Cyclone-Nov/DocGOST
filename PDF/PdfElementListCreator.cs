using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Org.BouncyCastle.Asn1.Crmf;

using GostDOC.Common;
using iText.Kernel.Pdf.Canvas;

namespace GostDOC.PDF
{
    /// <summary>
    /// Перечень элементов
    /// </summary>
    /// <seealso cref="GostDOC.PDF.PdfCreator" />
    internal class PdfElementListCreator : PdfCreator
    {

        private const string FileName = @"Перечень элементов.pdf";

        public PdfElementListCreator(string aSavePath) : base(aSavePath, DocType.ItemsList)
        { }

        /// <summary>
        /// добавить к документу первую страницу
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal override bool AddFirstPage(PdfDocument aInPdfDoc)
        {
            PdfPage page = aInPdfDoc.AddNewPage();

            var pdfCanvas = new PdfCanvas(page);
                      


            //Document doc = new Document(page,);
            //doc.SetLeftMargin(9 * PdfDefines.mmA4);
            //doc.SetRightMargin(9 * PdfDefines.mmA4);
            //doc.SetTopMargin(5 * PdfDefines.mmA4);
            //doc.SetBottomMargin(5 * PdfDefines.mmA4);



            return false;
        }

        /// <summary>
        /// добавить к документу последующие страницы
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal override bool AddNextPage(PdfDocument aInPdfDoc)
        {
            return false;
        }

        /// <summary>
        /// Создать документ
        /// </summary>
        public override void Create()
        {
            string[] names = { FileName, SavePath};
            string fullName = System.IO.Path.Combine(names);
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(fullName));
            pdfDoc.SetDefaultPageSize(PageSize.A4);


            bool next = AddFirstPage(pdfDoc);            
            while (next)
            {
                next = AddNextPage(pdfDoc);
            }
            
            if (pdfDoc.GetNumberOfPages() > 2)
            {
                AddRegisterList(pdfDoc);
            }


            Document doc = new Document(pdfDoc, PageSize.A4, false);            
            doc.SetLeftMargin(9 * PdfDefines.mmA4);
            doc.SetRightMargin(9 * PdfDefines.mmA4);
            doc.SetTopMargin(5 * PdfDefines.mmA4);
            doc.SetBottomMargin(5 * PdfDefines.mmA4);

            

            // var page = pdfDoc.AddNewPage();
            // var pdfCanvas = new PdfCanvas(page);
            // Rectangle rectangle = new Rectangle(mmA4* 10, mmA4 * 10, A4Width, mmA4 * 40);
            // Canvas canvas = new Canvas(pdfCanvas, rectangle);
            // canvas.Add(table);
            // PdfFont f1 = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN, true);

            //PdfFont times = PdfFontFactory.CreateFont("GOST_TYPE_A.ttf", "cp1251", true);
            //PdfFont gostBu = PdfFontFactory.CreateFont("GOST_BU.ttf", "cp1251", true);

            // float[] columnWidths = {.2f, .4f, 1, 6, .2f, 2};
            float[] columnWidths = { 1, 13, 0.1f, 5 };
            Table table =
                new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth(); //.SetHeight(A4Height);

            table.SetBorderBottom(new SolidBorder(2));
            iText.Layout.Style italicHeaderStyle = new Style();
            italicHeaderStyle.SetFont(f1).SetItalic().SetFontSize(14);
            iText.Layout.Style verticalCellStyle = new Style();
            verticalCellStyle.SetFont(f1).SetItalic().SetFontSize(14).SetRotationAngle(DegreesToRadians(90));

           
            var cell = new Cell();
            cell.Add(new Paragraph("Поз.").SetFont(f1));
            cell.Add(new Paragraph("обозна-").SetFont(f1).SetFixedLeading(5));
            cell.Add(new Paragraph("чение").SetFont(f1)); //.SetPaddings(0,0,0,0));
            ApplyForCell(cell);
            table.AddCell(cell);

            cell = new Cell().Add(new Paragraph("Наименование").SetFont(f1));
            ApplyForCell(cell);
            table.AddCell(cell);

            cell = new Cell().Add(new Paragraph("Кол.").SetFont(f1).SetMargin(0).SetPadding(0))
                .SetMargin(0).SetWidth(4 * PdfDefines.mmA4).SetPaddingLeft(-10).SetPaddingRight(-10);
            ApplyForCell(cell);
            table.AddCell(cell);

            cell = new Cell().Add(new Paragraph("Примечание").SetFont(f1));
            ApplyForCell(cell);
            table.AddCell(cell);


            AddEmptyCells(16, PdfDefines.ROW_HEIGHT, table);

            table.AddCell(AddTopAppendGraph("",""));

            AddEmptyCells(28, PdfDefines.ROW_HEIGHT, table);

            cell = new Cell(3, 1).SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER);
            table.AddCell(cell);
            cell = new Cell(3, 1).SetBorderLeft(Border.NO_BORDER);
            table.AddCell(cell);

            AddEmptyCells(12, PdfDefines.ROW_HEIGHT, table);

            table.AddCell(AddBottomAppendGraph());

            AddEmptyCells(12, PdfDefines.ROW_HEIGHT, table);


            FooterTableInfo footerTableInfo = new FooterTableInfo();
            table.AddCell(new Cell(1, 4).SetPadding(0).Add(AddFooterTable(footerTableInfo)).SetBorderRight(new SolidBorder(2)));

            doc.Add(table);
            doc.Close();
        }

        /// <summary>
        /// Добавить верхнюю дополнительную графу
        /// </summary>
        /// <param name="aFirstApp">значение для первичного применения</param>
        /// <param name="aRefNumber">значение для справочного номера</param>
        /// <returns></returns>
        private Table AddTopAppendGraph(string aFirstApp, string aRefNumber)
        {
            float[] columnWidths = { 0.6f, 1.1f};
            Table table =
                new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth(); //.SetHeight(A4Height);

            table.SetBorderBottom(new SolidBorder(2));
            iText.Layout.Style italicHeaderStyle = new Style();
            italicHeaderStyle.SetFont(f1).SetItalic().SetFontSize(14);
            iText.Layout.Style verticalCellStyle = new Style();
            verticalCellStyle.SetFont(f1).SetItalic().SetFontSize(14).SetRotationAngle(DegreesToRadians(90));

            var cell = new Cell(5, 1);
            ApplyVerticalCell(cell, "Перв. примен.");
            cell.SetWidth(5 * PdfDefines.mmA4);
            table.AddCell(cell);

            cell = new Cell(5, 1);
            ApplyVerticalCell(cell, aFirstApp);
            cell.SetWidth(9 * PdfDefines.mmA4).SetPaddingLeft(4);
            table.AddCell(cell);

            cell = new Cell(7, 1);
            ApplyVerticalCell(cell, "Справ. №");
            table.AddCell(cell);
            ApplyVerticalCell(cell, "");
            cell = new Cell(7, 1);
            table.AddCell(cell);

            return table;

        }

        /// <summary>
        /// добавить нижнюю дополнительную графу
        /// </summary>
        private Table AddBottomAppendGraph()
        {
            float[] columnWidths = { 0.6f, 1.1f };
            Table table =
                new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth(); //.SetHeight(A4Height);

            table.SetBorderBottom(new SolidBorder(2));
            iText.Layout.Style italicHeaderStyle = new Style();
            italicHeaderStyle.SetFont(f1).SetItalic().SetFontSize(14);
            iText.Layout.Style verticalCellStyle = new Style();
            verticalCellStyle.SetFont(f1).SetItalic().SetFontSize(14).SetRotationAngle(DegreesToRadians(90));

            var cell = new Cell(4, 1);
            ApplyVerticalCell(cell, "Подп. и дата");
            table.AddCell(cell);
            cell = new Cell(4, 1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);

            AddEmptyCells(16, PdfDefines.ROW_HEIGHT, table);

            cell = new Cell(3, 1);
            ApplyVerticalCell(cell, "Инв. № дубл");
            table.AddCell(cell);
            cell = new Cell(3, 1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);

            AddEmptyCells(12, PdfDefines.ROW_HEIGHT, table);

            cell = new Cell(3, 1);
            ApplyVerticalCell(cell, "Взам. инв. №");
            table.AddCell(cell);
            cell = new Cell(3, 1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);

            AddEmptyCells(12, PdfDefines.ROW_HEIGHT, table);

            cell = new Cell(4, 1);
            ApplyVerticalCell(cell, "Подп. и дата");
            table.AddCell(cell);
            cell = new Cell(4, 1);
            ApplyVerticalCell(cell, "");
            table.AddCell(cell);

            return table;
        }

        class FooterTableInfo
        {
            public string Abvgd = "ПАКБ.436122.800ПЭЗ";
            public string DevelopedBy = "Горбач";
            public string CheckedBy = "Васильев";
            public string ControlBy = "Корнева";
            public string ApprovedBy = "Гульцов";
            public string Name = "Модуль питания (МП)";

            public int PageNumber = 1;
            public int PagesCount = 8;
        }

        /// <summary>
        /// Добавить основную надпись
        /// </summary>
        private Table AddFooterTable(FooterTableInfo footerTableInfo)
        {
            var columnWidths = new[] { 0.8f, 1.2f, 3f, 2.5f, 1.1f, 7, 6 };
            var footerTable = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth().SetMargin(0);
            var centerAlignedStyle = new Style().SetFont(f1).SetItalic().SetFontSize(12).SetTextAlignment(TextAlignment.CENTER);

            footerTable.AddStyle(centerAlignedStyle);
            footerTable.SetBorderTop(new SolidBorder(2));

            bool isBigCellAdded = false;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; ++col)
                {
                    Cell tmp = null;
                    if (col != 5)
                    {
                        tmp = new Cell();
                        tmp.SetHeight(PdfDefines.INNER_TABLE_ROW_HEIGHT).SetBorderRight(new SolidBorder(2));
                    }
                    else if (!isBigCellAdded)
                    {
                        tmp = new Cell(3, 2);
                        tmp.SetVerticalAlignment(VerticalAlignment.MIDDLE).
                            SetHorizontalAlignment(HorizontalAlignment.CENTER).
                            SetBorderRight(Border.NO_BORDER).
                            SetBorderTop(Border.NO_BORDER).
                            SetBorderBottom(new SolidBorder(2));
                        tmp.Add(new Paragraph(footerTableInfo.Abvgd).
                            SetFont(f1).
                            SetFontSize(20).
                            SetItalic().
                            SetTextAlignment(TextAlignment.CENTER));
                        isBigCellAdded = true;
                    }
                    if (tmp == null) continue;

                    if (col != 0)
                    {
                        tmp.SetBorderLeft(new SolidBorder(2));
                    }
                    else
                    {
                        tmp.SetBorderLeft(Border.NO_BORDER);
                    }

                    string text = "";
                    if (row == 2)
                    {
                        switch (col)
                        {
                            case 0:
                                {
                                    text = "Изм";
                                    break;
                                }
                            case 1:
                                {
                                    text = "Лист";
                                    break;
                                }
                            case 2:
                                {
                                    text = "№ докум";
                                    break;
                                }
                            case 3:
                                {
                                    text = "Подп.";
                                    break;
                                }
                            case 4:
                                {
                                    text = "Дата";
                                    break;
                                }
                        }
                        tmp.Add(new Paragraph(text)
                            .SetFixedLeading(12).SetPaddingBottom(-5)
                        );
                    }
                    footerTable.AddCell(tmp);
                }
            }

            var leftAlignedStyle = new Style().SetTextAlignment(TextAlignment.LEFT);

            leftAlignedStyle.SetTextAlignment(TextAlignment.LEFT);

            void AddPersonRow(string textForPerson, string personName)
            {
                footerTable.AddCell(
                    new Cell(1, 2).
                        Add(new Paragraph(textForPerson).
                            /*SetPaddingBottom(-7).*/SetPaddingLeft(-1)).
                        AddStyle(leftAlignedStyle).
                        SetHeight(PdfDefines.INNER_TABLE_ROW_HEIGHT).
                        SetBorderRight(new SolidBorder(2)).
                        SetBorderLeft(Border.NO_BORDER));
                footerTable.AddCell(
                    new Cell().
                        Add(new Paragraph(personName).
                            SetPaddingBottom(-5).SetPaddingLeft(-1)).
                        AddStyle(leftAlignedStyle).
                        SetBorderRight(new SolidBorder(2)));

                for (int i = 0; i < 2; ++i)
                {
                    footerTable.AddCell(new Cell().SetBorderRight(new SolidBorder(2)));
                }
            }

            AddPersonRow("Разраб.", footerTableInfo.DevelopedBy);

            footerTable.AddCell(new Cell(5, 1).Add(new Paragraph(footerTableInfo.Name)).SetBorderRight(new SolidBorder(2)));
            var pageInfoSubTable = CreatePagesInfoTable(footerTableInfo);
            footerTable.AddCell(new Cell(5, 1).Add(pageInfoSubTable).SetPadding(0));

            AddPersonRow("Пров.", footerTableInfo.CheckedBy);
            AddPersonRow("", "");
            AddPersonRow("Н. контр", footerTableInfo.ControlBy);
            AddPersonRow("Утв.", footerTableInfo.ApprovedBy);

            return footerTable;
        }

        private static Table CreatePagesInfoTable(FooterTableInfo footerTableInfo)
        {
            Cell createCell(int rowspan = 1, int colspan = 1)
            {
                return new Cell(rowspan, colspan).SetBorderBottom(new SolidBorder(2))
                    .SetHeight(PdfDefines.INNER_TABLE_ROW_HEIGHT);
            }
            Paragraph createParagraph()
            {
                return new Paragraph().SetPaddingBottom(-10); //.SetMarginBottom(-5);
            }

            var columnWidths = new[] { 1f, 1f, 1f, 3f, 4f };
            var pageInfoSubTable = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth()
                .SetMargin(0).SetBorder(Border.NO_BORDER);

            pageInfoSubTable.AddCell(createCell(1, 3).Add(createParagraph().Add("Лит")).
                SetBorderLeft(Border.NO_BORDER).
                SetBorderRight(new SolidBorder(2)).
                SetBorderTop(Border.NO_BORDER));
            pageInfoSubTable.AddCell(createCell().Add(createParagraph().Add("Лист")).
                SetBorderRight(new SolidBorder(2)).
                SetBorderLeft(new SolidBorder(2)).
                SetBorderTop(Border.NO_BORDER));
            pageInfoSubTable.AddCell(createCell().Add(createParagraph().Add("Листов").SetBorderRight(Border.NO_BORDER)).
                SetBorderTop(Border.NO_BORDER).
                SetBorderRight(Border.NO_BORDER));

            for (int i = 0; i < 3; ++i)
            {
                var c = createCell();
                if (i == 0)
                {
                    c.SetBorderLeft(Border.NO_BORDER);
                }
                else
                {
                    c.SetBorderLeft(new SolidBorder(2));
                }
                c.SetBorderRight(new SolidBorder(2));
                pageInfoSubTable.AddCell(c);
            }

            pageInfoSubTable.AddCell(createCell().Add(createParagraph().Add(footerTableInfo.PageNumber.ToString())).SetBorderRight(new SolidBorder(2)));
            pageInfoSubTable.AddCell(createCell().Add(createParagraph().Add(footerTableInfo.PagesCount.ToString())).SetBorderRight(Border.NO_BORDER));

            return pageInfoSubTable;
        }


        void ApplyVerticalCell(Cell c, string text)
        {
            c.Add(
                new Paragraph(text).
                    SetFont(f1).
                    SetFontSize(12).
                    SetRotationAngle(DegreesToRadians(90)).
                    SetFixedLeading(10).
                    SetPadding(0).
                    SetPaddingRight(-10).
                    SetPaddingLeft(-10).
                    SetMargin(0).
                    SetItalic().
                    SetTextAlignment(TextAlignment.CENTER));

            c.SetHorizontalAlignment(HorizontalAlignment.CENTER)
             .SetVerticalAlignment(VerticalAlignment.MIDDLE)
             .SetMargin(0)
             .SetPadding(0)
             .SetBorder(new SolidBorder(2));
        }

        void AddEmptyCells(int numberOfCells, float height, Table aTable)
        {
            for (int i = 0; i < numberOfCells; ++i)
            {
                aTable.AddCell(
                    new Cell().SetHeight(height).SetBorderLeft(new SolidBorder(2)).SetBorderRight(new SolidBorder(2)));
            }
        }

        void ApplyForCell(Cell c)
        {
            iText.Layout.Style italicHeaderStyle = new Style();
            c.SetTextAlignment(TextAlignment.CENTER);
            c.SetHeight(PdfDefines.mmA4 * 15);
            c.SetVerticalAlignment(VerticalAlignment.MIDDLE);
            c.AddStyle(italicHeaderStyle);
            c.SetBorder(new SolidBorder(2));
        }
    }
}
