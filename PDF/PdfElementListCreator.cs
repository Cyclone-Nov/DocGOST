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
using GostDOC.Models;
using System.Data;

namespace GostDOC.PDF
{
    /// <summary>
    /// Перечень элементов
    /// </summary>
    /// <seealso cref="GostDOC.PDF.PdfCreator" />
    internal class PdfElementListCreator : PdfCreator
    {        

        private const string FileName = @"Перечень элементов.pdf";        

        public PdfElementListCreator() : base(DocType.ItemsList)
        {   
        }


        /// <summary>
        /// Создать документ
        /// </summary>
        public override void Create(Project project)
        {
            if (project.Configurations.Count == 0)
                return;
                        
            Configuration mainConfig = null;
            if (!project.Configurations.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
                return;
                        
            var dataTable = CreateDataTable(mainConfig.Specification);

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

            int next = AddFirstPage(doc, mainConfig.Graphs, dataTable);
            while (next > 0)
            {
                next = AddNextPage(doc, mainConfig.Graphs, dataTable);
            }

            if (pdfDoc.GetNumberOfPages() > 2)
            {
                AddRegisterList(doc, mainConfig.Graphs);
            }

            doc.Close();            
        }


        private DataTable CreateDataTable( IDictionary<string, Group> aData)
        {
            DataTable table = new DataTable();

            return table;
        }

        /// <summary>
        /// создать шаблон первой страницы документа
        /// (таблица данных в шаблоне не создается)
        /// </summary>
        /// <param name="aPageSize">Размер страницы</param>
        /// <returns></returns>
        internal PdfDocument CreateTemplateFirstPage(PageSize aPageSize)
        {
            MemoryStream stream = new MemoryStream();
            PdfDocument firstPage = new PdfDocument(new PdfWriter(stream));

            firstPage.SetDefaultPageSize(aPageSize);
            var document = new iText.Layout.Document(firstPage, aPageSize, true);

            SetPageMargins(document);
            
            // добавить таблицу с основной надписью для первой старницы
            //document.Add(CreateFirstTitleBlock(aPageSize));

            // добавить таблицу с верхней дополнительной графой
            //document.Add(CreateTopAppendGraph(aPageSize));

            // добавить таблицу с нижней дополнительной графой
            //document.Add(CreateBottomAppendGraph(aPageSize));

            doc.Close();
            return firstPage;
        }

        /// <summary>
        /// создать шаблон последующих страниц документа
        /// таблица данных в шаблоне не создается
        /// </summary>
        /// <returns></returns>
        internal PdfDocument CreateTemplateNextPage(PageSize aPageSize)
        {
            MemoryStream stream = new MemoryStream();
            PdfDocument nextPage = new PdfDocument(new PdfWriter(stream));

            nextPage.SetDefaultPageSize(aPageSize);
            var document = new iText.Layout.Document(nextPage, aPageSize, true);

            SetPageMargins(document);

            // добавить таблицу с основной надписью для дополнительного листа
            //document.Add(CreateNextTitleBlock(aPageSize));


            // добавить таблицу с нижней дополнительной графой
            //document.Add(CreateBottomAppendGraph(aPageSize));

            doc.Close();
            return nextPage;
        }


        /// <summary>
        /// создать страница регистрации изменений
        /// вместе с таблицей данных
        /// </summary>
        /// <returns></returns>
        internal PdfDocument CreateRegistrationPage()
        {
            MemoryStream stream = new MemoryStream();
            PdfDocument regPage = new PdfDocument(new PdfWriter(stream));

            regPage.SetDefaultPageSize(PageSize.A4);
            var document = new iText.Layout.Document(regPage, PageSize.A4, true);

            SetPageMargins(document);

            // добавить таблицу с основной надписью для дополнительного листа
            //document.Add(CreateNextTitleBlock(PageSize.A4));

            // добавить таблицу с нижней дополнительной графой
            //document.Add(CreateBottomAppendGraph(PageSize.A4));

            // добавить таблицу с данными


            doc.Close();
            return regPage;
        }

        /// <summary>
        /// добавить к документу первую страницу
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns>номер последней записанной строки. Если 0 - то достигнут конец таблицы данных</returns>
        internal override int AddFirstPage(iText.Layout.Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData)
        {
            SetPageMargins(aInDoc);
            
            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(PageSize, aGraphs, 0));

            // добавить таблицу с верхней дополнительной графой
            //aInDoc.Add(CreateTopAppendGraph(PageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            //aInDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));

            // добавить таблицу с данными
            int needNextPage = 0;
            
            //aInDoc.Add(CreateDataTable(aGroups, out needNextPage));

            return needNextPage;
        }

        /// <summary>
        /// добавить к документу последующие страницы
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal override int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData)
        {
            int row = 0;
            aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            return row;
        }


        private void SetPageMargins(iText.Layout.Document aDoc)
        {
            aDoc.SetLeftMargin(8 * PdfDefines.mmA4);
            aDoc.SetRightMargin(5 * PdfDefines.mmA4);
            aDoc.SetTopMargin(5 * PdfDefines.mmA4);
            aDoc.SetBottomMargin(5 * PdfDefines.mmA4);
        }


        /// <summary>
        /// создать таблицу основной надписи на первой странице
        /// </summary>
        /// <returns></returns>
        private Table CreateFirstTitleBlock(PageSize aPageSize, IDictionary<string, string> aGraphs, int aPages)
        {
            //             ..........
            //   Cell1     . cell2  .
            //.......................
            //.  Cell3     . cell4  .
            //.......................
            //.  Cell5     . cell6  .
            //.......................            

            float[] columnSizes = {65 * PdfDefines.mmA4, 120 * PdfDefines.mmA4 };
            Table mainTable = new Table(UnitValue.CreatePointArray(columnSizes));
            mainTable.SetMargin(0).SetPadding(0);

            #region Пустая ячейка для формирования таблицы (Cell 1)
            Cell cell1 = new Cell(1, 1);
            cell1.SetVerticalAlignment(VerticalAlignment.MIDDLE).SetHorizontalAlignment(HorizontalAlignment.CENTER).
                            SetBorderLeft(Border.NO_BORDER).SetBorderTop(Border.NO_BORDER).
                            SetBorderBottom(new SolidBorder(2)).SetBorderRight(new SolidBorder(2));            
            mainTable.AddCell(cell1);
            #endregion Cell 1

            #region Графы 27-30, не заполняются (Cell 2)
            //..................................
            //.   Cell2_1  . cell2_2 . cell2_3 .
            //..................................
            //.          cell2_4               .
            //..................................
            Table cell2Table = new Table(UnitValue.CreatePointArray(new float[] {14 * PdfDefines.mmA4, 53 * PdfDefines.mmA4, 53 * PdfDefines.mmA4 }));
            cell2Table.SetMargin(0).SetPadding(0).UseAllAvailableWidth();            
            int columns = 3;
            Cell cell = AddEmptyCell(1, 1).SetHeight(13 * PdfDefines.mmA4h);
            for (int i = 0; i < columns; i++)
                cell2Table.AddCell(cell.Clone(true)); // cell2_1 - cell2_3            
            cell2Table.AddCell(AddEmptyCell(1, 3).SetHeight(7.5f * PdfDefines.mmA4h)); // cell 2_4
            
            Cell cell2 = AddEmptyCell(1, 1).SetMargin(0).SetPadding(-2f);            
            cell2.Add(cell2Table);            
            mainTable.AddCell(cell2);
            mainTable.StartNewRow();
            #endregion Cell 2

            #region Графы 14-18, не заполняются пока (Cell3, 5c х 3r)            
            Table cell3Table = new Table(UnitValue.CreatePointArray(new float[] { 7 * PdfDefines.mmA4, 10 * PdfDefines.mmA4, 23.7f * PdfDefines.mmA4, 15f * PdfDefines.mmA4, 10 * PdfDefines.mmA4 }));
            cell3Table.SetMargin(0).SetPadding(0).UseAllAvailableWidth();                                    
            columns = 5;
            cell = AddEmptyCell(1, 1, 2, 2, 2, 1).SetMargin(0).SetHeight(4 * PdfDefines.mmA4);
            for (int i = 0; i < columns; i++) // 1 row
                cell3Table.AddCell(cell.Clone(false));
            cell = AddEmptyCell(1, 1, 2, 2, 1, 2).SetMargin(0).SetHeight(4 * PdfDefines.mmA4);
            for (int i = 0; i < columns; i++) // 2 row
                cell3Table.AddCell(cell.Clone(false));

            var textStyle = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1);
            cell = AddEmptyCell(1, 1).AddStyle(textStyle).SetFontSize(12).SetMargin(0).SetPaddings(-2,0,-2,0);
            cell3Table.AddCell(cell.Clone(false).Add(new Paragraph("Изм.")));
            cell3Table.AddCell(cell.Clone(false).Add(new Paragraph("Лист")));
            cell3Table.AddCell(cell.Clone(false).Add(new Paragraph("№ докум.")));
            cell3Table.AddCell(cell.Clone(false).Add(new Paragraph("Подп.")));
            cell3Table.AddCell(cell.Clone(false).Add(new Paragraph("Дата")));          

            Cell cell3 = AddEmptyCell(1, 1).SetMargins(0,0,0,0).SetPaddings(-2,-2,-2,-2).Add(cell3Table);
            mainTable.AddCell(cell3);
            #endregion Cell 3 

            #region Заполнение графы 2 (Cell 4)
            string res = string.Empty;
            if (aGraphs.TryGetValue(Common.Constants.GRAPH_2, out res))
            res += Common.Converters.GetDocumentCode(Type);
            textStyle = new Style().SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1);
            Cell cell4 = AddEmptyCell(1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetFontSize(20).SetHeight(13 * PdfDefines.mmA4h).SetPaddings(-2,0,-2,12);
            mainTable.AddCell(cell4);
            #endregion Cell 4             

            #region Заполнение граф 10-11, графы 12-13 не заполняются (Cell 5)
            textStyle = new Style().SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFontSize(12).SetFont(f1);
            Table cell5Table = new Table(UnitValue.CreatePointArray(new float[] { 17.5f * PdfDefines.mmA4, 23 * PdfDefines.mmA4, 15 * PdfDefines.mmA4, 10 * PdfDefines.mmA4 }));
            cell5Table.SetMargin(0).SetPadding(0).UseAllAvailableWidth();
            cell5Table.SetHorizontalAlignment(HorizontalAlignment.LEFT).SetVerticalAlignment(VerticalAlignment.TOP);

            cell = AddEmptyCell(1, 1, 2, 2, 2, 1).AddStyle(textStyle).SetMargin(0).SetPaddings(-1, -2, -1, 2);
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph("Разраб.")));
            res = string.Empty;
            if (!aGraphs.TryGetValue(Common.Constants.GRAPH_11bl_dev, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));
            cell5Table.AddCell(cell.Clone(false));
            cell5Table.AddCell(cell.Clone(false));

            cell = AddEmptyCell(1, 1, 2, 2, 1, 1).AddStyle(textStyle).SetMargin(0).SetPaddings(-1, 0, -1, 2);
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph("Пров.")));
            if (!aGraphs.TryGetValue(Common.Constants.GRAPH_11bl_chk, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));
            cell5Table.AddCell(cell.Clone(false));
            cell5Table.AddCell(cell.Clone(false));
            cell5Table.StartNewRow();
                        
            if (!aGraphs.TryGetValue(Common.Constants.GRAPH_10, out res))
                res = "";//TODO: log не удалось распарсить;
            cell5Table.AddCell(cell.Clone(true).Add(new Paragraph("X")));            
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_11app, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(cell.Clone(true).Add(new Paragraph(res)));
            cell5Table.AddCell(cell.Clone(true));
            cell5Table.AddCell(cell.Clone(true));
            cell5Table.StartNewRow();

            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph("Н. контр.")));            
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_11norm, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));
            cell5Table.AddCell(cell.Clone(false));
            cell5Table.AddCell(cell.Clone(false));

            cell = AddEmptyCell(1, 1, 2, 2, 1, 2).AddStyle(textStyle).SetMargin(0).SetPaddings(-1, -2, -1, 2);
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph("Утв.")));            
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_11affirm, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));
            cell5Table.AddCell(cell.Clone(false));
            cell5Table.AddCell(cell.Clone(false));

            Cell cell5 = AddEmptyCell(1, 1).SetMargin(0).SetPaddings(-2,-2,-2,-2).Add(cell5Table).SetHeight(30 * PdfDefines.mmA4);
            mainTable.AddCell(cell5);
            #endregion Cell 5

            #region Заполнение граф 1, 4, 7, 8. Графа 9 не заполняется (Cell 6)
            Table cell6Table = new Table(UnitValue.CreatePointArray(new float[] { 70 * PdfDefines.mmA4, 50 * PdfDefines.mmA4 }));
            cell6Table.SetMargin(0).SetPadding(0).UseAllAvailableWidth();
            
            textStyle = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1);            
            if(!aGraphs.TryGetValue(Constants.GRAPH_1, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetFontSize(16).SetMargin(0).SetPadding(-2));
            
            Table cell6_2Table = new Table(UnitValue.CreatePointArray(new float[] { 5 * PdfDefines.mmA4, 5 * PdfDefines.mmA4, 5 * PdfDefines.mmA4, 15 * PdfDefines.mmA4, 20 * PdfDefines.mmA4 }));
            cell6_2Table.SetMargin(0).SetPadding(0).UseAllAvailableWidth();            
            cell6_2Table.AddCell(AddEmptyCell(1, 3).Add(new Paragraph("Лит.").AddStyle(textStyle).SetFontSize(12)).SetMargin(0).SetPaddings(-2,0, -2,0));
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Лист").AddStyle(textStyle).SetFontSize(12)).SetMargin(0).SetPaddings(-2, 0, -2, 0));
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Листов").AddStyle(textStyle).SetFontSize(12)).SetMargin(0).SetPaddings(-2, 0, -2, 0));

            cell = AddEmptyCell(1, 1).AddStyle(textStyle).SetFontSize(12).SetMargin(0).SetPaddings(-2, 0, -2, 0);
            if (!aGraphs.TryGetValue(Constants.GRAPH_4, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6_2Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));            
            if(!aGraphs.TryGetValue(Constants.GRAPH_4a, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6_2Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));            
            if(!aGraphs.TryGetValue(Constants.GRAPH_4b, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6_2Table.AddCell(cell.Clone(false).Add(new Paragraph(res)));

            cell6_2Table.AddCell(cell.Clone(false).Add(new Paragraph("1")));
            cell6_2Table.AddCell(cell.Clone(false).Add(new Paragraph("")));
            cell6_2Table.AddCell(AddEmptyCell(3, 5).SetMargin(0).SetPaddings(-2,0,-2,0).SetHeight(19 * PdfDefines.mmA4));

            Cell cell6_2 = AddEmptyCell(1, 1).SetPadding(-2).SetMargin(0).Add(cell6_2Table);
            cell6Table.AddCell(cell6_2);

            Cell cell6 = AddEmptyCell(1, 1).SetPadding(-2).SetMargin(0).Add(cell6Table);
            mainTable.AddCell(cell6);
            #endregion Cell 6

            mainTable.SetFixedPosition(20 * PdfDefines.mmA4, 5 * PdfDefines.mmA4, 185 * PdfDefines.mmA4);

            // отрисовать таблицу в конкретном месте документа
            // PageSize ps = pdfDoc.getDefaultPageSize();
            //mainTable.setFixedPosition(ps.getWidth() - doc.getRightMargin() - totalWidth, ps.getHeight() - doc.getTopMargin() - totalHeight, totalWidth);
            //PageSize ps = pdfDoc.getDefaultPageSize();
            //IRenderer tableRenderer = table.createRendererSubTree().setParent(doc.getRenderer());
            //LayoutResult tableLayoutResult =
            //        tableRenderer.layout(new LayoutContext(new LayoutArea(0, new Rectangle(ps.getWidth(), 1000))));
            //float totalHeight = tableLayoutResult.getOccupiedArea().getBBox().getHeight();


            return mainTable;
        }
        

        /// <summary>
        /// создать таблицу основной надписи на последующих страницах
        /// </summary>
        /// <returns></returns>
        private Table CreateNextTitleBlock(PageSize aPageSize, IDictionary<string, string> aGraphs)
        {
            Table tbl = new Table(3);
            return tbl;
        }
        

        /// <summary>
        /// создать таблицу для верхней дополнительной графы
        /// </summary>
        /// <returns></returns>
        private Table CreateTopAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs)
        {
            Table tbl = new Table(3);
            return tbl;
        }

        /// <summary>
        /// создать таблицу для нижней дополнительной графы
        /// </summary>
        /// <returns></returns>
        private Table CreateBottomAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs)
        {
            Table tbl = new Table(3);
            return tbl;
        }
                

        /// <summary>
        /// добавить ячейку со строковым значеним в таблицу
        /// </summary>
        /// <param name="content">содержимое</param>
        /// <param name="borderWidth">толщина линии</param>
        /// <param name="colspan">количество занимаемых столбцов</param>
        /// <param name="alignment">выравнивание текста</param>
        /// <param name="font">шрифт текста</param>
        /// <returns></returns>
        public Cell createCell(String content, float borderWidth, int colspan, TextAlignment alignment, PdfFont font)
        {
            Cell cell = new Cell(1, colspan).Add(new Paragraph(content));
            cell.SetTextAlignment(alignment);
            cell.SetBorder(new SolidBorder(borderWidth));
            cell.SetFont(font);
            return cell;
        }

        /// <summary>
        /// добавить ячейку со значением в виде таблицы во внешнюю таблицу
        /// </summary>
        /// <param name="content">содержимое</param>
        /// <param name="borderWidth">толщина линии</param>
        /// <param name="colspan">количество занимаемых столбцов</param>
        /// <param name="alignment">выравнивание текста</param>
        /// <param name="font">шрифт текста</param>
        /// <returns></returns>
        public Cell createCell(Table content, float borderWidth, int colspan, TextAlignment alignment, PdfFont font)
        {
            Cell cell = new Cell(1, colspan).Add(content);
            cell.SetTextAlignment(alignment);
            cell.SetBorder(new SolidBorder(borderWidth));
            cell.SetFont(font);
            return cell;
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

        private Cell AddEmptyCell(int rowspan, int colspan)
        {
            Cell cell = new Cell(rowspan, colspan);
            cell.SetVerticalAlignment(VerticalAlignment.MIDDLE).
                 SetHorizontalAlignment(HorizontalAlignment.CENTER).
                 SetBorder(new SolidBorder(2));            
            return cell;
        }

        private Cell AddEmptyCell(int aRowspan, int aColspan, int aLeftBorder = 2, int aRightBorder = 2, int aTopBorder = 2, int aBottomBorder = 2)
        {
            Cell cell = new Cell(aRowspan, aColspan);
            cell.SetVerticalAlignment(VerticalAlignment.MIDDLE).
                 SetHorizontalAlignment(HorizontalAlignment.CENTER).
                 SetBorderLeft(new SolidBorder(aLeftBorder)).
                 SetBorderRight(new SolidBorder(aRightBorder)).
                 SetBorderTop(new SolidBorder(aTopBorder)).
                 SetBorderBottom(new SolidBorder(aBottomBorder));
            return cell;
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
