using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Font;
using iText.Kernel.Utils;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Org.BouncyCastle.Asn1.Crmf;

using GostDOC.Common;
using GostDOC.Models;
using System.IO;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Layout;

namespace GostDOC.PDF
{
    public abstract class PdfCreator
    {

        protected static PdfFont f1 = PdfDefines.MainFont;
        protected readonly float DATA_TABLE_LEFT = 19.3f * mmW() - TO_LEFT_CORRECTION;

        public readonly DocType Type;
        
        internal readonly PageSize _pageSize;

        protected static float mmH() {
            return PdfDefines.mmAXh;
        }
        protected static float mmW() {
            return PdfDefines.mmAXw;
        }

        // DO NOT USE THEM, USE WITH NOT MM
        protected static readonly float TOP_MARGIN_MM = 5;
        protected static readonly float BOTTOM_MARGIN_MM = 5;
        protected static readonly float LEFT_MARGIN_MM = 8;
        protected static readonly float RIGHT_MARGIN_MM = 5;

        protected static readonly float TO_LEFT_CORRECTION = 0;


        protected static readonly float BOTTOM_MARGIN = BOTTOM_MARGIN_MM * mmH();
        protected static readonly float LEFT_MARGIN = LEFT_MARGIN_MM * mmW();
        protected static readonly float TOP_MARGIN = TOP_MARGIN_MM * mmH();
        protected static readonly float RIGHT_MARGIN = RIGHT_MARGIN_MM * mmW();

        protected const float THICK_LINE_WIDTH = 2f;

        protected const float THIN_LINE_WIDTH = 0.5f; 

        protected static readonly float TITLE_BLOCK_WIDTH_MM = 185;
        protected static readonly float TITLE_BLOCK_WIDTH = TITLE_BLOCK_WIDTH_MM * mmW() + TO_LEFT_CORRECTION*2;
        protected static readonly float DEFAULT_TITLE_BLOCK_CELL_HEIGHT = 5 * mmH();
        protected static readonly float TITLE_BLOCK_FIRST_PAGE_FULL_HEIGHT_MM = (15 + 5 + 5 + 15 + 8 + 14);
        protected static readonly float TITLE_BLOCK_FIRST_PAGE_WITHOUT_APPEND_HEIGHT_MM = (15 + 5 + 5 + 15);


        protected readonly float TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE = (5 + 287 - 60 * 2) * mmH();
        protected readonly float APPEND_GRAPHS_LEFT = (20 - 5 - 7) * mmW() - TO_LEFT_CORRECTION;
        protected readonly float APPEND_GRAPHS_WIDTH = (5 + 7) * mmW();

        /// <summary>
        /// количетсов строк в таблице данных на первой странице документа
        /// </summary>
        protected readonly int RowNumberOnFirstPage;
        /// <summary>
        /// количетсов строк в таблице данных на остальных страницах документа
        /// </summary>
        protected readonly int RowNumberOnNextPage;

        /// <summary>
        /// количество строк в таблице данных на листе регистрации изменений
        /// </summary>
        protected readonly int RowNumberOnChangelist = 31;

        /// <summary>
        /// поток, содержащий PDF документ
        /// </summary>
        /// <value>
        /// The main stream.
        /// </value>
        protected MemoryStream MainStream;

        /// <summary>
        /// The PDF document
        /// </summary>
        protected PdfDocument _pdfDoc;

        /// <summary>
        /// The document
        /// </summary>
        protected iText.Layout.Document _doc;

        /// <summary>
        /// The PDF writer
        /// </summary>
        protected PdfWriter _pdfWriter;

        /// <summary>
        /// текущий номер страницы
        /// </summary>
        protected int _currentPageNumber = 0;



        public PdfCreator(DocType aType)
        {            
            Type = aType;           
            switch(aType)
            {
                case DocType.Bill:
                    {
                        _pageSize = new PageSize(PageSize.A3);
                        RowNumberOnFirstPage = 26;
                        RowNumberOnNextPage = 32;
                    }
                    break;
                case DocType.D27:
                    {
                        _pageSize = new PageSize(PageSize.A3);
                        RowNumberOnFirstPage = 24;
                        RowNumberOnNextPage = 29;
                    }
                    break;
                case DocType.Specification: {
                        _pageSize = new PageSize(PageSize.A4);
                        RowNumberOnFirstPage = 24;
                        RowNumberOnNextPage = 29;
                    }
                    break;
                case DocType.ItemsList:
                    {
                        _pageSize = new PageSize(PageSize.A4);
                        RowNumberOnFirstPage = 24;
                        RowNumberOnNextPage = 29;
                    }
                    break;                
                default:
                    {
                        _pageSize = new PageSize(PageSize.A4);
                        RowNumberOnFirstPage = 26;
                        RowNumberOnNextPage = 33;
                    }
                    break;
            }
        }


        public abstract void Create(DataTable aData, IDictionary<string, string> aMainGraphs);

        public byte[] GetData()
        {
            _doc.Flush();
            _pdfWriter.Flush();
            return MainStream.ToArray();
        }
                

        protected float GetTableHeight(Table table, int pageNumber) {
            var result = table.CreateRendererSubTree().SetParent(_doc.GetRenderer()).Layout(new LayoutContext(new LayoutArea(pageNumber, new Rectangle(0, 0, PageSize.A4.GetWidth(), PageSize.A4.GetHeight()))));
            float tableHeight = result.GetOccupiedArea().GetBBox().GetHeight();
            return tableHeight;
        }

        protected void SetPageMargins(iText.Layout.Document aDoc) {
            aDoc.SetLeftMargin(0);
            aDoc.SetRightMargin(0);
            aDoc.SetTopMargin(0);
            aDoc.SetBottomMargin(0);
            return;
            aDoc.SetLeftMargin(LEFT_MARGIN);
            aDoc.SetRightMargin(RIGHT_MARGIN);
            aDoc.SetTopMargin(TOP_MARGIN);
            aDoc.SetBottomMargin(BOTTOM_MARGIN);
        }

        /// <summary>
        /// добавить к документу лист регистрации изменений
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        internal void AddRegisterList(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, int aPageNumber)
        {
            //aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            aInPdfDoc.Add(new AreaBreak(PageSize.A4));

            SetPageMargins(aInPdfDoc);

            var regTable = CreateRegisterTable();
            regTable.SetFixedPosition(
                DATA_TABLE_LEFT,
                PdfDefines.A4Height - (GetTableHeight(regTable, 1) + TOP_MARGIN) + 5.51f,
                TITLE_BLOCK_WIDTH - 0.02f);
            aInPdfDoc.Add(regTable);


            // добавить таблицу с основной надписью для последуюших старницы
            var titleBlock = CreateNextTitleBlock(new TitleBlockStruct { PageSize = _pageSize, Graphs = aGraphs, CurrentPage = aPageNumber, DocType = Type });
            titleBlock.SetFixedPosition(DATA_TABLE_LEFT, TOP_MARGIN + 4.01f,
                TITLE_BLOCK_WIDTH - 0.02f);
            aInPdfDoc.Add(titleBlock);

            // добавить таблицу с нижней дополнительной графой
            aInPdfDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

            var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH - 2f - TO_LEFT_CORRECTION;
            DrawVerticalLine(aPageNumber, fromLeft, BOTTOM_MARGIN + (8+7) * mmW()-6f, 2, 200);

            AddCopyFormatSubscription(aInPdfDoc, aPageNumber);

            AddVerticalProjectSubscription(aInPdfDoc, aGraphs);
        }

        Table CreateRegisterTable() {
            float[] columnSizes = {
                8 * mmW(), 
                20 * mmW(),
                20 * mmW(),
                20 * mmW(),
                20 * mmW(),
                20 * mmW(),
                25 * mmW(),
                25 * mmW(),
                15 * mmW(),
                12 * mmW(),
            };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

            Paragraph CreateParagraph(string text) {
                var style = new Style().SetTextAlignment(TextAlignment.CENTER).SetFontSize(12).SetFont(f1).SetPaddingTop(-2);
                return new Paragraph(text).AddStyle(style);
            }

            Cell CreateCell(int rowspan , int colspan) => new Cell(rowspan, colspan).SetBorder(THICK_BORDER).
                                                                                     SetVerticalAlignment(VerticalAlignment.MIDDLE).
                                                                                     SetHorizontalAlignment(HorizontalAlignment.CENTER);

            tbl.AddCell(new Cell(1, 10).
                SetBorder(THICK_BORDER).
                SetVerticalAlignment(VerticalAlignment.MIDDLE).
                SetHorizontalAlignment(HorizontalAlignment.CENTER).
                SetHeight(10*mmH()).
                Add(CreateParagraph("Лист регистрации изменений")));

            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Изм.")));
            tbl.AddCell(CreateCell(1,4).Add(CreateParagraph("Номера листов (страниц)")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Всего листов (страниц) в докум.")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("№ докум.")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Входящий № сопроводительного докум. и дата")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Подп.")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Дата")));

            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("измененных")));
            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("заменяемых")));
            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("новых")));
            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("аннулированных")));


            for (int i = 0; i < (RowNumberOnNextPage-4) * 10; ++i) {
                tbl.AddCell(new Cell().SetHeight(8*mmH()).SetPadding(0).SetBorderLeft(THICK_BORDER)).SetBorderRight(THICK_BORDER);
            }
            for (int i = 0; i < 10; ++i) {
                tbl.AddCell(new Cell().SetHeight(8 * mmH()).SetPadding(0).SetBorderLeft(THICK_BORDER).SetBorderRight(THICK_BORDER).SetBorderBottom(THICK_BORDER));
            }

            tbl.SetFixedPosition(19.3f * mmW(), 20 * mmW() + 3f, TITLE_BLOCK_WIDTH + 2f);

            return tbl;
        }

        /// <summary>
        /// добавить к документу первую страницу
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal abstract int AddFirstPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData,int aCountPages);

        /// <summary>
        /// добавить к документу последующие страницы
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal abstract int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNumber, int aLastProcessedRow);


        public static double DegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        protected static Border THICK_BORDER = new SolidBorder(THICK_LINE_WIDTH);

        protected static Border THIN_BORDER = new SolidBorder(THIN_LINE_WIDTH);

        //        protected static Border THICK_BORDER {
        //            return new SolidBorder(THICK_LINE_WIDTH);
        //        }

        protected static string GetGraphByName(IDictionary<string, string> aGraphs, string graph) {
            if (!aGraphs.TryGetValue(graph, out var s)) {
                s = string.Empty; 
                //TODO: log не удалось распарсить;
            }
            return s;
        }

        protected class TitleBlockStruct {
            public PageSize PageSize;
            public IDictionary<string, string> Graphs;
            public int Pages;
            public int CurrentPage;
            public bool AppendGraphs = true;
            public DocType DocType;
        }

        protected struct DataTableStruct {
            public DataTable Data;
            public bool FirstPage;
            public IDictionary<string, string> Graphs;
            public int StartRow;
        }


        string GetAdditionalGraph2(DocType aDocType) {
            string additional= "";
            switch (aDocType) {
                case DocType.Bill:
                    additional= "ВП";
                    break;
                case DocType.ItemsList:
                    additional= "ПЭ3";
                    break;
            }

            return additional;
        }

        string GetAdditionalGraph1(DocType aDocType) {
            string documentTypeGraph1 = "";
            switch (aDocType) {
                case DocType.Bill:
                    documentTypeGraph1 = "Ведомость покупных изделий";
                    break;
                case DocType.ItemsList:
                    documentTypeGraph1 = "Перечень элементов";
                    break;
            }

            return documentTypeGraph1;
        }


        /// <summary>
        /// создать таблицу основной надписи на первой странице
        /// </summary>
        /// <returns></returns>
        protected Table CreateFirstTitleBlock(TitleBlockStruct titleBlockStruct) {

            string GetGraph(string graph) {
                return GetGraphByName(titleBlockStruct.Graphs, graph);
            }
            Paragraph CreatePaddingTopParagraph(string text) => new Paragraph(text).SetPaddingTop(-2f);

            float[] columnSizes = {
                7 * mmW(), 
                10 * mmW(),
                23 * mmW(),
                15 * mmW(),
                10 * mmW(),
                14 * mmW(),
                53 * mmW(),
                3 * mmW(),
                5 * mmW(),
                5 * mmW(),
                5 * mmW(),
                15 * mmW(),
                20 * mmW(),
            };
            Table mainTable = new Table(UnitValue.CreatePointArray(columnSizes));
            mainTable.SetFont(f1).SetItalic();


            #region Left Upper (empty)

            mainTable.AddCell(new Cell(2, 5).SetBorder(Border.NO_BORDER));

            #endregion 


            #region Right Upper (additional)

            mainTable.AddCell(new Cell(1, 1).SetHeight(14*mmH()).SetBorder(THICK_BORDER));
            mainTable.AddCell(new Cell(1, 1).SetBorder(THICK_BORDER));
            mainTable.AddCell(new Cell(1,6).SetBorder(THICK_BORDER));

            mainTable.AddCell(new Cell(1,8).SetHeight(8*mmH()).SetBorder(THICK_BORDER));

            #endregion

            float leftTableCellHeight = 5 * mmH() -0.775f;
            Cell leftTableCell = new Cell(1, 1)
                .SetHeight(leftTableCellHeight)
                .SetPadding(0)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER);
            
            for (int i = 0; i < 5; ++i) {
                mainTable.AddCell(leftTableCell.Clone(false).SetBorderTop(THICK_BORDER));
            }


            #region Graph 2
            
            var graph2 = GetGraph(Constants.GRAPH_2) + GetAdditionalGraph2(titleBlockStruct.DocType);
            mainTable.AddCell(new Cell(3, 8)
                .SetBorder(THICK_BORDER).SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(CreatePaddingTopParagraph(graph2)));

            #endregion


            #region Graph 14-18

            void Add14to18Graph(string graph) {
                mainTable.AddCell(
                    leftTableCell.Clone(false)
                        .Add(CreatePaddingTopParagraph(GetGraph(graph))));
            }

            Add14to18Graph(Constants.GRAPH_14);
            Add14to18Graph(Constants.GRAPH_15);
            Add14to18Graph(Constants.GRAPH_16);
            Add14to18Graph(Constants.GRAPH_17);
            Add14to18Graph(Constants.GRAPH_18);

            #endregion


            Table AddLeftUpperCell(string text) => 
                mainTable.AddCell(leftTableCell.Clone(false)
                    .SetBorderTop(THICK_BORDER)
                    .SetBorderBottom(THICK_BORDER)
                    .Add(CreatePaddingTopParagraph(text)));

            AddLeftUpperCell("Изм.");
            AddLeftUpperCell("Лист");
            AddLeftUpperCell("№ Докум");
            AddLeftUpperCell("Подп.");
            AddLeftUpperCell("Дата");


            Cell leftTableCell2Column = new Cell(1, 2)
                .SetHeight(leftTableCellHeight)
                .SetPadding(0)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetBorderLeft(THICK_BORDER)
                .SetBorderRight(THICK_BORDER);

            Table AddCellWithText(Cell cell, string text) => mainTable.AddCell(cell.Clone(false).Add(CreatePaddingTopParagraph(text)));


            #region Разраб.

            var graph11Cell = leftTableCell.Clone(false).SetTextAlignment(TextAlignment.LEFT);
            AddCellWithText(leftTableCell2Column, "Разраб.");
            string dev_str = (Type == DocType.Specification) ? GetGraph(Constants.GRAPH_11sp_dev) : GetGraph(Constants.GRAPH_11bl_dev);
            mainTable.AddCell(graph11Cell.Clone(false).Add(CreatePaddingTopParagraph(dev_str)));
            for (int i = 0; i < 2; ++i) {
                mainTable.AddCell(leftTableCell.Clone(false));
            }
            
            #endregion



            #region Graph 1

            string documentTypeGraph1 = GetAdditionalGraph1(titleBlockStruct.DocType);
            var graph1Cell = new Cell(5, 3)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(THICK_BORDER).Add(new Paragraph(GetGraph(Constants.GRAPH_1)).SetFontSize(20));
            if (!string.IsNullOrEmpty(documentTypeGraph1)) {
                graph1Cell.Add(new Paragraph(documentTypeGraph1).SetFontSize(12));
            }
            mainTable.AddCell(graph1Cell);

            #endregion

             
            mainTable.AddCell(new Cell(1, 3).SetPadding(0).Add(CreatePaddingTopParagraph("Лит.")).SetTextAlignment(TextAlignment.CENTER).SetBorder(THICK_BORDER));
            mainTable.AddCell(new Cell(1, 1).SetPadding(0).Add(CreatePaddingTopParagraph("Лист")).SetTextAlignment(TextAlignment.CENTER).SetBorder(THICK_BORDER));
            mainTable.AddCell(new Cell(1, 1).SetPadding(0).Add(CreatePaddingTopParagraph("Листов")).SetTextAlignment(TextAlignment.CENTER).SetBorder(THICK_BORDER));


            #region Пров.

            AddCellWithText(leftTableCell2Column, "Пров.");
            string chk_str = (Type == DocType.Specification) ? GetGraph(Constants.GRAPH_11sp_chk) : GetGraph(Constants.GRAPH_11bl_chk);
            mainTable.AddCell(graph11Cell.Clone(false).Add(CreatePaddingTopParagraph(chk_str)));
            for (int i = 0; i < 2; ++i) {
                mainTable.AddCell(leftTableCell.Clone(false));
            }
           
            #endregion


            #region Graph 4, 7, 8

            var fullyBorderCell = leftTableCell.Clone(false).SetBorder(THICK_BORDER);
            mainTable.AddCell(fullyBorderCell.Clone(false).Add(CreatePaddingTopParagraph(GetGraph(Constants.GRAPH_4))));
            mainTable.AddCell(fullyBorderCell.Clone(false).Add(CreatePaddingTopParagraph(GetGraph(Constants.GRAPH_4a))));
            mainTable.AddCell(fullyBorderCell.Clone(false).Add(CreatePaddingTopParagraph(GetGraph(Constants.GRAPH_4b))));
            mainTable.AddCell(fullyBorderCell.Clone(false).Add(CreatePaddingTopParagraph(titleBlockStruct.CurrentPage.ToString())));
            mainTable.AddCell(fullyBorderCell.Clone(false).Add(CreatePaddingTopParagraph(titleBlockStruct.Pages.ToString())));
            
            #endregion
           

            // graph 10-13
            mainTable.AddCell(leftTableCell2Column);
            for (int i = 0; i < 3; ++i) {
                mainTable.AddCell(leftTableCell.Clone(false));
            }


            #region Graph 9

            mainTable.AddCell(new Cell(3, 5).SetBorder(THICK_BORDER).Add(CreatePaddingTopParagraph(GetGraph(Constants.GRAPH_9))));

            #endregion


            #region Н. контр.

            AddCellWithText(leftTableCell2Column,"Н. контр.");
            mainTable.AddCell(graph11Cell.Clone(false).Add(CreatePaddingTopParagraph(GetGraph(Constants.GRAPH_11norm))));
            for (int i = 0; i < 2; ++i) {
                mainTable.AddCell(leftTableCell.Clone(false));
            }

            #endregion


            #region Утв.

            AddCellWithText(leftTableCell2Column.Clone(false).SetBorderBottom(THICK_BORDER),"Утв.");
            mainTable.AddCell(graph11Cell.Clone(false).SetBorderBottom(THICK_BORDER).Add(CreatePaddingTopParagraph(GetGraph(Constants.GRAPH_11affirm))));
            for (int i = 0; i < 2; ++i) {
                mainTable.AddCell(leftTableCell.Clone(false).SetBorderBottom(THICK_BORDER));
            }

            #endregion

            if (titleBlockStruct.PageSize.Contains(PageSize.A3)) {
            }
            else {
                // A4
                var left = 20 * mmW() - 2f - TO_LEFT_CORRECTION;
                mainTable.SetFixedPosition(left, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH/*+TO_LEFT_CORRECTION*/);
            }

            return mainTable;
        }

        protected void DrawHorizontalLine(int aPageNumber, float x, float y, float aWidth, float aLength) {
            Canvas canvas = new Canvas(new PdfCanvas(_pdfDoc.GetPage(aPageNumber)),new Rectangle(x, y, aLength, aWidth));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(aLength));
        }

        protected void DrawVerticalLine(int aPageNumber, float x, float y, float aWidth, float aLength) {
            var canvas = new Canvas(new PdfCanvas(_pdfDoc.GetPage(aPageNumber)),new Rectangle(x, y, aWidth,aLength));
            canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(aLength).SetRotationAngle(DegreesToRadians(90)));
        }
        
        /// <summary>
        /// создать таблицу основной надписи на последующих страницах
        /// </summary>
        /// <returns></returns>
        protected Table CreateNextTitleBlock(TitleBlockStruct titleBlockStruct) {
            var aGraphs = titleBlockStruct.Graphs;
            var aPageSize = titleBlockStruct.PageSize;   

            float[] columnSizes = {
                7.5f * mmW(), 
                10 * mmW(),
                23 * mmW(),
                15 * mmW(),
                10 * mmW(),
                110 * mmW(),
                10 * mmW(),
            };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

            var titleBlockHeightCell = new Cell().SetHeight(DEFAULT_TITLE_BLOCK_CELL_HEIGHT).SetPadding(0).SetBorderRight(THICK_BORDER);
            Cell CreateCell() {
                return new Cell().SetHeight(DEFAULT_TITLE_BLOCK_CELL_HEIGHT).SetPadding(0).SetBorderRight(THICK_BORDER);
            }
            Paragraph CreateParagraph(string text) {
                return new Paragraph(text).SetItalic().SetPaddingTop(-2).SetFontSize(12).SetFont(f1).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            }

            for (int i = 0; i < 5; ++i) {
                tbl.AddCell(CreateCell().SetBorderTop(THICK_BORDER).SetBorderLeft(THICK_BORDER));
            }

            var graph2 = GetGraphByName(aGraphs, Constants.GRAPH_2) + GetAdditionalGraph2(titleBlockStruct.DocType);            
            tbl.AddCell(new Cell(3, 1).
                Add(new Paragraph(graph2).SetFont(f1).SetItalic().SetFontSize(20).SetTextAlignment(TextAlignment.CENTER)).
                SetVerticalAlignment(VerticalAlignment.MIDDLE).
                SetBorderTop(THICK_BORDER).
                SetBorderRight(THICK_BORDER).
                SetBorderBottom(THICK_BORDER));

            var rightestCell = new Cell(3, 1).
                SetPadding(0).
                SetBorderTop(THICK_BORDER).
                SetBorderRight(THICK_BORDER).
                SetBorderBottom(THICK_BORDER);

            var rightestCellTable = new Table(UnitValue.CreatePercentArray(new[] {1f})).UseAllAvailableWidth();
            rightestCellTable.AddCell(
                new Cell().
                    SetHeight(7*mmH()).
                    SetBorderLeft(Border.NO_BORDER).
                    SetBorderRight(Border.NO_BORDER).
                    SetBorderTop(Border.NO_BORDER).
                    SetBorderBottom(THICK_BORDER).
                    SetPadding(0).
                    Add(CreateParagraph("Лист").SetPaddingTop(2).SetPaddingLeft(2)));
            rightestCellTable.AddCell(
                new Cell().
                    SetHeight(8*mmH()).
                    SetBorderLeft(Border.NO_BORDER).
                    SetBorderRight(Border.NO_BORDER).
                    SetBorderBottom(Border.NO_BORDER).
                    SetPadding(0).
                    Add(CreateParagraph(titleBlockStruct.CurrentPage.ToString())
                        .SetPaddingTop(2)
                        .SetTextAlignment(TextAlignment.CENTER))); 

            rightestCell.Add(rightestCellTable);
            tbl.AddCell(rightestCell);

            void AddGraphCell(string text, bool bottomBorder=false) {
                var c = CreateCell().Add(CreateParagraph(text).SetTextAlignment(TextAlignment.CENTER));
                if (bottomBorder) {
                    c.SetBorderBottom(THICK_BORDER);
                }
                tbl.AddCell(c);
            }

            void AddGraphCell2(Cell cell, string text ) {
                tbl.AddCell(cell.Clone(false).Add(CreateParagraph(text).SetTextAlignment(TextAlignment.CENTER)).SetBorderLeft(THICK_BORDER));
            }
            var topAndBottomBorderCell = titleBlockHeightCell.Clone(false).SetBorderBottom(THICK_BORDER).SetBorderTop(THICK_BORDER);

            //AddGraphCell();
            AddGraphCell2(topAndBottomBorderCell.SetBorderTop(Border.NO_BORDER).SetBorderBottom(THIN_BORDER), GetGraphByName(aGraphs, Constants.GRAPH_14));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_15));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_16));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_17));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_18));

            
            AddGraphCell2(topAndBottomBorderCell.SetBorderBottom(THICK_BORDER).SetBorderTop(THICK_BORDER), "Изм.");
            AddGraphCell2(topAndBottomBorderCell.SetBorderBottom(THICK_BORDER).SetBorderTop(THICK_BORDER), "Лист");
            AddGraphCell2(topAndBottomBorderCell.SetBorderBottom(THICK_BORDER).SetBorderTop(THICK_BORDER), "№ докум.");
            AddGraphCell2(topAndBottomBorderCell.SetBorderBottom(THICK_BORDER).SetBorderTop(THICK_BORDER), "Подп.");
            AddGraphCell2(topAndBottomBorderCell.SetBorderBottom(THICK_BORDER).SetBorderTop(THICK_BORDER), "Дата");


            // switch A3/A4
            if (aPageSize.Contains(PageSize.A3)) {
                //tbl.SetFixedPosition(PdfDefines.A3Height - TITLE_BLOCK_WIDTH - RIGHT_MARGIN, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH);
            } else {
                float left = 20 * mmW()-(TO_LEFT_CORRECTION+1f);
                tbl.SetFixedPosition(left, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH-1f);
            }

            return tbl;
        }
 
        protected static Cell CreateAppendGraphCell(float height, string text = null) {
            var c = new Cell();
            if (text != null) {
                c.Add(
                    new Paragraph(text)
                        .SetFont(f1)
                        .SetFontSize(12)
                        .SetRotationAngle(DegreesToRadians(90))
                        .SetFixedLeading(10)
                        .SetPadding(0)
                        .SetPaddingRight(-10)
                        .SetPaddingLeft(-10)
                        .SetMargin(0)
                        .SetItalic()
                        .SetWidth(height)
                        .SetTextAlignment(TextAlignment.CENTER));
            }

            c.SetHorizontalAlignment(HorizontalAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetMargin(0)
                .SetPadding(0).SetHeight(height).SetBorder(new SolidBorder(2));

            return c;
        }

        protected static Cell CreateAppendGraphWideTextCell(float height, string text = null)
        {
            var c = new Cell();
            if (text != null)
            {
                c.Add(
                    new Paragraph(text)
                        .SetFont(f1)
                        .SetFontSize(14)
                        .SetRotationAngle(DegreesToRadians(90))
                        .SetFixedLeading(10)
                        .SetPadding(0)
                        .SetPaddingTop(3)
                        .SetPaddingRight(-10)
                        .SetPaddingLeft(-10)
                        .SetMargin(0)
                        .SetItalic()
                        .SetWidth(height)
                        .SetTextAlignment(TextAlignment.CENTER));
            }

            c.SetHorizontalAlignment(HorizontalAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetMargin(0)
                .SetPadding(0).SetHeight(height).SetBorder(new SolidBorder(2));

            return c;
        }

        /// <summary>
        /// создать таблицу для верхней дополнительной графы
        /// </summary>
        /// <returns></returns>
        protected Table CreateTopAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs) {
        float[] columnSizes = {5 * mmW(), 7 * mmW()};
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

            tbl.AddCell(CreateAppendGraphCell(60 * mmW(), "Перв. примен."));            
            tbl.AddCell(CreateAppendGraphWideTextCell(60 * mmW(), GetGraphByName(aGraphs, Constants.GRAPH_25)));

            tbl.AddCell(CreateAppendGraphCell(60 * mmW(), "Справ. №"));
            tbl.AddCell(CreateAppendGraphCell(60 * mmW()));

            tbl.SetFixedPosition(
                APPEND_GRAPHS_LEFT, 
                PdfDefines.A4Height - (TOP_MARGIN + GetTableHeight(tbl, 1)) /*+ 5.5f*/,
                // TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE, 
                APPEND_GRAPHS_WIDTH);

            return tbl;
        }

        /// <summary>
        /// создать таблицу для нижней дополнительной графы
        /// </summary>
        /// <returns></returns>
        protected Table CreateBottomAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs) { 
            float[] columnSizes = {5 * mmW(), 7 * mmW()};
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

            tbl.AddCell(CreateAppendGraphCell(35 * mmW(), "Подп. и дата"));
            tbl.AddCell(CreateAppendGraphCell(35 * mmW()));

            tbl.AddCell(CreateAppendGraphCell(25 * mmW(), "Инв. № дубл."));
            tbl.AddCell(CreateAppendGraphCell(25 * mmW()));

            tbl.AddCell(CreateAppendGraphCell(25 * mmW(), "Взам. инв. №"));
            tbl.AddCell(CreateAppendGraphCell(25 * mmW()));

            tbl.AddCell(CreateAppendGraphCell(35 * mmW(), "Подп. и дата").SetHeight(35 * mmW()));
            tbl.AddCell(CreateAppendGraphCell(35 * mmW()));

            tbl.AddCell(CreateAppendGraphCell(25 * mmW(), "Инв № подл."));
            tbl.AddCell(CreateAppendGraphCell(25 * mmW() + 2f));
       
            tbl.SetFixedPosition(APPEND_GRAPHS_LEFT, BOTTOM_MARGIN, APPEND_GRAPHS_WIDTH);

            return tbl;
        }


        protected void AddVerticalProjectSubscription(iText.Layout.Document aInDoc, IDictionary<string, string> aGraphs)
        {
            var style = new Style().SetItalic().SetFontSize(12).SetFont(f1).SetTextAlignment(TextAlignment.CENTER);

            var p =
                new Paragraph(GetGraphByName(aGraphs, Constants.GRAPH_PROJECT))
                    .SetRotationAngle(DegreesToRadians(90))
                    .AddStyle(style)
                    .SetFixedPosition(10 * mmW() + 2 - TO_LEFT_CORRECTION, TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE + 45 * mmW(), 100);
            aInDoc.Add(p);
        }


        protected void AddCopyFormatSubscription(iText.Layout.Document aInDoc, int aPageNumber)
        {
            var style = new Style().SetItalic().SetFontSize(12).SetFont(f1).SetTextAlignment(TextAlignment.CENTER);

            float bottom = 0;
            float left = 0;
            float next_left = 0;
            string text = string.Empty;

            var page = _pdfDoc.GetPage(aPageNumber);
            var size = page.GetPageSize();

            if (size.GetWidth() == PageSize.A4.GetWidth())
            {
                bottom = /*-2*/0;
                left = (7 + 10 + 32 + 15 + 10 + 14) * mmW() - TO_LEFT_CORRECTION;
                next_left = (7 + 10 + 32 + 15 + 10 + 70) * mmW() + 20 - TO_LEFT_CORRECTION;
                text = "Формат А4";
            }
            else
            {
                bottom = 0;
                left = (60 + 45 + 70 + 50 + 65) * mmW() - TO_LEFT_CORRECTION;
                next_left = (60 + 45 + 70 + 50 + 32 + 100) * mmW() + 20 - TO_LEFT_CORRECTION;
                text = "Формат А3";
            }

            var p = new Paragraph("Копировал").AddStyle(style).SetFixedPosition(left, bottom, 100);
            aInDoc.Add(p);

            p = new Paragraph(text).AddStyle(style).SetFixedPosition(next_left, bottom, 100);
            aInDoc.Add(p);
        }

        /// <summary>
        /// добавить пустые строки в таблицу PDF
        /// </summary>
        /// <param name="aTable">таблица PDF</param>
        /// <param name="aRows">количество пустых строк которые надо добавить</param>
        /// <param name="aColumns">количество столбцов в таблице</param>
        /// <param name="aTemplateCell">шаблон ячейки</param>
        /// <param name="aLastRowIsFinal">признак, что последняя строка - это последняя строка таблицы</param>
        protected void AddEmptyRowToPdfTable(Table aTable, int aRows, int aColumns, Cell aTemplateCell,
            bool aLastRowIsFinal = false) {
            int bodyRowsCnt = aRows - 1;
            for (int i = 0; i < bodyRowsCnt * aColumns; i++) {
                aTable.AddCell(aTemplateCell.Clone(false));
            }

            float borderWidth = (aLastRowIsFinal) ? THICK_LINE_WIDTH : 1;
            for (int i = 0; i < aColumns; i++) {
                aTable.AddCell(aTemplateCell.Clone(false).SetBorderBottom(new SolidBorder(borderWidth)));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aRowspan"></param>
        /// <param name="aColspan"></param>
        /// <param name="aLeftBorder"></param>
        /// <param name="aRightBorder"></param>
        /// <param name="aTopBorder"></param>
        /// <param name="aBottomBorder"></param>
        /// <returns></returns>
        protected Cell CreateEmptyCell(
                int aRowspan, int aColspan,
                int aLeftBorder = (int) THICK_LINE_WIDTH,
                int aRightBorder = (int) THICK_LINE_WIDTH,
                int aTopBorder = (int) THICK_LINE_WIDTH,
                int aBottomBorder = (int) THICK_LINE_WIDTH) {

            Cell cell = new Cell(aRowspan, aColspan);
            cell.SetVerticalAlignment(VerticalAlignment.MIDDLE).SetHorizontalAlignment(HorizontalAlignment.CENTER);

            cell.SetBorderBottom(aBottomBorder == 0 ? Border.NO_BORDER : new SolidBorder(aBottomBorder));
            cell.SetBorderTop(aTopBorder == 0 ? Border.NO_BORDER : new SolidBorder(aTopBorder));
            cell.SetBorderLeft(aLeftBorder == 0 ? Border.NO_BORDER : new SolidBorder(aLeftBorder));
            cell.SetBorderRight(aRightBorder == 0 ? Border.NO_BORDER : new SolidBorder(aRightBorder));

            return cell;
        }        
    }
}
