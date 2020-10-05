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
    class PdfBillCreator : PdfCreator
    {
        protected new readonly float LEFT_MARGIN = 5 * mmW();
        protected new readonly float RIGHT_MARGIN = 5 * mmH();

        public PdfBillCreator() : base(DocType.Bill) {
        }

        protected new static float mmH() {
            // this document is landscape orientation
            return PdfDefines.mmAXw;
        }
        protected new static float mmW() {
            // this document is landscape orientation
            return PdfDefines.mmAXh;
        }

        public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs) 
        {
            var dataTable = aData;
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
            _doc = new Document(_pdfDoc, _pdfDoc.GetDefaultPageSize().Rotate(), true);
            
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

            //AddPageCountOnFirstPage(_doc, _currentPageNumber);

            _doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aCountPages) {
            SetPageMargins(aInDoc);
            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));
            aInDoc.Add(CreateFirstTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, Pages = aCountPages, DocType = DocType.Bill}));
            aInDoc.Add(CreateTable(aData, true, 0, out var lpr));
            DrawLines(_pdfDoc.GetFirstPage());
            return lpr;
        }


        internal override int AddNextPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNumber, int aStartRow) {
            aInDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            SetPageMargins(aInDoc);

            int lastNextProcessedRow;
            var dataTable = CreateTable(aData, false, aStartRow, out lastNextProcessedRow);
            dataTable.SetFixedPosition(19.3f * mmH(), BOTTOM_MARGIN + 16 * mmH(), TITLE_BLOCK_WIDTH + 2f);
            aInDoc.Add(dataTable);

            aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));
            aInDoc.Add(CreateNextTitleBlock(new TitleBlockStruct { PageSize = _pageSize, Graphs = aGraphs, Pages = aPageNumber, DocType = DocType.Bill}));
            DrawLines(_pdfDoc.GetPage(2));
            return lastNextProcessedRow;
        }

        Table CreateTable(DataTable aData, bool firstPage, int aStartRow, out int outLastProcessedRow) {
            
            const int COLUMNS = 11;
            float[] columnSizes = {
                60 * mmW(), 
                45 * mmW(), 
                70 * mmW(), 
                55 * mmW(),
                70 * mmW(),
                16 * mmW(),
                16 * mmW(),
                16 * mmW(),
                16 * mmW(),
                24 * mmW(),
            };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            tbl.SetMargin(0).SetPadding(0).SetFont(f1).SetFontSize(12).SetItalic().SetTextAlignment(TextAlignment.CENTER);

            Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1)
                .SetMargin(0)
                .SetPaddings(0, 0, 0, 0)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(CreateThickBorder())
                .SetBorderRight(CreateThickBorder())
                .SetFontSize(14)
                .SetHeight(8 * mmH());
            Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(CreateThickBorder())
                .SetBorderRight(CreateThickBorder())
                .SetFontSize(14)
                .SetHeight(8 * mmH());

            AddDataTableHeader(tbl);

            var rowNumber = firstPage ? RowNumberOnFirstPage : RowNumberOnNextPage;
            outLastProcessedRow = aStartRow;

            var Rows = aData.Rows.Cast<DataRow>().ToArray();
            DataRow row;
            int inc = 0;
            for (int ind = aStartRow; ind < Rows.Length; ind++)
            {
                if (rowNumber <= 0) {
                    break;
                }

                row = Rows[ind];

                string GetCellString(string columnName) =>(row[columnName] == DBNull.Value)
                    ? string.Empty
                    : (string) row[columnName];

                BasePreparer.FormattedString GetCellStringFormatted(string columnName) =>
                (row[columnName] == System.DBNull.Value)
                    ? null 
                    : ((BasePreparer.FormattedString) row[columnName]);
                                    
                string name = GetCellString(Constants.ColumnName);
                string productCode = GetCellString(Constants.ColumnProductCode);
                string deliveryDocSign = GetCellString(Constants.ColumnDeliveryDocSign);
                string supplier = GetCellString(Constants.ColumnSupplier);
                string entry = GetCellString(Constants.ColumnEntry);
                int quantityDev = (row[Constants.ColumnQuantityDevice] == DBNull.Value) ? 0 : (int) row[Constants.ColumnQuantityDevice];
                int quantityComplex = (row[Constants.ColumnQuantityComplex] == DBNull.Value) ? 0 : (int) row[Constants.ColumnQuantityComplex];
                int quantityReg = (row[Constants.ColumnQuantityRegul] == DBNull.Value) ? 0 : (int) row[Constants.ColumnQuantityRegul];
                int quantityTotal = (row[Constants.ColumnQuantityTotal] == DBNull.Value) ? 0 : (int) row[Constants.ColumnQuantityTotal];
                string note = GetCellString(Constants.ColumnFootnote);

                inc++;
                if (string.IsNullOrEmpty(name)) 
                {
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(inc.ToString())));
                    AddEmptyRowToPdfTable(tbl, 1, COLUMNS - 1, leftPaddCell);
                    rowNumber--;
                }
                else if (string.IsNullOrEmpty(productCode) && string.IsNullOrEmpty(deliveryDocSign) && string.IsNullOrEmpty(supplier) && string.IsNullOrEmpty(entry)) 
                {
                    // это наименование группы
                    if (rowNumber > 4) 
                    {
                        // если есть место для записи более 4 строк то записываем группу, иначе выходим
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString())));                        
                        tbl.AddCell(leftPaddCell.Clone(true).Add(new Paragraph(name)));
                        tbl.AddCell(leftPaddCell.Clone(false)); // Код продукции
                        tbl.AddCell(leftPaddCell.Clone(false)); // Обозначение документа на поставку
                        tbl.AddCell(leftPaddCell.Clone(false)); // Поставщик
                        tbl.AddCell(leftPaddCell.Clone(false)); // Куда входит (обозначение)
                        tbl.AddCell(leftPaddCell.Clone(false)); // Количество на изделие
                        tbl.AddCell(leftPaddCell.Clone(false)); // Количество в комплекты
                        tbl.AddCell(leftPaddCell.Clone(false)); // Количество на регулир.
                        tbl.AddCell(leftPaddCell.Clone(false)); // Количество всего
                        tbl.AddCell(leftPaddCell.Clone(false)); // Примечание
                        rowNumber--;
                    }
                    else                 
                        break;                
                }
                else 
                {
                    // просто запишем строку
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString())));                        
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(name)));
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(productCode))); // Код продукции
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(deliveryDocSign))); // Обозначение документа на поставку
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(supplier))); // Поставщик
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(entry))); // Куда входит (обозначение)
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(quantityDev.ToString()))); // Количество на изделие
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(quantityComplex.ToString()))); // Количество в комплекты
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(quantityReg.ToString()))); // Количество на регулир.
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(quantityTotal.ToString()))); // Количество всего
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(note))); // Примечание
                    rowNumber--; 
                }
                outLastProcessedRow++;
            }


            //for (int i = 0; i < (rowNumber-1)*10; ++i) {
            //    tbl.AddCell(CreateCell().SetHeight(8*mmH()));
            //}
            //for (int i = 0; i < 10; ++i) {
            //    tbl.AddCell(CreateCell().SetBorderBottom(CreateThickBorder()).SetHeight(8*mmH()));
            //}

            var ass  = columnSizes.Sum();
            var bottom = firstPage ? BOTTOM_MARGIN + TITLE_BLOCK_FIRST_PAGE_FULL_HEIGHT_MM * mmH() : 0;
            tbl.SetFixedPosition(APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f, bottom, columnSizes.Sum() + 8*mmW() +0.5f);

             // дополним таблицу пустыми строками если она не полностью заполнена
            if (rowNumber > 0) {
                AddEmptyRowToPdfTable(tbl, rowNumber, COLUMNS, centrAlignCell, true);
            }
            if (outLastProcessedRow == aData.Rows.Count) {
                outLastProcessedRow = 0;
            }
            return tbl;
        }

        /// <summary>
        /// Adds the data table header.
        /// </summary>
        /// <param name="aTable">a table.</param>
        void AddDataTableHeader(Table aTable) {

            Cell CreateCell(int rowspan=1, int colspan=1) => new Cell(rowspan, colspan).SetPadding(0).SetMargin(0).SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder());

            void AddMainHeaderCell(string text) =>
                aTable.AddCell(CreateCell(2,1)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBorder(CreateThickBorder()).Add(new Paragraph(text)));

            AddMainHeaderCell("Наименование");
            AddMainHeaderCell("Код продукции");
            AddMainHeaderCell("Обозначение документа на поставку");
            AddMainHeaderCell("Поставщик");
            AddMainHeaderCell("Куда входит (обозначение)");

            aTable.AddCell(
                CreateCell(1, 4)
                    .SetBorder(CreateThickBorder())
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetHeight(9*mmH()).Add(new Paragraph("Количество")));
            AddMainHeaderCell("Примечание");
            
            void AddSecondaryHeaderCell(string text) => aTable.AddCell(CreateCell().SetBorder(CreateThickBorder()).SetHeight(18*mmH()).Add(new Paragraph(text)));

            AddSecondaryHeaderCell("на из-\nделие");
            AddSecondaryHeaderCell("в ком-\nплекте");
            AddSecondaryHeaderCell("на ре-\nгулир");
            AddSecondaryHeaderCell("всего");
        }

        void DrawLines(PdfPage aPage) {
            var pageWidth = aPage.GetPageSize().GetWidth();

            Canvas canvas = new Canvas(new PdfCanvas(aPage),
                new Rectangle(APPEND_GRAPHS_LEFT, BOTTOM_MARGIN, PdfDefines.A3Width, 2));
            canvas.Add(
                new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth((228) * mmW()));

            var leftVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;
            var x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f;
            var y = BOTTOM_MARGIN;
            canvas = new Canvas(new PdfCanvas(aPage), new Rectangle(x, y, 2, leftVertLineHeight));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(leftVertLineHeight).SetRotationAngle(DegreesToRadians(90)));

            var upperHorizLineWidth = pageWidth - (x+RIGHT_MARGIN);
            y = BOTTOM_MARGIN + leftVertLineHeight;
            canvas = new Canvas(new PdfCanvas(aPage), new Rectangle(x, y, upperHorizLineWidth, 2));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(upperHorizLineWidth));

            var rightVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;//+ upperHorizLineWidth;
            x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f + upperHorizLineWidth;
            y = BOTTOM_MARGIN+2f;
            canvas = new Canvas(new PdfCanvas(aPage), new Rectangle(x, y, 2, rightVertLineHeight));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(rightVertLineHeight).SetRotationAngle(DegreesToRadians(90)));
        }

        protected new void AddPageCountOnFirstPage(iText.Layout.Document aDoc, int aPageCount) {
            float bottom = 65f;
            float left = PdfDefines.A3Height - TITLE_BLOCK_WIDTH_MM * mmW() + (7 + 10 + 23 + 15 + 10 + 14 + 53+5+5+5+15) * mmW()+ 20;
            //left = 300;
            aDoc.ShowTextAligned(new Paragraph(aPageCount.ToString()).
                                SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFontSize(12).SetFont(f1),
                                left, bottom,
                                1, 
                                TextAlignment.CENTER, VerticalAlignment.MIDDLE, 
                                0);
        }
    }
}
