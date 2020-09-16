﻿using System;
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
using System.Runtime;
using iText.Kernel.Pdf.Canvas.Draw;


namespace GostDOC.PDF {
/// <summary>
/// Перечень элементов
/// </summary>
/// <seealso cref="GostDOC.PDF.PdfCreator" />
internal class PdfElementListCreator : PdfCreator {
    enum PageEnum {
        FIRST_PAGE,
        NEXT_PAGES
    }

    private const string FileName = @"Перечень элементов.pdf";

    public PdfElementListCreator() : base(DocType.ItemsList) 
    {
    }


    /// <summary>
    /// Создать PDF документ
    /// </summary>
    public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs) 
    {   
        var dataTable = aData;
        var graphs = aMainGraphs;

        if (pdfWriter != null) {
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
        doc = new iText.Layout.Document(pdfDoc, pdfDoc.GetDefaultPageSize(), false);

        int lastProcessedRow = AddFirstPage(doc, graphs, dataTable);
        
        _currentPageNumber = 1;
        while (lastProcessedRow > 0) {
            _currentPageNumber++;
            lastProcessedRow = AddNextPage(doc, graphs, dataTable, _currentPageNumber, lastProcessedRow);
        }
        
        if (pdfDoc.GetNumberOfPages() > MAX_PAGES_WITHOUT_CHANGELIST) {
            _currentPageNumber++;
            AddRegisterList(doc, graphs, _currentPageNumber);
        }

        AddPageCountOnFirstPage(doc, _currentPageNumber);

        doc.Close();
     }

    /// <summary>
    /// создать страница регистрации изменений
    /// вместе с таблицей данных
    /// </summary>
    /// <returns></returns>
    internal void AddRegistrationPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData) {
    }

    /// <summary>
    /// добавить к документу первую страницу
    /// </summary>
    /// <returns>номер последней записанной строки. Если 0 - то достигнут конец таблицы данных</returns>
    internal override int AddFirstPage(iText.Layout.Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {
        SetPageMargins(aInDoc);

        int lastProcessedRow = 0;
        // добавить таблицу с данными
        aInDoc.Add(CreateDataTable(aData, true, 0, out lastProcessedRow));

        // добавить таблицу с основной надписью для первой старницы
        aInDoc.Add(CreateFirstTitleBlock(_pageSize, aGraphs, 0));

        // добавить таблицу с верхней дополнительной графой
        aInDoc.Add(CreateTopAppendGraph(_pageSize, aGraphs));

        // добавить таблицу с нижней дополнительной графой
        aInDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));
        
        DrawLinesFirstPage();
        AddSecondaryElements(aInDoc, aGraphs);

        return lastProcessedRow;
    }

    void AddSecondaryElements(iText.Layout.Document aInDoc, IDictionary<string, string> aGraphs) {
        var style = new Style().SetItalic().SetFontSize(12).SetFont(f1).SetTextAlignment(TextAlignment.CENTER);
        var p = new Paragraph(GetGraphByName(aGraphs, Constants.GRAPH_PROJECT)).SetRotationAngle(DegreesToRadians(90))
            .AddStyle(style).SetFixedPosition(10 * PdfDefines.mmA4 + 2,
                TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE + 45 * PdfDefines.mmA4, 100);
        aInDoc.Add(p);
        p = new Paragraph("Копировал").AddStyle(style)
            .SetFixedPosition((7 + 10 + 32 + 15 + 10 + 14) * PdfDefines.mmA4, 0, 100);
        aInDoc.Add(p);
        p = new Paragraph("Формат А4").AddStyle(style)
            .SetFixedPosition((7 + 10 + 32 + 15 + 10 + 70) * PdfDefines.mmA4 + 20, 0, 100);
        aInDoc.Add(p);

    }

    void DrawLinesFirstPage() {
        // нарисовать недостающую линию
        var fromLeft = 19.3f * PdfDefines.mmA4 + TITLE_BLOCK_WIDTH-2f;
        Canvas canvas = new Canvas(
            new PdfCanvas(pdfDoc.GetFirstPage()), 
            new Rectangle(fromLeft, BOTTOM_MARGIN + (15 + 5 + 5 + 15 + 8 + 14) * PdfDefines.mmA4 + 2f, 2, 30));
        canvas.Add(new LineSeparator(
            new SolidLine(THICK_LINE_WIDTH)).SetWidth(15).SetRotationAngle(DegreesToRadians(90)));
    }


    /// <summary>
    /// добавить к документу последующие страницы
    /// </summary>
    /// <param name="aInPdfDoc">a in PDF document.</param>
    /// <returns></returns>
    internal override int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aStartRow) {
        aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        SetPageMargins(aInPdfDoc);

        // добавить таблицу с данными
        int lastNextProcessedRow;
        var dataTable = CreateDataTable(aData, false, aStartRow, out lastNextProcessedRow);
        dataTable.SetFixedPosition(19.3f * PdfDefines.mmA4, BOTTOM_MARGIN + 16 * PdfDefines.mmA4,
            TITLE_BLOCK_WIDTH + 2f);
        aInPdfDoc.Add(dataTable);


        // добавить таблицу с основной надписью для последуюших старницы
        aInPdfDoc.Add(CreateNextTitleBlock(_pageSize, aGraphs, aPageNumber));

        // добавить таблицу с нижней дополнительной графой
        aInPdfDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

        return lastNextProcessedRow;
    }

    private new void SetPageMargins(iText.Layout.Document aDoc) {
        aDoc.SetLeftMargin(8 * PdfDefines.mmA4);
        aDoc.SetRightMargin(5 * PdfDefines.mmA4);
        aDoc.SetTopMargin(5 * PdfDefines.mmA4);
        aDoc.SetBottomMargin(5 * PdfDefines.mmA4);
    }

  
    /// <summary>
    /// создание таблицы данных
    /// </summary>
    /// <param name="aData">таблица данных</param>
    /// <param name="firstPage">признак первой или последующих страниц</param>
    /// <param name="aStartRow">строка таблицы данных с которой надо начинать запись в PDF страницу</param>
    /// <param name="outLastProcessedRow">последняя обработанная строка таблицы данных</param>
    /// <returns></returns>
    private Table CreateDataTable(DataTable aData, bool firstPage, int aStartRow, out int outLastProcessedRow) {
        float[] columnSizes = {20 * PdfDefines.mmA4, 110 * PdfDefines.mmA4, 10 * PdfDefines.mmA4, 45 * PdfDefines.mmA4};
        Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
        tbl.SetMargin(0).SetPadding(0);

        // add header            
        Cell headerCell = CreateEmptyCell(1, 1).SetMargin(0).SetPaddings(-2, -2, -2, -2)
            .SetHeight(15 * PdfDefines.mmA4h).SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1)
            .SetFontSize(16);
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Поз. обозна-\nчение").SetFixedLeading(11.0f)));
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Наименование")));
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Кол.")));
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Примечание")));

        // fill table
        Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 0)
            .SetHeight(8 * PdfDefines.mmA4h).SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1)
            .SetFontSize(14);
        Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
            .SetHeight(8 * PdfDefines.mmA4h).SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1)
            .SetFontSize(14);
        //UnitValue uFontSize = mainCell.GetProperty<UnitValue>(24); // 24 - index for FontSize property
        //float fontSize = uFontSize.GetValue();
        float fontSize = 14;
        PdfFont font = leftPaddCell.GetProperty<PdfFont>(20); // 20 - index for Font property

        int remainingPdfTabeRows = (firstPage) ? RowNumberOnFirstPage : RowNumberOnNextPage;
        outLastProcessedRow = aStartRow;


        var Rows = aData.Rows.Cast<DataRow>().ToArray();
        DataRow row;
        for (int ind = aStartRow; ind < Rows.Length; ind++) {
            row = Rows[ind];
            string position = (row[Constants.ColumnPosition] == System.DBNull.Value)
                ? string.Empty
                : (string) row[Constants.ColumnPosition];
            string name = (row[Constants.ColumnName] == System.DBNull.Value)
                ? string.Empty
                : (string) row[Constants.ColumnName];
            int quantity = (row[Constants.ColumnQuantity] == System.DBNull.Value)
                ? 0
                : (int) row[Constants.ColumnQuantity];
            string note = (row[Constants.ColumnFootnote] == System.DBNull.Value)
                ? string.Empty
                : (string) row[Constants.ColumnFootnote];

            if (string.IsNullOrEmpty(name)) {
                AddEmptyRowToPdfTable(tbl, 1, 4, leftPaddCell);
                remainingPdfTabeRows--;
            }
            else if (string.IsNullOrEmpty(position)) {
                // это наименование группы
                if (remainingPdfTabeRows > 4
                ) // если есть место для записи более 4 строк то записываем группу, иначе выходим
                {
                    tbl.AddCell(centrAlignCell.Clone(false));
                    tbl.AddCell(centrAlignCell.Clone(true).Add(new Paragraph(name)));
                    tbl.AddCell(centrAlignCell.Clone(false));
                    tbl.AddCell(leftPaddCell.Clone(false));
                    remainingPdfTabeRows--;
                }
                else
                    break;
            }
            else {
                // разобьем наименование на несколько строк исходя из длины текста
                string[] namestrings = SplitNameByWidth(110 * PdfDefines.mmA4, fontSize, font, name).ToArray();
                if (namestrings.Length <= remainingPdfTabeRows) {
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(position)));
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(namestrings[0])));
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(quantity.ToString())));
                    tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(note)));
                    remainingPdfTabeRows--;

                    if (namestrings.Length > 1) {
                        for (int i = 1; i < namestrings.Length; i++) {
                            tbl.AddCell(centrAlignCell.Clone(false));
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
        if (remainingPdfTabeRows > 0)
            AddEmptyRowToPdfTable(tbl, remainingPdfTabeRows, 4, centrAlignCell, true);

        // если записали всю табица с данными, то обнуляем количество оставшихся строк
        if (outLastProcessedRow == aData.Rows.Count)
            outLastProcessedRow = 0;

        tbl.SetFixedPosition(19.3f * PdfDefines.mmA4, 78 * PdfDefines.mmA4 + 0.5f, TITLE_BLOCK_WIDTH);

        return tbl;
    }

    /// <summary>
    /// добавить пустые строки в таблицу PDF
    /// </summary>
    /// <param name="aTable">таблица PDF</param>
    /// <param name="aRows">количество пустых строк которые надо добавить</param>
    /// <param name="aColumns">количество столбцов в таблице</param>
    /// <param name="aTemplateCell">шаблон ячейки</param>
    /// <param name="aLastRowIsFinal">признак, что последняя строка - это последняя строка таблицы</param>
    private void AddEmptyRowToPdfTable(Table aTable, int aRows, int aColumns, Cell aTemplateCell,
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
    private Cell CreateEmptyCell(int aRowspan, int aColspan, int aLeftBorder = 2, int aRightBorder = 2,
        int aTopBorder = 2, int aBottomBorder = 2) {
        Cell cell = new Cell(aRowspan, aColspan);
        cell.SetVerticalAlignment(VerticalAlignment.MIDDLE).SetHorizontalAlignment(HorizontalAlignment.CENTER);

        cell.SetBorderBottom(aBottomBorder == 0 ? Border.NO_BORDER : new SolidBorder(aBottomBorder));
        cell.SetBorderTop(aTopBorder == 0 ? Border.NO_BORDER : new SolidBorder(aTopBorder));
        cell.SetBorderLeft(aLeftBorder == 0 ? Border.NO_BORDER : new SolidBorder(aLeftBorder));
        cell.SetBorderRight(aRightBorder == 0 ? Border.NO_BORDER : new SolidBorder(aRightBorder));

        return cell;
    }

    /// <summary>
    /// Разбить строку на несколько строк исходя из длины текста
    /// </summary>
    /// <param name="aLength">максимальная длина в мм</param>
    /// <param name="aFontSize">размер шрифта</param>
    /// <param name="aFont">шрифт</param>
    /// <param name="aString">строка для разбивки</param>
    /// <returns></returns>
    private List<string> SplitNameByWidth(float aLength, float aFontSize, PdfFont aFont, string aString) 
    {
        List<string> name_strings = new List<string>();
        int default_padding = 4;
        float maxLength = aLength - default_padding;
        float currLength = aFont.GetWidth(aString, aFontSize);

        GetLimitSubstring(name_strings, maxLength, currLength, aString);

        return name_strings;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name_strings"></param>
    /// <param name="maxLength"></param>
    /// <param name="currLength"></param>
    /// <param name="aFullName"></param>
    private void GetLimitSubstring(List<string> name_strings, float maxLength, float currLength, string aFullName) 
    {
        if (currLength < maxLength) {
            name_strings.Add(aFullName);
        }
        else {
            string fullName = aFullName;
            int symbOnMaxLength = (int) ((fullName.Length / currLength) * maxLength);
            string partName = fullName.Substring(0, symbOnMaxLength);
            int index = partName.LastIndexOfAny(new char[] {' ', '-', '.'});
            name_strings.Add(fullName.Substring(0, index));
            fullName = fullName.Substring(index + 1);
            currLength = fullName.Length;
            GetLimitSubstring(name_strings, maxLength, currLength, fullName);
        }
    }
}
}