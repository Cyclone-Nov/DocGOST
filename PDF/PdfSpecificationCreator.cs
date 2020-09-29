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
using iText.Layout.Layout;
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
            var graphs = aMainGraphs;

            if (_pdfWriter != null)
            {
                _doc.Close();
                _doc = null;
                _pdfDoc.Close();
                _pdfDoc = null;
                _pdfWriter.Close();
                _pdfWriter.Dispose();
                _pdfWriter = null;
                MainStream.Dispose();
                MainStream = null;                
            }

            f1 = PdfFontFactory.CreateFont(@"Font\\GOST_TYPE_A.ttf", "cp1251", true);
            MainStream = new MemoryStream();
            _pdfWriter = new PdfWriter(MainStream);
            _pdfDoc = new PdfDocument(_pdfWriter);
            _pdfDoc.SetDefaultPageSize(_pageSize);
            _doc = new iText.Layout.Document(_pdfDoc, _pdfDoc.GetDefaultPageSize(), true);
            
            var dataTable = aData;
           
            int lastProcessedRow = AddFirstPage(_doc, graphs, dataTable);
            
            _currentPageNumber = 1;
            while (lastProcessedRow > 0) {
                _currentPageNumber++;
                lastProcessedRow = AddNextPage(_doc, graphs, dataTable, _currentPageNumber, lastProcessedRow);
            }
            
            if (_pdfDoc.GetNumberOfPages() > MAX_PAGES_WITHOUT_CHANGELIST) {
                _currentPageNumber++;
                AddRegisterList(_doc, graphs, _currentPageNumber);
            }
            
            AddPageCountOnFirstPage(_doc, _currentPageNumber);

            _doc.Close();            
        }

        float GetTableHeight(/*int pageNumber,*/ Table table) {
            var result = table.CreateRendererSubTree().SetParent(_doc.GetRenderer()).Layout(new LayoutContext(new LayoutArea(1, new Rectangle(0, 0, PageSize.A4.GetWidth(), PageSize.A4.GetHeight()))));
            float tableHeight = result.GetOccupiedArea().GetBBox().GetHeight();
            return tableHeight;
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {

            SetPageMargins(aInDoc);

            var dataTable = CreateDataTable(new DataTableStruct{Data=aData, FirstPage = true, StartRow = 0}, out var lastProcessedRow);
            dataTable.SetFixedPosition(
                19.3f * mmW(),
                PdfDefines.A4Height - (GetTableHeight(dataTable) + TOP_MARGIN_MM * mmH()) + 5.51f,
                TITLE_BLOCK_WIDTH - 0.02f);

            aInDoc.Add(dataTable);
            
            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, Pages = 2}));

            // добавить таблицу с верхней дополнительной графой
            aInDoc.Add(CreateTopAppendGraph(_pageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawMissingLinesFirstPage();

            return lastProcessedRow;
        }

        internal override int AddNextPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNamuber, int aStartRow) {
            aInDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            SetPageMargins(aInDoc);
            var dataTable = CreateDataTable(new DataTableStruct{Graphs = aGraphs, Data = aData, FirstPage = false, StartRow = aStartRow}, out var lastProcessedRow);
            dataTable.SetFixedPosition(
                19.3f * mmW(), 
                PdfDefines.A4Height - (GetTableHeight(dataTable) + TOP_MARGIN_MM * mmH()) + 5.51f,
                TITLE_BLOCK_WIDTH+2f);
            aInDoc.Add(dataTable);
            
            // добавить таблицу с основной надписью 
            aInDoc.Add(CreateNextTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, CurrentPage = 2}));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawMissingLinesNextPage(_currentPageNumber);

            return lastProcessedRow;
        }

        Table CreateDataTable(DataTableStruct aDataTableStruct, out int outLastProcessedRow) {
            const int COLUMNS = 7;
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

            // fill table
            Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1)
                .SetMargin(0)
                .SetPaddings(0, 0, 0, 0)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(CreateThickBorder())
                .SetBorderRight(CreateThickBorder())
                .SetFontSize(14);
            Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(CreateThickBorder())
                .SetBorderRight(CreateThickBorder())
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

                BasePreparer.FormattedString GetCellStringFormatted(string columnName) =>
                    (row[columnName] == System.DBNull.Value)
                        ? new BasePreparer.FormattedString { Value=String.Empty}
                        : ((BasePreparer.FormattedString) row[columnName]);


                string format = GetCellString(Constants.ColumnFormat);
                string zone = GetCellString(Constants.ColumnZone);
                string position = GetCellString(Constants.ColumnPosition);
                string designation = GetCellString(Constants.ColumnDesignation);
                string note = GetCellString(Constants.ColumnFootnote);

                var name = GetCellStringFormatted(Constants.ColumnName);

                int quantity = (row[Constants.ColumnQuantity] == System.DBNull.Value)
                    ? 0
                    : (int) row[Constants.ColumnQuantity];

                if (string.IsNullOrEmpty(name.Value)) {
                    AddEmptyRowToPdfTable(tbl, 1, COLUMNS, leftPaddCell);
                    remainingPdfTabeRows--;
                }
                else {
                    // разобьем наименование на несколько строк исходя из длины текста
                    string[] namestrings = SplitStringByWidth(110 * mmW(), fontSize, font, name.Value).ToArray();
                    if (namestrings.Length <= remainingPdfTabeRows) { 
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(format))); // формат
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(zone))); // зона
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(position)));
                        tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(designation))); // обозначение

                        Cell nameCell = leftPaddCell.Clone(false);
                        if (name.TextAlignment == TextAlignment.CENTER) {
                            nameCell = (centrAlignCell.Clone(false).Add(new Paragraph(namestrings[0]))); // наименование
                        } else if (name.TextAlignment == TextAlignment.LEFT) {
                            nameCell = (leftPaddCell.Clone(false).Add(new Paragraph(namestrings[0]))); // наименование
                        }
                        if (name.IsUnderlined) nameCell.SetUnderline(1.0f, 0);
                        tbl.AddCell(nameCell);

                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(quantity == 0 ? "" : quantity.ToString())));
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

            // дополним таблицу пустыми строками если она не полностью заполнена
            if (remainingPdfTabeRows > 0) {
                AddEmptyRowToPdfTable(tbl, remainingPdfTabeRows, COLUMNS, centrAlignCell, true);
            }
            if (outLastProcessedRow == aData.Rows.Count) {
                outLastProcessedRow = 0;
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
            Canvas canvas = new Canvas(new PdfCanvas(_pdfDoc.GetFirstPage()),
                new Rectangle(fromLeft, BOTTOM_MARGIN + TITLE_BLOCK_FIRST_PAGE_WITHOUT_APPEND_HEIGHT_MM * mmH() -2f, 2, 120));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(120)
                .SetRotationAngle(DegreesToRadians(90)));

        }
        private void DrawMissingLinesNextPage(int pageNumber) {
            // нарисовать недостающую линию
            var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH;
            Canvas canvas = new Canvas(new PdfCanvas(_pdfDoc.GetPage(pageNumber)),
                new Rectangle(fromLeft, BOTTOM_MARGIN + (8+7) * mmW()-6f, 2, 60));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(50)
                .SetRotationAngle(DegreesToRadians(90)));

        }

    }
}
