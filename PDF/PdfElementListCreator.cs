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

        /// <summary>
        /// поток, содержащий PDF документ
        /// </summary>
        /// <value>
        /// The main stream.
        /// </value>
        public MemoryStream MainStream { get; } = new MemoryStream();

        /// <summary>
        /// The PDF document
        /// </summary>
        private PdfDocument pdfDoc;

        /// <summary>
        /// The document
        /// </summary>
        private iText.Layout.Document doc;

        /// <summary>
        /// The PDF writer
        /// </summary>
        private PdfWriter pdfWriter;

        public PdfElementListCreator() : base(DocType.ItemsList)
        {
            pdfWriter = new PdfWriter(MainStream);
            pdfDoc = new PdfDocument(pdfWriter);
            pdfDoc.SetDefaultPageSize(PageSize.A4);
            doc = new iText.Layout.Document(pdfDoc, pdfDoc.GetDefaultPageSize(), false);
        }

        public override byte[] GetData()
        {
            doc.Flush();
            pdfWriter.Flush();
            var arr = MainStream.ToArray();
            return arr;
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

            // var page = pdfDoc.AddNewPage();
            // var pdfCanvas = new PdfCanvas(page);
            // Rectangle rectangle = new Rectangle(mmA4* 10, mmA4 * 10, A4Width, mmA4 * 40);
            // Canvas canvas = new Canvas(pdfCanvas, rectangle);
            // canvas.Add(table);
            // PdfFont f1 = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN, true);

            //PdfFont times = PdfFontFactory.CreateFont("GOST_TYPE_A.ttf", "cp1251", true);
            //PdfFont gostBu = PdfFontFactory.CreateFont("GOST_BU.ttf", "cp1251", true);

            // float[] columnWidths = {.2f, .4f, 1, 6, .2f, 2};
            //float[] columnWidths = { 1, 13, 0.1f, 5 };
            //Table table =
            //    new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth(); //.SetHeight(A4Height);

            //table.SetBorderBottom(new SolidBorder(2));
            //iText.Layout.Style italicHeaderStyle = new Style();
            //italicHeaderStyle.SetFont(f1).SetItalic().SetFontSize(14);
            //iText.Layout.Style verticalCellStyle = new Style();
            //verticalCellStyle.SetFont(f1).SetItalic().SetFontSize(14).SetRotationAngle(DegreesToRadians(90));


            //var cell = new Cell();
            //cell.Add(new Paragraph("Поз.").SetFont(f1));
            //cell.Add(new Paragraph("обозна-").SetFont(f1).SetFixedLeading(5));
            //cell.Add(new Paragraph("чение").SetFont(f1)); //.SetPaddings(0,0,0,0));
            //ApplyForCell(cell);
            //table.AddCell(cell);

            //cell = new Cell().Add(new Paragraph("Наименование").SetFont(f1));
            //ApplyForCell(cell);
            //table.AddCell(cell);

            //cell = new Cell().Add(new Paragraph("Кол.").SetFont(f1).SetMargin(0).SetPadding(0))
            //    .SetMargin(0).SetWidth(4 * PdfDefines.mmA4).SetPaddingLeft(-10).SetPaddingRight(-10);
            //ApplyForCell(cell);
            //table.AddCell(cell);

            //cell = new Cell().Add(new Paragraph("Примечание").SetFont(f1));
            //ApplyForCell(cell);
            //table.AddCell(cell);


            //AddEmptyCells(16, PdfDefines.ROW_HEIGHT, table);

            //table.AddCell(AddTopAppendGraph("", ""));

            //AddEmptyCells(28, PdfDefines.ROW_HEIGHT, table);

            //cell = new Cell(3, 1).SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER);
            //table.AddCell(cell);
            //cell = new Cell(3, 1).SetBorderLeft(Border.NO_BORDER);
            //table.AddCell(cell);

            //AddEmptyCells(12, PdfDefines.ROW_HEIGHT, table);

            //table.AddCell(AddBottomAppendGraph());

            //AddEmptyCells(12, PdfDefines.ROW_HEIGHT, table);


            //FooterTableInfo footerTableInfo = new FooterTableInfo();
            //table.AddCell(new Cell(1, 4).SetPadding(0).Add(AddFooterTable(footerTableInfo)).SetBorderRight(new SolidBorder(2)));

            //doc.Add(table);
            //doc.Close();
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
            aDoc.SetLeftMargin(9 * PdfDefines.mmA4);
            aDoc.SetRightMargin(9 * PdfDefines.mmA4);
            aDoc.SetTopMargin(5 * PdfDefines.mmA4);
            aDoc.SetBottomMargin(5 * PdfDefines.mmA4);
        }

        /// <summary>
        /// создать таблицу основной надписи
        /// </summary>
        /// <returns></returns>
        private Table CreateTitleBlock(PageSize aPageSize, IDictionary<string, string> aGraphs)
        {
            Table tbl = new Table(3);
            return tbl;
        }

        /// <summary>
        /// создать таблицу основной надписи
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
            mainTable.SetWidth(185 * PdfDefines.mmA4);            
            mainTable.SetHeight(62 * PdfDefines.mmA4);

            Cell cell1 = new Cell(1, 1);
            cell1.SetVerticalAlignment(VerticalAlignment.MIDDLE).
                            SetHorizontalAlignment(HorizontalAlignment.CENTER).
                            SetBorderLeft(Border.NO_BORDER).
                            SetBorderTop(Border.NO_BORDER).
                            SetBorderBottom(new SolidBorder(2)).
                            SetBorderRight(new SolidBorder(2)).
                            SetHeight(22 * PdfDefines.mmA4);

            mainTable.AddCell(cell1);

            // fill cell 2
            //..................................
            //.   Cell2_1  . cell2_2 . cell2_3 .
            //..................................
            //.          cell2_4               .
            //..................................
            Table cell2Table = new Table(UnitValue.CreatePointArray(new float[] {14 * PdfDefines.mmA4, 53 * PdfDefines.mmA4, 53 * PdfDefines.mmA4 }));
            int columns = 3;
            for (int i = 0; i < columns; i++)
                cell2Table.AddCell(AddEmptyCell(1, 1).SetHeight(14 * PdfDefines.mmA4)); // cell2_1 - cell2_3            
            cell2Table.AddCell(AddEmptyCell(1, 3).SetHeight(8 * PdfDefines.mmA4)); // cell 2_4

            Cell cell2 = AddEmptyCell(1, 1);            
            cell2.Add(cell2Table);
            mainTable.AddCell(cell2);

            // fill cell 3 (5c х 3r)            
            Table cell3Table = new Table(UnitValue.CreatePointArray(new float[] { 7 * PdfDefines.mmA4, 10 * PdfDefines.mmA4, 23 * PdfDefines.mmA4, 15 * PdfDefines.mmA4, 10 * PdfDefines.mmA4 }));
            columns = 5;
            for (int i = 0; i < columns; i++) // 1 row
                cell3Table.AddCell(AddEmptyCell(1, 1, 2, 2, 2, 1).SetHeight(5 * PdfDefines.mmA4));
            for (int i = 0; i < columns; i++) // 2 row
                cell3Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 2).SetHeight(5 * PdfDefines.mmA4));

            var textStyle = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1);

            cell3Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Изм.")).AddStyle(textStyle).SetFontSize(12).SetHeight(5 * PdfDefines.mmA4));
            cell3Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Лист")).AddStyle(textStyle)).SetFontSize(12).SetHeight(5 * PdfDefines.mmA4);
            cell3Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("№ докум.")).AddStyle(textStyle).SetFontSize(12).SetHeight(5 * PdfDefines.mmA4));
            cell3Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Подп.")).AddStyle(textStyle).SetFontSize(12).SetHeight(5 * PdfDefines.mmA4));
            cell3Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Дата")).AddStyle(textStyle).SetFontSize(12).SetHeight(5 * PdfDefines.mmA4));

            Cell cell3 = AddEmptyCell(1, 1).Add(cell3Table);
            mainTable.AddCell(cell3);

            // fill cell 4             
            string res = string.Empty;
            if (aGraphs.TryGetValue(Common.Constants.GRAPH_2, out res))
            res += Common.Converters.GetDocumentCode(Type);
            Cell cell4 = AddEmptyCell(1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetFontSize(20).SetHeight(15 * PdfDefines.mmA4);
            mainTable.AddCell(cell4);

            // fill cell 5
            textStyle = new Style().SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFontSize(12).SetFont(f1);
            Table cell5Table = new Table(UnitValue.CreatePointArray(new float[] { 17 * PdfDefines.mmA4, 23 * PdfDefines.mmA4, 15 * PdfDefines.mmA4, 10 * PdfDefines.mmA4 }));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 2, 1).Add(new Paragraph("Разраб.")).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if (!aGraphs.TryGetValue(Common.Constants.GRAPH_11bl_dev, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 2, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 2, 1).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 2, 1).SetHeight(5 * PdfDefines.mmA4));

            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).Add(new Paragraph("Пров.")).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if (!aGraphs.TryGetValue(Common.Constants.GRAPH_11bl_chk, out res))
            res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).SetHeight(5 * PdfDefines.mmA4));

            res = string.Empty;
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_10, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_11app, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).SetHeight(5 * PdfDefines.mmA4));

            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).Add(new Paragraph("Н. контр.")).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_11norm, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 1).SetHeight(5 * PdfDefines.mmA4));

            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 2).Add(new Paragraph("Утв.")).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if(!aGraphs.TryGetValue(Common.Constants.GRAPH_11affirm, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 2).Add(new Paragraph(res)).AddStyle(textStyle).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 2).SetHeight(5 * PdfDefines.mmA4));
            cell5Table.AddCell(AddEmptyCell(1, 1, 2, 2, 1, 2).SetHeight(5 * PdfDefines.mmA4));

            Cell cell5 = AddEmptyCell(1, 1).Add(cell5Table);
            mainTable.AddCell(cell5);

            // fill Cell6
            Table cell6Table = new Table(UnitValue.CreatePointArray(new float[] { 70 * PdfDefines.mmA4, 50 * PdfDefines.mmA4 }));
            textStyle = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1);
            res = string.Empty;
            if(!aGraphs.TryGetValue(Constants.GRAPH_1, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph(res)).AddStyle(textStyle).SetFontSize(16).SetHeight(25 * PdfDefines.mmA4));

            Table cell6_2Table = new Table(UnitValue.CreatePointArray(new float[] { 5 * PdfDefines.mmA4, 5 * PdfDefines.mmA4, 5 * PdfDefines.mmA4, 15 * PdfDefines.mmA4, 20 * PdfDefines.mmA4 }));
            cell6_2Table.AddCell(AddEmptyCell(3, 1).Add(new Paragraph("Лит.").AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Лист").AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Листов").AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));

            res = string.Empty;
            if(!aGraphs.TryGetValue(Constants.GRAPH_4, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph(res).AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if(!aGraphs.TryGetValue(Constants.GRAPH_4a, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph(res).AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));
            res = string.Empty;
            if(!aGraphs.TryGetValue(Constants.GRAPH_4b, out res))
                res = string.Empty;//TODO: log не удалось распарсить;
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph(res).AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));

            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("1").AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));
            cell6_2Table.AddCell(AddEmptyCell(1, 1).Add(new Paragraph("Х").AddStyle(textStyle).SetFontSize(12)).SetHeight(5 * PdfDefines.mmA4));
            cell6_2Table.AddCell(AddEmptyCell(5, 3).SetHeight(15 * PdfDefines.mmA4));

            Cell cell6_2 = AddEmptyCell(1, 1).Add(cell6_2Table);
            cell6Table.AddCell(cell6_2);

            Cell cell6 = AddEmptyCell(1, 1).Add(cell6Table);
            mainTable.AddCell(cell6);

            return mainTable;
        }

        /// <summary>
        /// создать таблицу основной надписи
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
