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
using Xceed.Wpf.Toolkit.Core.Converters;
using Document = iText.Layout.Document;

namespace GostDOC.PDF
{
    class PdfSpecificationCreator : PdfCreator {

        //private static readonly float DATA_TABLE_CELL_HEIGHT_MM = 8;
        //private new readonly float DATA_TABLE_LEFT = 19.3f * mmW() - TO_LEFT_CORRECTION;

        public PdfSpecificationCreator() : base(DocType.Specification) {
        }

        public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs, Dictionary<string, object> aAppParams) 
        {
            var graphs = aMainGraphs;
            if (aData == null)
                return;

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
            int countPages = PdfUtils.GetCountPage(Type, dataTable.Rows.Count);
            int lastProcessedRow = AddFirstPage(_doc, graphs, dataTable, countPages);
            
            _currentPageNumber = 1;
            while (lastProcessedRow > 0) {
                _currentPageNumber++;
                lastProcessedRow = AddNextPage(_doc, graphs, dataTable, _currentPageNumber, lastProcessedRow);
            }
            
            if (_pdfDoc.GetNumberOfPages() > PdfDefines.MAX_PAGES_WITHOUT_CHANGELIST) {
                _currentPageNumber++;
                AddRegisterList(_doc, graphs, _currentPageNumber);
            }
            
            _doc.Close();            
        }



        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aCountPages, Dictionary<string, object> aAppParams = null) {

            SetPageMargins(aInDoc);

            var dataTable = CreateDataTable(new DataTableStruct{Data=aData, FirstPage = true, StartRow = 0}, out var lastProcessedRow);
            dataTable.SetFixedPosition(
                DATA_TABLE_LEFT,
                PdfDefines.A4Height - (GetTableHeight(dataTable, 1) + TOP_MARGIN),
                TITLE_BLOCK_WIDTH /*- 0.02f*/);

            aInDoc.Add(dataTable);
            
            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, Pages = aCountPages, CurrentPage = 1, DocType = DocType.Specification}));

            // добавить таблицу с верхней дополнительной графой
            aInDoc.Add(CreateTopAppendGraph(_pageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawLines(1);

            AddCopyFormatSubscription(aInDoc, 1);

            AddVerticalProjectSubscription(aInDoc, aGraphs);

            return lastProcessedRow;
        }

        internal override int AddNextPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNamuber, int aStartRow, Dictionary<string, object> aAppParams = null) {

            SetPageMargins(aInDoc);

            aInDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            SetPageMargins(aInDoc);
            var dataTable = CreateDataTable(new DataTableStruct{Graphs = aGraphs, Data = aData, FirstPage = false, StartRow = aStartRow}, out var lastProcessedRow);
            dataTable.SetFixedPosition(
                DATA_TABLE_LEFT,
                PdfDefines.A4Height - (GetTableHeight(dataTable, 1) + TOP_MARGIN) + 1.5f,
                TITLE_BLOCK_WIDTH);
            aInDoc.Add(dataTable);
            
            // добавить таблицу с основной надписью             
            var titleBlock = CreateNextTitleBlock(new TitleBlockStruct { PageSize = _pageSize, Graphs = aGraphs, CurrentPage = aPageNamuber, DocType = DocType.Specification });
            titleBlock.SetFixedPosition(DATA_TABLE_LEFT, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH - 0.02f);
            aInDoc.Add(titleBlock);


            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            DrawLines(_currentPageNumber);

            AddCopyFormatSubscription(aInDoc, aPageNamuber);

            AddVerticalProjectSubscription(aInDoc, aGraphs);

            return lastProcessedRow;
        }

        void AddDataTableHeader(Table aTable) {

            Cell headerCell = new Cell().SetVerticalAlignment(VerticalAlignment.MIDDLE).SetBorder(THICK_BORDER).SetHeight(15*mmH()).SetPadding(0);
            Paragraph CreateParagraph(string text) => new Paragraph(text).SetFont(f1).SetItalic().SetTextAlignment(TextAlignment.CENTER).SetFontSize(14)/*.SetMargin(0)*/;

            Table AddHeaderCell90(string text) => 
                aTable.AddCell(headerCell.Clone(false)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .Add(CreateParagraph(text).SetRotationAngle(DegreesToRadians(90))));
            Table AddHeaderCell(string text) => 
                aTable.AddCell(headerCell.Clone(false)
                    .Add(CreateParagraph(text).SetFixedLeading(11)));

            AddHeaderCell90("Формат");
            AddHeaderCell90("Зона");
            AddHeaderCell90("Поз.");
            AddHeaderCell("Обозначение");
            AddHeaderCell("Наименование");
            AddHeaderCell90("Кол.");
            AddHeaderCell("Приме-\nчание");
        }

        Table CreateDataTable(DataTableStruct aDataTableStruct, out int outLastProcessedRow) {
            const int COLUMNS = 7;
            var aStartRow = aDataTableStruct.StartRow;

            float[] columnSizes = { 
                Constants.SpecificationColumn1FormatWidth  * mmW(),
                Constants.SpecificationColumn2ZoneWidth  * mmW(),
                Constants.SpecificationColumn3PositionWidth  * mmW(),
                Constants.SpecificationColumn4SignWidth * mmW(),
                Constants.SpecificationColumn5NameWidth * mmW(),
                Constants.SpecificationColumn6QuantityWidth * mmW(),
                Constants.SpecificationColumn7FootnoteWidth * mmW()};

            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            tbl.SetMargin(0).SetPadding(0);
            
            AddDataTableHeader(tbl);

            Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1)
                .SetMargin(0)
                .SetPaddings(0, 0, 0, 0)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER)
                .SetFontSize(Constants.SpecificationFontSize);
            Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER)
                .SetFontSize(Constants.SpecificationFontSize);
            Cell rightPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 2, 0, 0)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER)
                .SetFontSize(Constants.SpecificationFontSize);

            int remainingPdfTableRows = (aDataTableStruct.FirstPage) ? RowNumberOnFirstPage : RowNumberOnNextPage;
            outLastProcessedRow = aStartRow;

            var Rows = aDataTableStruct.Data.Rows.Cast<DataRow>().ToArray();
            DataRow row;
            for (int ind = aStartRow; ind < Rows.Length; ind++) {

                if (remainingPdfTableRows <= 0) {
                    break;
                }

                row = Rows[ind];

                string GetCellString(string columnName) =>
                    (row[columnName] == System.DBNull.Value)
                        ? string.Empty
                        : ((BasePreparer.FormattedString) row[columnName]).Value;

                BasePreparer.FormattedString GetCellStringFormatted(string columnName) =>
                    (row[columnName] == System.DBNull.Value)
                        ? null 
                        : ((BasePreparer.FormattedString) row[columnName]);

                string format = GetCellString(Constants.ColumnFormat).Truncate(3);
                string zone = GetCellString(Constants.ColumnZone).Truncate(3);
                string position = GetCellString(Constants.ColumnPosition).Truncate(3);
                string note = GetCellString(Constants.ColumnFootnote).Truncate(14);
                var sign = GetCellStringFormatted(Constants.ColumnSign);
                if (sign != null) {
                    sign.Value = sign.Value.Truncate(38);
                }

                var name = GetCellStringFormatted(Constants.ColumnName);

                void AddCellFormatted(BasePreparer.FormattedString fs) {
                    Cell c;
                    if (fs != null)
                    {                        
                        if (fs.TextAlignment == TextAlignment.CENTER)
                        {
                            c = (centrAlignCell.Clone(false).Add(new Paragraph(fs.Value)));
                        } else if (fs.TextAlignment == TextAlignment.LEFT)
                        {
                            c = (leftPaddCell.Clone(false).Add(new Paragraph(fs.Value)));
                        }
                        else{
                            c = (rightPaddCell.Clone(false).Add(new Paragraph(fs.Value)));
                        }
                        if (fs.IsUnderlined) c.SetUnderline(0.5f, -1);
                    }
                    else
                    {
                        c = centrAlignCell.Clone(false);
                    }
                    tbl.AddCell(c);
                }

                int quantity = (row[Constants.ColumnQuantity] == System.DBNull.Value)
                    ? 0
                    : (int) row[Constants.ColumnQuantity];

                // если пустая строка или строка с резервированием номера позиции
                if ((name == null || string.IsNullOrEmpty(name.Value)) &&
                    (sign == null || string.IsNullOrEmpty(sign.Value)) && 
                     string.IsNullOrEmpty(note))
                {                    
                    AddEmptyRowToPdfTable(tbl, 1, COLUMNS, leftPaddCell, remainingPdfTableRows == 1 ? true : false);                    
                    remainingPdfTableRows--;
                } 
                else if (string.IsNullOrEmpty(position) && string.IsNullOrEmpty(note) && string.IsNullOrEmpty(format))  {
                    // наименование группы
                    if (remainingPdfTableRows > 4)  {
                        // если есть место для записи более 4 строк то записываем группу, иначе выходим
                        tbl.AddCell(centrAlignCell.Clone(false)); // формат
                        tbl.AddCell(centrAlignCell.Clone(false)); // зона
                        tbl.AddCell(centrAlignCell.Clone(false)); // поз                        
                        AddCellFormatted(sign); 
                        AddCellFormatted(name);
                        tbl.AddCell(centrAlignCell.Clone(false)); // кол
                        tbl.AddCell(centrAlignCell.Clone(false)); // примеч.
                        remainingPdfTableRows--;
                    }
                    else {
                        break;
                    }
                } else  {
                    if (remainingPdfTableRows == 1) {
                        centrAlignCell.SetBorderBottom(THICK_BORDER);
                        leftPaddCell.SetBorderBottom(THICK_BORDER);
                    }
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(format))); // формат
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(zone))); // зона
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(position)));
                    AddCellFormatted(sign); // обозначение
                    AddCellFormatted(name);
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(quantity == 0 ? "" : quantity.ToString())));
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(note)));
                    remainingPdfTableRows--;
                }

                outLastProcessedRow++;
            }

            // дополним таблицу пустыми строками если она не полностью заполнена
            if (remainingPdfTableRows > 0) {
                AddEmptyRowToPdfTable(tbl, remainingPdfTableRows, COLUMNS, centrAlignCell, true);
            }
            if (outLastProcessedRow == aDataTableStruct.Data.Rows.Count) {
                outLastProcessedRow = 0;
            }

            return tbl;
        }

        private void DrawLines(int pageNumber) {
            var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH - 2f - TO_LEFT_CORRECTION;
            DrawVerticalLine(pageNumber, fromLeft, BOTTOM_MARGIN + (8+7) * mmW()-6f, 2, 200);
        }                

    }
}
