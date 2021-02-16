using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using GostDOC.Common;
using GostDOC.DataPreparation;
using GostDOC.Models;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Document = iText.Layout.Document;

namespace GostDOC.PDF
{
    class PdfBillCreator : PdfCreator
    {
        private float MIDDLE_FONT_SIZE = 14;
        //protected new static readonly float BOTTOM_MARGIN = BOTTOM_MARGIN_MM * mmH() + 4f;

        readonly float[] COLUMN_SIZES = {
                Constants.BillColumn1IncWidth * mmH()-4f,
                Constants.BillColumn2NameWidth * mmW(),
                Constants.BillColumn3ProductCodeWidth * mmW(),
                Constants.BillColumn4DeliveryDocSignWidth * mmW(),
                Constants.BillColumn5SupplierWidth * mmW(),
                Constants.BillColumn6EntryWidth * mmW(),
                Constants.BillColumn7QuantityDeviceWidth * mmW(),
                Constants.BillColumn8QuantityComplexWidth * mmW(),
                Constants.BillColumn9QuantityRegulWidth * mmW(),
                Constants.BillColumn10QuantityTotalWidth * mmW(),
                Constants.BillColumn11FootnoteWidth * mmW(),
        };

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

        public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs, Dictionary<string, object> aAppParams) 
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
            
            int countPages = CommonUtils.GetCountPage(Type, dataTable.Rows.Count);
            int lastProcessedRow = AddFirstPage(_doc, graphs, dataTable, countPages);
        
            _currentPageNumber = 1;
            while (lastProcessedRow > 0) {
                _currentPageNumber++;
                lastProcessedRow = AddNextPage(_doc, graphs, dataTable, _currentPageNumber, lastProcessedRow);
            }
        
            if (_pdfDoc.GetNumberOfPages() > Constants.MAX_PAGES_WITHOUT_CHANGELIST) {
                _currentPageNumber++;                
                AddRegisterList(_doc, graphs, _currentPageNumber, 
                                new Dictionary<string, object>() { 
                                    { Constants.AppParamDocSign, Constants.BillDocSign } 
                                });
            }

            _doc.Close();            
        }

        internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aCountPages, Dictionary<string, object> aAppParams = null) {
            SetPageMargins(aInDoc);

            // добавить таблицу с верхней дополнительной графой
            aInDoc.Add(CreateTopAppendGraph(aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(aGraphs));

            var titleBlock = CreateFirstTitleBlock(new TitleBlockStruct {
                PageSize = _pageSize, Graphs = aGraphs, Pages = aCountPages, CurrentPage = 1,  DocType = DocType.Bill, DocSign = Constants.BillDocSign
            });            
            titleBlock.SetFixedPosition(
                PdfDefines.A3Height-RIGHT_MARGIN-TITLE_BLOCK_WIDTH+LEFT_MARGIN -14.7f -5f, 
                BOTTOM_MARGIN, 
                TITLE_BLOCK_WIDTH);
            aInDoc.Add(titleBlock);

            var dataTable = CreateTable(aData, true, 0, out var lpr);
            dataTable.SetFixedPosition(
                APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f, 
                PdfDefines.A3Width - (GetTableHeight(dataTable, 1) + TOP_MARGIN),
                COLUMN_SIZES.Sum() +13.25f-5f);
            aInDoc.Add(dataTable);

            DrawLines(1);

            // добавление надписи "Утвержден ХХХВП-ЛУ"
            aGraphs.TryGetValue(Constants.GRAPH_2, out var decimal_number);            
            string inscription = $"Утвержден {decimal_number}ВП-ЛУ";
            AddInscriptionText(inscription);

            AddCopyFormatSubscription(aInDoc, 1);

            AddVerticalProjectSubscription(aInDoc, aGraphs);

            return lpr;
        }


        internal override int AddNextPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNumber, int aStartRow, Dictionary<string, object> aAppParams = null) {
            aInDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            SetPageMargins(aInDoc);
            aInDoc.Add(CreateBottomAppendGraph(aGraphs));

            var titleBlock = CreateNextTitleBlock(new TitleBlockStruct {
                PageSize = _pageSize, Graphs = aGraphs, Pages = aPageNumber, CurrentPage = aPageNumber, DocType = DocType.Bill, DocSign = Constants.BillDocSign
            });
            titleBlock.SetFixedPosition(PdfDefines.A3Height-RIGHT_MARGIN-TITLE_BLOCK_WIDTH+LEFT_MARGIN -14.8f-5f, 
                                        BOTTOM_MARGIN, 
                                        TITLE_BLOCK_WIDTH);
            aInDoc.Add(titleBlock);
            
            int lastNextProcessedRow;
            var dataTable = CreateTable(aData, false, aStartRow, out lastNextProcessedRow);
            dataTable.SetFixedPosition(APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f, 
                                       PdfDefines.A3Width - (GetTableHeight(dataTable, aPageNumber) + TOP_MARGIN),
                                       COLUMN_SIZES.Sum() + 0*mmW() +13.25f-5f);
            aInDoc.Add(dataTable);

            DrawLines(aPageNumber);
            AddCopyFormatSubscription(aInDoc, aPageNumber);
            AddVerticalProjectSubscription(aInDoc, aGraphs);

            return lastNextProcessedRow;
        }


        Table CreateTable(DataTable aData, bool firstPage, int aStartRow, out int outLastProcessedRow) {
            int COLUMNS = COLUMN_SIZES.Length;
            float CELL_HEIGHT = 8 * mmH() - 3f;

            Table tbl = new Table(UnitValue.CreatePointArray(COLUMN_SIZES));
            tbl.SetMargin(0).SetPadding(0).SetFont(f1).SetItalic().SetTextAlignment(TextAlignment.CENTER);

            Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1)
                .SetMargin(0)
                .SetPaddings(0, 0, 0, 0)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER)
                .SetFontSize(Constants.BillFontSize)
                .SetHeight(CELL_HEIGHT);
            Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
                .SetHeight(8 * PdfDefines.mmAXh)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetItalic()
                .SetFont(f1)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER)
                .SetFontSize(Constants.BillFontSize)
                .SetHeight(CELL_HEIGHT);

            AddDataTableHeader(tbl);

            var rowNumber = firstPage ? RowNumberOnFirstPage : RowNumberOnNextPage;
            outLastProcessedRow = aStartRow;

            var Rows = aData.Rows.Cast<DataRow>().ToArray();
            DataRow row;
            int inc = 0;
            for (int ind = aStartRow; ind < Rows.Length; ind++)
            {
                if (rowNumber <= 0) 
                    break;
               
                if(rowNumber == 1)
                {
                    centrAlignCell = centrAlignCell.SetBorderBottom(THICK_BORDER);
                    leftPaddCell = leftPaddCell.SetBorderBottom(THICK_BORDER);
                }

                row = Rows[ind];
                string GetCellString(string columnName) =>
                    (row[columnName] == DBNull.Value) ? string.Empty : ((BasePreparer.FormattedString)row[columnName]).Value;

                //BasePreparer.FormattedString GetCellStringFormatted(string columnName) =>
                //    (row[columnName] == DBNull.Value) ? null : ((BasePreparer.FormattedString) row[columnName]);

                string name               = GetCellString(Constants.ColumnName);//.Truncate(35);
                string productCode        = GetCellString(Constants.ColumnProductCode); // .Truncate(24);
                string deliveryDocSign    = GetCellString(Constants.ColumnDeliveryDocSign);//.Truncate(32);
                string supplier           = GetCellString(Constants.ColumnSupplier);//.Truncate(35);
                string entry              = GetCellString(Constants.ColumnEntry);//.Truncate(42);                
                string strQuantityDev     = string.Empty, strQuantityComplex = string.Empty, strQuantityReg = string.Empty;
                string strQuantityTotal   = GetCellString(Constants.ColumnQuantityTotal);//.Truncate(5);
                int quantityTotal = 0;
                if (!string.IsNullOrEmpty(strQuantityTotal))
                    quantityTotal = Int32.Parse(strQuantityTotal);
                
                if (row[Constants.ColumnQuantityDevice] != DBNull.Value)
                {
                    strQuantityDev = ((int)row[Constants.ColumnQuantityDevice]).ToString().Truncate(5);
                    int quantityComplex = (row[Constants.ColumnQuantityComplex] == DBNull.Value) ? 0 : (int)row[Constants.ColumnQuantityComplex];
                    strQuantityComplex = quantityComplex == 0 ? string.Empty : quantityComplex.ToString().Truncate(5);
                    int quantityReg = (row[Constants.ColumnQuantityRegul] == DBNull.Value) ? 0 : (int)row[Constants.ColumnQuantityRegul];
                    strQuantityReg = quantityReg == 0 ? string.Empty : quantityReg.ToString().Truncate(5);                                        
                }                

                string note             = GetCellString(Constants.ColumnFootnote).Truncate(12);

                inc++;
                if (IsEmptyRow(row))
                {
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString())));
                    AddEmptyRowToPdfTable(tbl, 1, COLUMNS - 1, leftPaddCell, rowNumber == 1);
                    rowNumber--;
                } 
                else if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(entry) && quantityTotal > 0)  // это строка с суммой по элементам
                {
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString())));                        
                        AddEmptyRowToPdfTable(tbl, 1, COLUMNS - 3, leftPaddCell, rowNumber == 1);                        
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(strQuantityTotal)).SetUnderline(1, 10.0f)); // Количество всего
                        tbl.AddCell(leftPaddCell.Clone(false)); // Примечание
                        rowNumber--;
                }
                else if (IsGroupName(row))
                {
                    if (rowNumber > 4) // если осталось мнее 5 строк для записи группы, то переходим на следующий лист
                    {
                        // если есть место для записи более 4 строк то записываем группу, иначе выходим
                        tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString())));
                        if (!string.IsNullOrEmpty(productCode))
                        {
                            tbl.AddCell(leftPaddCell.Clone(true).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(name)).SetUnderline());
                            tbl.AddCell(leftPaddCell.Clone(true).Add(new Paragraph(productCode)).SetUnderline());
                            AddEmptyRowToPdfTable(tbl, 1, COLUMNS - 3, leftPaddCell);
                        } else
                        {
                            tbl.AddCell(leftPaddCell.Clone(true).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph(name)).SetUnderline());
                            AddEmptyRowToPdfTable(tbl, 1, COLUMNS - 2, leftPaddCell);
                        }
                        rowNumber--;
                    } else
                    {
                        inc--;
                        break;
                    }
                }
                else 
                {
                    // просто запишем строку
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString())));                        
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(name)));
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(productCode)));          // Код продукции
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(deliveryDocSign)));      // Обозначение документа на поставку
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(supplier)));             // Поставщик
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(entry)));                // Куда входит (обозначение)
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(strQuantityDev)));     // Количество на изделие
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(strQuantityComplex))); // Количество в комплекты
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(strQuantityReg)));     // Количество на регулир.
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(strQuantityTotal)));   // Количество всего
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(note)));                 // Примечание
                    rowNumber--; 
                }
                outLastProcessedRow++;
            }

            // дополним таблицу пустыми строками если она не полностью заполнена
            if (rowNumber > 0) {
                for(int j = 0; j < rowNumber;j++)
                {
                    inc++;
                    var cell = centrAlignCell.Clone(false).Add(new Paragraph(inc.ToString()));
                    if (j + 1 == rowNumber)
                        cell.SetBorderBottom(THICK_BORDER);
                    tbl.AddCell(cell);                      
                    
                    AddEmptyRowToPdfTable(tbl, 1, COLUMNS-1, centrAlignCell, (j+1 == rowNumber));
                }
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

            Cell CreateCell(int rowspan=1, int colspan=1) => 
                new Cell(rowspan, colspan)
                    .SetPadding(0)
                    //.SetMargin(0)
                    .SetBorderLeft(THICK_BORDER)
                    .SetBorderRight(THICK_BORDER);

            var mainHeaderCell = new Cell(2, 1)
                .SetFontSize(MIDDLE_FONT_SIZE)
                .SetHeight(27*mmH())
                .SetPadding(0)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(THICK_BORDER);
            var mainHeaderCell11 = new Cell(1, 1)
                .SetFontSize(MIDDLE_FONT_SIZE)
                .SetPadding(0)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(THICK_BORDER);

            void AddMainHeaderCell(string text) => aTable.AddCell(mainHeaderCell.Clone(false).Add(new Paragraph(text)));

            aTable.AddCell(
                mainHeaderCell.Clone(false).Add(
                    new Paragraph("№ строки")
                        .SetTextAlignment(TextAlignment.CENTER)                        
                        .SetRotationAngle(DegreesToRadians(90))                        
                        .SetPaddingLeft(-10)
                        .SetPaddingRight(-10)
                        .SetPaddingBottom(10))                        
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            //SetFixedLeading(10)
            //.SetPadding(0)
            //.SetPaddingRight(-10)
            //

            AddMainHeaderCell("Наименование");
            AddMainHeaderCell("Код продукции");
            AddMainHeaderCell("Обозначение документа на поставку");
            AddMainHeaderCell("Поставщик");
            AddMainHeaderCell("Куда входит (обозначение)");

            aTable.AddCell(
                CreateCell(1, 4)
                    .SetBorder(THICK_BORDER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetFontSize(MIDDLE_FONT_SIZE)
                    //.SetHeight(9*mmH())
                    .Add(new Paragraph("Количество")));
            AddMainHeaderCell("Примечание");
            
            void AddSecondaryHeaderCell(string text) => aTable.AddCell(mainHeaderCell11.Clone(false).SetHeight(18*mmH()).Add(new Paragraph(text).SetFixedLeading(12f)));

            AddSecondaryHeaderCell("на из-\nделие");
            AddSecondaryHeaderCell("в ком-\nплекте");
            AddSecondaryHeaderCell("на ре-\nгулир.");
            AddSecondaryHeaderCell("всего");
        }

        void DrawLines(int aPageNumber) {
            //var pageWidth =  _pdfDoc.GetPage(aPageNumber).GetPageSize().GetWidth();
            //var leftVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2;
            var x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - 2f;
            var y = BOTTOM_MARGIN;
            
            var bottomHorizLineWidth = PdfDefines.A3Height - (x+RIGHT_MARGIN);
            DrawHorizontalLine(aPageNumber, APPEND_GRAPHS_LEFT, y, THICK_LINE_WIDTH,bottomHorizLineWidth);
        
            var rightVertLineHeight = PdfDefines.A3Width - BOTTOM_MARGIN * 2 - 10f;
            x = APPEND_GRAPHS_LEFT + APPEND_GRAPHS_WIDTH - -3.9f + bottomHorizLineWidth - 5f;
            y = BOTTOM_MARGIN+2f;
            DrawVerticalLine(aPageNumber, x, y,THICK_LINE_WIDTH, rightVertLineHeight);
        }

        void AddInscriptionText(string aText)
        {    
            Paragraph paragraph = new Paragraph(aText).
                  SetMargin(0).                  
                  SetFont(f1).
                  SetFontSize(14).
                  SetItalic();

            PdfPage page = _pdfDoc.GetPage(1);
            Canvas canvas = new Canvas(new PdfCanvas(page), page.GetMediaBox());
            canvas.ShowTextAligned(paragraph, 655, 170, TextAlignment.LEFT);
        }

        /// <summary>
        /// проверка на пустую строку
        /// </summary>
        /// <param name="aRow">a row.</param>
        /// <returns>
        ///   <c>true</c> if [is empty row] [the specified a row]; otherwise, <c>false</c>.
        /// </returns>
        bool IsEmptyRow(DataRow aRow)
        {            
            if (string.IsNullOrEmpty(aRow[Constants.ColumnName].ToString())     &&
                string.IsNullOrEmpty(aRow[Constants.ColumnSupplier].ToString()) &&
                string.IsNullOrEmpty(aRow[Constants.ColumnFootnote].ToString()) &&
                string.IsNullOrEmpty(aRow[Constants.ColumnQuantityTotal].ToString()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// проверка строки на заголовок
        /// </summary>
        /// <param name="aRow">a row.</param>
        /// <returns>
        ///   <c>true</c> if [is empty row] [the specified a row]; otherwise, <c>false</c>.
        /// </returns>
        bool IsGroupName(DataRow aRow)
        {            
            if (!string.IsNullOrEmpty(aRow[Constants.ColumnTextFormat].ToString()))
            {
                return true;
            }
            return false;
        }

    }
}
