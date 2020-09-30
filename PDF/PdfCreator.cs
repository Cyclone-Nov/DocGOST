﻿using System;
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

namespace GostDOC.PDF
{
    public abstract class PdfCreator
    {

        protected static PdfFont f1 = PdfDefines.MainFont;

        /// <summary>
        /// The save path
        /// </summary>
        public readonly string SavePath;

        public readonly DocType Type;
        
        internal readonly PageSize _pageSize;

        protected static float mmH() {
            return PdfDefines.mmAXh;
        }
        protected static float mmW() {
            return PdfDefines.mmAXw;
        }

        protected static readonly float TOP_MARGIN_MM = 5;
        protected static readonly float BOTTOM_MARGIN_MM = 5;
        protected static readonly float LEFT_MARGIN_MM = 8;
        protected static readonly float RIGHT_MARGIN_MM = 5;

        protected static readonly float BOTTOM_MARGIN = BOTTOM_MARGIN_MM * mmH();
        protected static readonly float LEFT_MARGIN = LEFT_MARGIN_MM * mmW();
        protected static readonly float TOP_MARGIN = TOP_MARGIN_MM * mmH();
        protected static readonly float RIGHT_MARGIN = RIGHT_MARGIN_MM * mmW();

        protected const float THICK_LINE_WIDTH = 2f; 
        
        protected static readonly float TITLE_BLOCK_WIDTH_MM = 185;
        protected static readonly float TITLE_BLOCK_WIDTH = TITLE_BLOCK_WIDTH_MM * mmW();
        protected static readonly float DEFAULT_TITLE_BLOCK_CELL_HEIGHT = 5 * mmH();
        protected static readonly float TITLE_BLOCK_FIRST_PAGE_FULL_HEIGHT_MM = (15 + 5 + 5 + 15 + 8 + 14);
        protected static readonly float TITLE_BLOCK_FIRST_PAGE_WITHOUT_APPEND_HEIGHT_MM = (15 + 5 + 5 + 15);


        protected readonly float TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE = (5 + 287 - 60 * 2) * mmW();
        protected readonly float APPEND_GRAPHS_LEFT = (20 - 5 - 7) * mmW();
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
                        RowNumberOnFirstPage = 24;
                        RowNumberOnNextPage = 29;
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
                        RowNumberOnNextPage = 31;
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
                

        protected void SetPageMargins(iText.Layout.Document aDoc) {
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
            aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            SetPageMargins(aInPdfDoc);

            aInPdfDoc.Add(CreateRegisterTable());

            // добавить таблицу с основной надписью для последуюших старницы
            aInPdfDoc.Add(CreateNextTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs}));

            // добавить таблицу с нижней дополнительной графой
            aInPdfDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));
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
                var style = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFontSize(12).SetFont(f1).SetPaddingTop(-2);
                return new Paragraph(text).AddStyle(style);
            }

            Cell CreateCell(int rowspan , int colspan) => new Cell(rowspan, colspan).SetBorder(CreateThickBorder());

            tbl.AddCell(new Cell(1, 10).
                SetBorder(CreateThickBorder()).
                SetHeight(10*mmH()).
                Add(CreateParagraph("Лист регистрации изменений")));

            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Изм.")));
            tbl.AddCell(CreateCell(1,4).Add(CreateParagraph("Номера листов (страниц)")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Всего листов (страниц) в докум.")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("№ докум.")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Входящий № сопроводительного докум. и дата")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Подп.")));
            tbl.AddCell(CreateCell(2,1).Add(CreateParagraph("Дата")));

            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("Измененных")));
            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("Заменяемых")));
            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("Новых")));
            tbl.AddCell(CreateCell(1,1).Add(CreateParagraph("Аннулированных")));


            for (int i = 0; i < (RowNumberOnNextPage-4) * 10; ++i) {
                tbl.AddCell(new Cell().SetHeight(8*mmH()).SetPadding(0).SetBorderLeft(CreateThickBorder())).SetBorderRight(CreateThickBorder());
            }
            for (int i = 0; i < 10; ++i) {
                tbl.AddCell(new Cell().SetHeight(8 * mmH()).SetPadding(0).SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder()).SetBorderBottom(CreateThickBorder()));
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

        protected static Border CreateThickBorder() {
            return new SolidBorder(THICK_LINE_WIDTH);
        }
        
        protected static string GetGraphByName(IDictionary<string, string> aGraphs, string graph) {
            if (!aGraphs.TryGetValue(graph, out var s)) {
                s = string.Empty; 
                //TODO: log не удалось распарсить;
            }
            return s;
        }

        protected struct TitleBlockStruct {
            public PageSize PageSize;
            public IDictionary<string, string> Graphs;
            public int Pages;
            public int CurrentPage;
            public bool AppendGraphs;

        }

        protected struct DataTableStruct {
            public DataTable Data;
            public bool FirstPage;
            public IDictionary<string, string> Graphs;
            public int StartRow;
        }

        /// <summary>
        /// создать таблицу основной надписи на первой странице
        /// </summary>
        /// <returns></returns>
        protected Table CreateFirstTitleBlock(TitleBlockStruct titleBlockStruct) {
            var aGraphs = titleBlockStruct.Graphs;
            var aPageSize = titleBlockStruct.PageSize;        

            float[] columnSizes = {65 * mmW(), 120 * mmW()};
            Table mainTable = new Table(UnitValue.CreatePointArray(columnSizes));

            Cell CreateMainTableCell() {
                return new Cell().SetBorder(Border.NO_BORDER).SetMargin(0).SetPadding(0);
            }


            string GetGraph(string graph) {
                return GetGraphByName(aGraphs, graph);
            }

            #region Пустая ячейка слева

            mainTable.AddCell(CreateMainTableCell());

            #endregion

            #region Правая верхняя таблица (доп. графы)

            if (titleBlockStruct.AppendGraphs) {
                var rightTopTable = new Table(UnitValue.CreatePointArray(new[] {
                    14 * mmW(),
                    53 * mmW(),
                    53 * mmW(),
                }));

                float rightTopTableCellHeight1 = 14 * mmH();
                float rightTopTableCellHeight2 = 8 * mmH();

                Cell CreateRightTopTableCell(float height, int rowspan = 1, int colspan = 1) {
                    return new Cell(rowspan, colspan).SetHeight(height).SetBorder(CreateThickBorder());
                }

                void Add27to29Graph(string graph) {
                    rightTopTable.AddCell(CreateRightTopTableCell(rightTopTableCellHeight1)
                        .Add(new Paragraph(GetGraph(graph))));
                }

                Add27to29Graph(Constants.GRAPH_27);
                Add27to29Graph(Constants.GRAPH_28);
                Add27to29Graph(Constants.GRAPH_29);

                rightTopTable.AddCell(CreateRightTopTableCell(rightTopTableCellHeight2, 1, 3)
                    .Add(new Paragraph(GetGraph(Constants.GRAPH_30))).SetBorderBottom(Border.NO_BORDER));

                mainTable.AddCell(CreateMainTableCell().Add(rightTopTable).SetPaddingLeft(-2).SetPaddingRight(-5));
            } else {
                mainTable.AddCell(CreateMainTableCell());
            }

            #endregion

            #region Левая таблица

            var leftTableTextStyle =
                new Style().SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFontSize(12).SetFont(f1);
            var leftTable = new Table(UnitValue.CreatePointArray(new[] {
                7 * mmW(),
                10 * mmW(),
                23 * mmW(),
                15 * mmW(),
                10 * mmW()
            })).AddStyle(leftTableTextStyle);
            float leftTableCellHeight = 5 * mmH();

            Cell CreateLeftTableCell(int rowspan = 1, int colspan = 1) {
                return new Cell(rowspan, colspan).SetHeight(leftTableCellHeight).SetPadding(0);
            }

            for (int i = 0; i < 5; ++i) {
                leftTable.AddCell(CreateLeftTableCell()
                    .SetBorderLeft(CreateThickBorder())
                    .SetBorderRight(CreateThickBorder())
                    .SetBorderTop(CreateThickBorder()));
            }

            void Add14to18Graph(string graph) {
                leftTable.AddCell(
                    CreateLeftTableCell()
                        .Add(new Paragraph(GetGraph(graph)))
                        .SetBorderLeft(CreateThickBorder())
                        .SetBorderRight(CreateThickBorder()));
            }

            Add14to18Graph(Constants.GRAPH_14);
            Add14to18Graph(Constants.GRAPH_15);
            Add14to18Graph(Constants.GRAPH_16);
            Add14to18Graph(Constants.GRAPH_17);
            Add14to18Graph(Constants.GRAPH_18);

            Paragraph CreateLeftTableTopParagraph(string text) {
                var style = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFontSize(12).SetFont(f1)
                    .SetPaddingTop(-2);
                return new Paragraph(text).AddStyle(style);
            }

            void AddToTopLeftTable(string text) {
                leftTable.AddCell(
                    CreateLeftTableCell()
                        .SetBorderRight(CreateThickBorder())
                        .SetBorderLeft(CreateThickBorder())
                        .Add(CreateLeftTableTopParagraph(text)));
            }

            AddToTopLeftTable("Изм.");
            AddToTopLeftTable("Лист");
            AddToTopLeftTable("№ Докум.");
            AddToTopLeftTable("Подп.");
            AddToTopLeftTable("Дата");

            Paragraph CreateLeftTableBottomParagraph(string text) {
                var style = new Style().SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFontSize(12).SetFont(f1)
                    .SetPaddingTop(-2);
                return new Paragraph(text).AddStyle(style);
            }

            void SetThickBorder(Cell c, bool topBorder, bool bottomBorder) {
                if (topBorder) {
                    c.SetBorderTop(CreateThickBorder());
                }

                if (bottomBorder) {
                    c.SetBorderBottom(CreateThickBorder());
                }
            }

            void AddToBottomLeftTable(string text, string graph, bool topBorder = false, bool bottomBorder = false) {
                var c = CreateLeftTableCell(1, 2)
                    .SetBorderLeft(CreateThickBorder())
                    .SetBorderRight(CreateThickBorder())
                    .Add(CreateLeftTableBottomParagraph(text));
                SetThickBorder(c, topBorder, bottomBorder);
                leftTable.AddCell(c);

                c = CreateLeftTableCell()
                    .SetBorderRight(CreateThickBorder())
                    .Add(CreateLeftTableBottomParagraph(GetGraph(graph)));
                SetThickBorder(c, topBorder, bottomBorder);
                leftTable.AddCell(c);

                for (int i = 0; i < 2; ++i) {
                    c = CreateLeftTableCell().SetBorderRight(CreateThickBorder());
                    SetThickBorder(c, topBorder, bottomBorder);
                    leftTable.AddCell(c);
                }
            }


            AddToBottomLeftTable("Разраб.", Constants.GRAPH_11sp_dev, true);
            AddToBottomLeftTable("Пров.", Constants.GRAPH_11sp_chk);


            leftTable.AddCell(CreateLeftTableCell(1, 2)
                .SetBorderLeft(CreateThickBorder())
                .SetBorderRight(CreateThickBorder())
                .Add(CreateLeftTableBottomParagraph(GetGraph(Constants.GRAPH_10))));
            leftTable.AddCell(CreateLeftTableCell().SetBorderRight(CreateThickBorder())
                .Add(CreateLeftTableBottomParagraph(GetGraph(Constants.GRAPH_11))));
            leftTable.AddCell(CreateLeftTableCell().SetBorderRight(CreateThickBorder())
                .Add(CreateLeftTableBottomParagraph(GetGraph(Constants.GRAPH_12))));
            leftTable.AddCell(CreateLeftTableCell().SetBorderRight(CreateThickBorder())
                .Add(CreateLeftTableBottomParagraph(GetGraph(Constants.GRAPH_13))));

            AddToBottomLeftTable("Н. контр", Constants.GRAPH_11norm);
            AddToBottomLeftTable("Утв.", Constants.GRAPH_11affirm, false, true);

            mainTable.AddCell(CreateMainTableCell().Add(leftTable));

            #endregion

            #region Правая нижняя таблица

            var rightBottomTable = new Table(UnitValue.CreatePercentArray(new[] {1f})).UseAllAvailableWidth();
            rightBottomTable.AddCell(
                new Cell().Add(
                        new Paragraph(GetGraph(Constants.GRAPH_2)).AddStyle(
                            new Style().SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1).SetMarginLeft(20)
                                .SetFontSize(20))).SetHeight(15 * mmH()).SetPaddings(0, 0, 1, 0)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetBorderLeft(Border.NO_BORDER)
                    .SetBorderBottom(Border.NO_BORDER).SetBorderTop(CreateThickBorder())
                    .SetBorderRight(CreateThickBorder()));

            var innerRightBottomTable =
                new Table(UnitValue.CreatePointArray(new[] {
                    (53 * 2 + 14 - 50) * mmW(),
                    50 * mmW(),
                }));

            var textStyle = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1);

            innerRightBottomTable.AddCell(
                new Cell().AddStyle(textStyle).SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER)
                    .SetBorderTop(CreateThickBorder()).SetBorderBottom(Border.NO_BORDER).SetPaddings(-1, 0, 0, 0)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE).Add(new Paragraph(GetGraph(Constants.GRAPH_1))));

            var tableGraph4789 =
                new Table(UnitValue.CreatePointArray(new[] {
                    5 * mmW(),
                    5 * mmW(),
                    5 * mmW(),
                    15 * mmW(),
                    20 * mmW(),
                }));

            Cell CreateTableGraph478Cell(int colspan = 1, int rowspan = 1, bool borderTop = false,
                bool borderLeft = false, bool borderBottom = false) {
                var height = 5 * mmH();
                var c = new Cell(colspan, rowspan).SetHeight(height).SetPadding(0).SetBorderRight(CreateThickBorder());
                if (borderTop) {
                    c.SetBorderTop(CreateThickBorder());
                }

                if (borderLeft) {
                    c.SetBorderLeft(CreateThickBorder());
                }

                if (borderBottom) {
                    c.SetBorderBottom(CreateThickBorder());
                }

                return c;
            }

            Paragraph CreateTableGraph478Paragraph(string text) {
                var style = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFontSize(12).SetFont(f1)
                    .SetPaddingTop(-2);
                return new Paragraph(text).AddStyle(style);
            }

            tableGraph4789.AddCell(CreateTableGraph478Cell(1, 3, borderTop: true, borderLeft: true, borderBottom: true)
                .Add(CreateTableGraph478Paragraph("Лит.")));
            tableGraph4789.AddCell(CreateTableGraph478Cell(borderTop: true, borderBottom: true)
                .Add(CreateTableGraph478Paragraph("Лист")));
            tableGraph4789.AddCell(CreateTableGraph478Cell(borderTop: true, borderBottom: true)
                .Add(CreateTableGraph478Paragraph("Листов")));

            tableGraph4789.AddCell(CreateTableGraph478Cell(borderLeft: true));
            tableGraph4789.AddCell(CreateTableGraph478Cell(borderLeft: true)
                .Add(CreateTableGraph478Paragraph(GetGraph(Constants.GRAPH_4))));
            tableGraph4789.AddCell(CreateTableGraph478Cell(borderLeft: true));
            tableGraph4789.AddCell(CreateTableGraph478Cell(borderLeft: true)
                .Add(CreateTableGraph478Paragraph("1")));                
            tableGraph4789.AddCell(CreateTableGraph478Cell(borderLeft: true)
                .Add(CreateTableGraph478Paragraph(""/*titleBlockStruct.Pages.ToString()*/)));

            tableGraph4789.AddCell(
                new Cell(1, 5).SetHeight(15 * mmH() - 2).SetPaddings(0, 0, 0, 0)
                    .SetBorderLeft(CreateThickBorder()).SetBorderRight(CreateThickBorder())
                    .SetBorderTop(CreateThickBorder()).SetBorderBottom(CreateThickBorder())
                    .Add(CreateTableGraph478Paragraph(GetGraph(Constants.GRAPH_9))));

            innerRightBottomTable.AddCell(new Cell().Add(tableGraph4789).SetBorder(Border.NO_BORDER)
                .SetPaddings(-1, -1, 0, 0));

            rightBottomTable.AddCell(new Cell().SetPadding(0).SetBorder(Border.NO_BORDER).Add(innerRightBottomTable));
            mainTable.AddCell(CreateMainTableCell().Add(rightBottomTable));

            #endregion



            if (aPageSize.Contains(PageSize.A3)) {
                // A3
                mainTable.SetFixedPosition(415 * mmW()-TITLE_BLOCK_WIDTH+2f, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH);
                
                Canvas canvas = new Canvas(new PdfCanvas(_pdfDoc.GetFirstPage()),
                    new Rectangle((295)* mmW(), BOTTOM_MARGIN, PdfDefines.A4Width, 2));
                canvas.Add(
                    new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth((53 * 2 + 14 - 50) * mmW()));

            } else { 
                // A4
                var left = 20 * mmW() - 2f;
                mainTable.SetFixedPosition(left, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH);

                Canvas canvas = new Canvas(new PdfCanvas(_pdfDoc.GetFirstPage()),
                    new Rectangle(left + (7 + 10 + 23 + 15 + 10) * mmW(), BOTTOM_MARGIN, PdfDefines.A4Width, 2));
                canvas.Add(
                    new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth((53 * 2 + 14 - 50) * mmW()));
            }

            return mainTable;
        }

        
        /// <summary>
        /// создать таблицу основной надписи на последующих страницах
        /// </summary>
        /// <returns></returns>
        protected Table CreateNextTitleBlock(TitleBlockStruct titleBlockStruct) {
            var aGraphs = titleBlockStruct.Graphs;
            var aPageSize = titleBlockStruct.PageSize;   

            float[] columnSizes = {
                7 * mmW(), 
                10 * mmW(),
                23 * mmW(),
                15 * mmW(),
                10 * mmW(),
                110 * mmW(),
                10 * mmW(),
            };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

            Cell CreateCell() {
                return new Cell().SetHeight(DEFAULT_TITLE_BLOCK_CELL_HEIGHT).SetPadding(0).SetBorderRight(CreateThickBorder());
            }
            Paragraph CreateParagraph(string text) {
                return new Paragraph(text).SetItalic().SetPaddingTop(-2).SetFontSize(12).SetFont(f1).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            }

            for (int i = 0; i < 5; ++i) {
                tbl.AddCell(CreateCell().SetBorderTop(CreateThickBorder()));
            }

            var xxx = GetGraphByName(aGraphs, Constants.GRAPH_2);
            tbl.AddCell(new Cell(3, 1).
                Add(new Paragraph(GetGraphByName(aGraphs, Constants.GRAPH_2)).SetFont(f1).SetItalic().SetFontSize(20).SetTextAlignment(TextAlignment.CENTER)).
                SetVerticalAlignment(VerticalAlignment.MIDDLE).
                SetBorderTop(CreateThickBorder()).
                SetBorderRight(CreateThickBorder()).
                SetBorderBottom(CreateThickBorder()));

            var rightestCell = new Cell(3, 1).
                SetPadding(0).
                SetBorderTop(CreateThickBorder()).
                SetBorderRight(CreateThickBorder()).
                SetBorderBottom(CreateThickBorder());

            var rightestCellTable = new Table(UnitValue.CreatePercentArray(new[] {1f})).UseAllAvailableWidth();
            rightestCellTable.AddCell(
                new Cell().
                    SetHeight(7*mmH()).
                    SetBorderLeft(Border.NO_BORDER).
                    SetBorderRight(Border.NO_BORDER).
                    SetBorderTop(Border.NO_BORDER).
                    SetBorderBottom(CreateThickBorder()).
                    SetPadding(0).
                    Add(CreateParagraph("Лист").SetPaddingTop(2).SetPaddingLeft(2)));
            rightestCellTable.AddCell(
                new Cell().
                    SetHeight(8*mmH()).
                    SetBorderLeft(Border.NO_BORDER).
                    SetBorderRight(Border.NO_BORDER).
                    SetBorderBottom(Border.NO_BORDER).
                    SetPadding(0).
                    Add(CreateParagraph(titleBlockStruct.CurrentPage.ToString()).SetPaddingTop(2).SetPaddingLeft(7))); 

            rightestCell.Add(rightestCellTable);
            tbl.AddCell(rightestCell);

            void AddGraphCell(string text, bool bottomBorder=false) {
                var c = CreateCell().Add(CreateParagraph(text));
                if (bottomBorder) {
                    c.SetBorderBottom(CreateThickBorder());
                }
                tbl.AddCell(c);
            }

            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_14));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_15));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_16));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_17));
            AddGraphCell(GetGraphByName(aGraphs, Constants.GRAPH_18));

            AddGraphCell( "Изм.", bottomBorder:true);
            AddGraphCell( "Лист", bottomBorder:true);
            AddGraphCell( "№ докум.", bottomBorder:true);
            AddGraphCell( "Подп.", bottomBorder:true);
            AddGraphCell( "Дата", bottomBorder:true);



            // switch A3/A4
            if (aPageSize.Contains(PageSize.A3)) {
                tbl.SetFixedPosition(PdfDefines.A3Height - TITLE_BLOCK_WIDTH - RIGHT_MARGIN, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH);
            } else {
                tbl.SetFixedPosition(20 * mmW(), BOTTOM_MARGIN, TITLE_BLOCK_WIDTH);
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


        /// <summary>
        /// создать таблицу для верхней дополнительной графы
        /// </summary>
        /// <returns></returns>
        protected Table CreateTopAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs) {
        float[] columnSizes = {5 * mmW(), 7 * mmW()};
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

        tbl.AddCell(CreateAppendGraphCell(60 * mmW(), "Перв. примен."));
        tbl.AddCell(CreateAppendGraphCell(60 * mmW()));

        tbl.AddCell(CreateAppendGraphCell(60 * mmW(), "Справ. №"));
        tbl.AddCell(CreateAppendGraphCell(60 * mmW()));

            tbl.SetFixedPosition(APPEND_GRAPHS_LEFT, TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE, APPEND_GRAPHS_WIDTH);

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

        /// <summary>
        /// добавить количество страниц на первую страницу документа
        /// </summary>
        /// <param name="aInPdfDoc">PDF документ</param>
        /// <param name="aPageCount">общее количество страние</param>
        protected void AddPageCountOnFirstPage(iText.Layout.Document aDoc, int aPageCount)
        {   
            float left = 0f;
            float bottom = 65f;
            left = (7 + 10 + 23 + 15 + 10 + 110) * mmW() + 55;                

            aDoc.ShowTextAligned(new Paragraph(aPageCount.ToString()).
                                SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFontSize(12).SetFont(f1),
                                left, bottom,
                                1, 
                                TextAlignment.CENTER, VerticalAlignment.MIDDLE, 
                                0);
        
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

            int borderWidth = (aLastRowIsFinal) ? 2 : 1;
            for (int i = 0; i < aColumns; i++)
                aTable.AddCell(aTemplateCell.Clone(false).SetBorderBottom(new SolidBorder(borderWidth)));
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
