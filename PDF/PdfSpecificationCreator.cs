using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.DataPreparation;
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
    class PdfSpecificationCreator : PdfCreator {

        private static readonly float DATA_TABLE_CELL_HEIGHT_MM = 8;

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

            //aInDoc.Add(CreateDataTable(aData, true, 0, out lastProcessedRow));
            var dataTable = CreateDataTable(new DataTableStruct{Data=aData, FirstPage = true, StartRow = 0}, out var lastProcessedRow);
            dataTable.SetFixedPosition(
                19.3f * mmW(),
                PdfDefines.A4Height - ((15 + DATA_TABLE_CELL_HEIGHT_MM * RowNumberOnFirstPage) * mmH() + TOP_MARGIN) - 30*mmH(),
                TITLE_BLOCK_WIDTH - 0.02f);

            aInDoc.Add(dataTable);
            
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
            var mainTable = CreateDataTable(new DataTableStruct{Graphs = aGraphs, Data = aData, FirstPage = false}, out var lastProcessedRow);
            mainTable.SetFixedPosition(19.3f * mmW(), 37 * mmW()+5f, TITLE_BLOCK_WIDTH+2f);
            aInDoc.Add(mainTable);
            
            // добавить таблицу с основной надписью 
            aInDoc.Add(CreateNextTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, CurrentPage = 2}));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawMissingLinesNextPage(2);

            return 0;
        }

        Table CreateDataTable(DataTableStruct aDataTableStruct, out int outLastProcessedRow) {

            var aData = aDataTableStruct.Data;
            var aGraphs = aDataTableStruct.Graphs;
            var aFirstPage = aDataTableStruct.FirstPage;
            var aStartRow = aDataTableStruct.StartRow;

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

//            var rowsNumber = firstPage ? RowNumberOnFirstPage : RowNumberOnNextPage;
//            for (int i = 0; i < (rowsNumber-1) * 7; ++i) {
//                tbl.AddCell(new Cell().SetHeight(8*mmH()).SetPadding(0).SetBorderLeft(CreateThickBorder())).SetBorderRight(CreateThickBorder());
//            }
//            for (int i = 0; i < 7; ++i) {
//                tbl.AddCell(new Cell().
//                        SetHeight(8 * mmH()).
//                        SetPadding(0).
//                        SetBorderLeft(CreateThickBorder()).
//                        SetBorderRight(CreateThickBorder()).
//                        SetBorderBottom(CreateThickBorder()));
//            }


            //Cell CreateDataTableCell() => CreateCell().SetHeight(8 * mmH()).SetPadding(0).SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder()).SetBorderBottom(CreateThickBorder());

            // fill table
            Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 0)
                .SetHeight(8 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1).SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder()).SetBorderBottom(CreateThickBorder())
                .SetFontSize(14);
            Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
                .SetHeight(8 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1).SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder()).SetBorderBottom(CreateThickBorder())
                .SetFontSize(14);
            float fontSize = 14;
            PdfFont font = leftPaddCell.GetProperty<PdfFont>(20); // 20 - index for Font property

            int remainingPdfTabeRows = (aFirstPage) ? RowNumberOnFirstPage : RowNumberOnNextPage;
            outLastProcessedRow = aStartRow;

            var Rows = aData.Rows.Cast<DataRow>().ToArray();
            DataRow row;
            for (int ind = aStartRow; ind < Rows.Length; ind++) {
                row = Rows[ind];

                string GetCellString(string columnName) =>
                    (row[columnName] == System.DBNull.Value)
                        ? string.Empty
                        : ((BasePreparer.FormattedString) row[columnName]).Value;

                string format = GetCellString(Constants.ColumnFormat);
                string zone = GetCellString(Constants.ColumnZone);
                string position = GetCellString(Constants.ColumnPosition);
                string designation = GetCellString(Constants.ColumnDesignation);
                string name = GetCellString(Constants.ColumnName);
                string note = GetCellString(Constants.ColumnFootnote);

                int quantity = (row[Constants.ColumnQuantity] == System.DBNull.Value)
                    ? 0
                    : (int) row[Constants.ColumnQuantity];

                if (string.IsNullOrEmpty(name)) {
                    AddEmptyRowToPdfTable(tbl, 1, 7, leftPaddCell);
                    remainingPdfTabeRows--;
                }
                else {
                    // разобьем наименование на несколько строк исходя из длины текста
                    string[] namestrings = SplitStringByWidth(110 * mmW(), fontSize, font, name).ToArray();
                    if (namestrings.Length <= remainingPdfTabeRows) { 
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(format))); // формат
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(zone))); // зона
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(position)));
                        tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(designation))); // обозначение
                        tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(namestrings[0]))); // наименование
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(quantity.ToString())));
                        tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(note)));
                        remainingPdfTabeRows--;

                        if (namestrings.Length > 1) {
                            for (int i = 1; i < namestrings.Length; i++) {
                                for (int x = 0; x < 4; ++x) {
                                    tbl.AddCell(centrAlignCell.Clone(false));
                                }
                                tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(namestrings[i])));
                                tbl.AddCell(centrAlignCell.Clone(false));
                                tbl.AddCell(leftPaddCell.Clone(false));
                                remainingPdfTabeRows--;
                            }
                        }
                    }
                    else
                        break;
                }

                outLastProcessedRow++;
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
