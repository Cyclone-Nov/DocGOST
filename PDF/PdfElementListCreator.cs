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
using System.Runtime;
using iText.Kernel.Pdf.Canvas.Draw;


namespace GostDOC.PDF {
/// <summary>
/// Перечень элементов
/// </summary>
/// <seealso cref="GostDOC.PDF.PdfCreator" />
internal class PdfElementListCreator : PdfCreator {

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

        if (_pdfWriter != null) {
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
        _doc = new iText.Layout.Document(_pdfDoc, _pdfDoc.GetDefaultPageSize(), false);

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

    /// <summary>
    /// добавить к документу первую страницу
    /// </summary>
    /// <returns>номер последней записанной строки. Если 0 - то достигнут конец таблицы данных</returns>
    internal override int AddFirstPage(iText.Layout.Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {
        SetPageMargins(aInDoc);

        // добавить таблицу с данными
        aInDoc.Add(CreateDataTable(new DataTableStruct {Data = aData, FirstPage = true, StartRow = 0}, out var lastProcessedRow));

        // добавить таблицу с основной надписью для первой старницы
        aInDoc.Add(CreateFirstTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, Pages = 0, AppendGraphs = true}));

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
            .AddStyle(style).SetFixedPosition(10 * mmW() + 2,
                TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE + 45 * mmW(), 100);
        aInDoc.Add(p);
        p = new Paragraph("Копировал").AddStyle(style)
            .SetFixedPosition((7 + 10 + 32 + 15 + 10 + 14) * mmW(), 0, 100);
        aInDoc.Add(p);
        p = new Paragraph("Формат А4").AddStyle(style)
            .SetFixedPosition((7 + 10 + 32 + 15 + 10 + 70) * mmW() + 20, 0, 100);
        aInDoc.Add(p);

    }

    void DrawLinesFirstPage() {
        // нарисовать недостающую линию
        var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH-2f;
        Canvas canvas = new Canvas(
            new PdfCanvas(_pdfDoc.GetFirstPage()), 
            new Rectangle(fromLeft, BOTTOM_MARGIN + TITLE_BLOCK_FIRST_PAGE_WITHOUT_APPEND_HEIGHT_MM * mmH() + 2f, 2, 100));
        canvas.Add(new LineSeparator(
            new SolidLine(THICK_LINE_WIDTH)).SetWidth(100).SetRotationAngle(DegreesToRadians(90)));
    }


    /// <summary>
    /// добавить к документу последующие страницы
    /// </summary>
    /// <param name="aInPdfDoc">a in PDF document.</param>
    /// <returns></returns>
    internal override int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNumber, int aStartRow) {
        aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        SetPageMargins(aInPdfDoc);

        // добавить таблицу с данными
        var dataTable = CreateDataTable(new DataTableStruct{Data = aData, FirstPage =false, StartRow = aStartRow}, out var lastProcessedRow);
        dataTable.SetFixedPosition(19.3f * mmW(), BOTTOM_MARGIN + 16 * mmW()+2.5f, TITLE_BLOCK_WIDTH + 2f);
        aInPdfDoc.Add(dataTable);

        // добавить таблицу с основной надписью для последуюших старницы
        aInPdfDoc.Add(CreateNextTitleBlock(new TitleBlockStruct {PageSize = _pageSize, Graphs = aGraphs, CurrentPage = aPageNumber }));

        // добавить таблицу с нижней дополнительной графой
        aInPdfDoc.Add(CreateBottomAppendGraph(_pageSize, aGraphs));

        return lastProcessedRow;
    }

    private new void SetPageMargins(iText.Layout.Document aDoc) {
        aDoc.SetLeftMargin(8 * mmW());
        aDoc.SetRightMargin(5 * mmW());
        aDoc.SetTopMargin(5 * mmW());
        aDoc.SetBottomMargin(5 * mmW());
    }


    /// <summary>
    /// создание таблицы данных
    /// </summary>
    /// <param name="aDataTableStruct">данные для создания таблицы</param>
    /// <param name="outLastProcessedRow">последняя обработанная строка таблицы данных</param>
    /// <returns></returns>
    private Table CreateDataTable(DataTableStruct aDataTableStruct, out int outLastProcessedRow) {
        var aData = aDataTableStruct.Data;
        var aGraphs = aDataTableStruct.Graphs;
        var aFirstPage = aDataTableStruct.FirstPage;
        var aStartRow = aDataTableStruct.StartRow;
        
        float[] columnSizes = {20 * mmW(), 110 * mmW(), 10 * mmW(), 45 * mmW()};
        Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
        tbl.SetMargin(0).SetPadding(0);

        // add header            
        Cell headerCell = CreateEmptyCell(1, 1).SetMargin(0).SetPaddings(-2, -2, -2, -2)
            .SetHeight(15 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1)
            .SetFontSize(16);
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Поз. обозна-\nчение").SetFixedLeading(11.5f)));
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Наименование")));
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Кол.")));
        tbl.AddHeaderCell(headerCell.Clone(false).Add(new Paragraph("Примечание")));

        // fill table
        Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 0)
            .SetHeight(8 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1)
            .SetFontSize(14);
        Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
            .SetHeight(8 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1)
            .SetFontSize(14);
        float fontSize = 14;
        PdfFont font = leftPaddCell.GetProperty<PdfFont>(20); // 20 - index for Font property

        int remainingPdfTabeRows = (aFirstPage) ? RowNumberOnFirstPage : RowNumberOnNextPage;
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
                if (remainingPdfTabeRows > 4) {
                    // если есть место для записи более 4 строк то записываем группу, иначе выходим
                    tbl.AddCell(centrAlignCell.Clone(false));
                    tbl.AddCell(centrAlignCell.Clone(true).Add(new Paragraph(name)));
                    tbl.AddCell(centrAlignCell.Clone(false));
                    tbl.AddCell(leftPaddCell.Clone(false));
                    remainingPdfTabeRows--;
                }
                else {
                    break;
                }
            }
            else {
                // разобьем наименование на несколько строк исходя из длины текста
                string[] namestrings = SplitStringByWidth(110 * mmW(), fontSize, font, name).ToArray();
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
                } else {
                    break;
                }
            }

            outLastProcessedRow++;
        }

        // дополним таблицу пустыми строками если она не полностью заполнена
        if (remainingPdfTabeRows > 0) {
            AddEmptyRowToPdfTable(tbl, remainingPdfTabeRows, 4, centrAlignCell, true);
        }
        // если записали всю табица с данными, то обнуляем количество оставшихся строк
        if (outLastProcessedRow == aData.Rows.Count) {
            outLastProcessedRow = 0;
        }

        tbl.SetFixedPosition(19.3f * mmW(), 78 * mmW()+0.48f, TITLE_BLOCK_WIDTH);

        return tbl;
    }

}
}