using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using GostDOC.Common;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using GostDOC.DataPreparation;

namespace GostDOC.PDF {
/// <summary>
/// Перечень элементов
/// </summary>
/// <seealso cref="GostDOC.PDF.PdfCreator" />
internal class PdfElementListCreator : PdfCreator {

    private const string FileName = @"Перечень элементов.pdf";
    //private readonly float DATA_TABLE_LEFT = 19.3f * mmW() - TO_LEFT_CORRECTION;

    public PdfElementListCreator() : base(DocType.ItemsList) 
    {
    }


    /// <summary>
    /// Создать PDF документ
    /// </summary>
    public override void Create(DataTable aData, IDictionary<string, string> aMainGraphs, Dictionary<string, object> aAppParams) 
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
        _doc = new Document(_pdfDoc, _pdfDoc.GetDefaultPageSize(), true);
        int countPages = CommonUtils.GetCountPage(Type, dataTable.Rows.Count);
        int lastProcessedRow = AddFirstPage(_doc, graphs, dataTable, countPages, aAppParams);
        
        _currentPageNumber = 1;
        while (lastProcessedRow > 0) {
            _currentPageNumber++;
            lastProcessedRow = AddNextPage(_doc, graphs, dataTable, _currentPageNumber, lastProcessedRow, aAppParams);
        }
         
        if (_pdfDoc.GetNumberOfPages() > Constants.MAX_PAGES_WITHOUT_CHANGELIST) {
            _currentPageNumber++;
            AddRegisterList(_doc, graphs, _currentPageNumber, aAppParams);
        }

        _doc.Close();
     }

    /// <summary>
    /// добавить к документу первую страницу
    /// </summary>
    /// <returns>номер последней записанной строки. Если 0 - то достигнут конец таблицы данных</returns>
    internal override int AddFirstPage(Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData, int aCountPages, Dictionary<string, object> aAppParams) {
        SetPageMargins(aInDoc);

        // добавить таблицу с данными
        var dataTable = CreateDataTable(new DataTableStruct {Data = aData, FirstPage = true, StartRow = 0}, out var lastProcessedRow);
        dataTable.SetFixedPosition(
            DATA_TABLE_LEFT,
            PdfDefines.A4Height - (GetTableHeight(dataTable, 1) + TOP_MARGIN),
            TITLE_BLOCK_WIDTH);
        aInDoc.Add(dataTable);

        aAppParams.TryGetValue(Constants.AppParamDocSign, out var docSign);

        // добавить таблицу с основной надписью для первой старницы
        aInDoc.Add(CreateFirstTitleBlock(new TitleBlockStruct {
            PageSize = _pageSize, 
            Graphs = aGraphs, 
            Pages = aCountPages, 
            AppendGraphs = true, 
            DocType = DocType.ItemsList,
            CurrentPage = 1,
            DocSign = docSign.ToString()
        }));

        // добавить таблицу с верхней дополнительной графой
        aInDoc.Add(CreateTopAppendGraph(aGraphs));

        // добавить таблицу с нижней дополнительной графой
        aInDoc.Add(CreateBottomAppendGraph(aGraphs));

        DrawLines(1);
        AddCopyFormatSubscription(aInDoc, 1);
        AddVerticalProjectSubscription(aInDoc, aGraphs);

        return lastProcessedRow;
    }


    void DrawLines(int aPageNumber) {
        if (aPageNumber == 1) {
            var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH - 2f - TO_LEFT_CORRECTION;
            DrawVerticalLine(1, fromLeft, BOTTOM_MARGIN + TITLE_BLOCK_FIRST_PAGE_WITHOUT_APPEND_HEIGHT_MM * mmH() + 2f, 2, 120);
        }
        else {
            var fromLeft = 19.3f * mmW() + TITLE_BLOCK_WIDTH - 2f - TO_LEFT_CORRECTION;
            DrawVerticalLine(aPageNumber, fromLeft, BOTTOM_MARGIN + 2f, 2, 100);
        }
    }


    /// <summary>
    /// добавить к документу последующие страницы
    /// </summary>
    /// <param name="aInPdfDoc">a in PDF document.</param>
    /// <returns></returns>
    internal override int AddNextPage(Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aPageNumber, int aStartRow, Dictionary<string, object> aAppParams) {
        aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        SetPageMargins(aInPdfDoc);

        // добавить таблицу с данными
        var dataTable = CreateDataTable(new DataTableStruct{Data = aData, FirstPage =false, StartRow = aStartRow}, out var lastProcessedRow);
        dataTable.SetFixedPosition(
            DATA_TABLE_LEFT,
            PdfDefines.A4Height - (GetTableHeight(dataTable, aPageNumber) + TOP_MARGIN) - 2.5f,
            TITLE_BLOCK_WIDTH);
        aInPdfDoc.Add(dataTable);

        aAppParams.TryGetValue(Constants.AppParamDocSign, out var docSign);

        var titleBlock = CreateNextTitleBlock(new TitleBlockStruct { PageSize = _pageSize, Graphs = aGraphs, CurrentPage = aPageNumber, DocType = DocType.ItemsList, DocSign = docSign.ToString() });
        titleBlock.SetFixedPosition(DATA_TABLE_LEFT, BOTTOM_MARGIN, TITLE_BLOCK_WIDTH - 0.02f);
        aInPdfDoc.Add(titleBlock);

        aInPdfDoc.Add(CreateBottomAppendGraph(aGraphs));

        DrawLines(aPageNumber);

        AddCopyFormatSubscription(aInPdfDoc, aPageNumber);

        AddVerticalProjectSubscription(aInPdfDoc, aGraphs);

        return lastProcessedRow;
    }

    private new void SetPageMargins(Document aDoc) {
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
        //var aGraphs = aDataTableStruct.Graphs;
        var aFirstPage = aDataTableStruct.FirstPage;
        var aStartRow = aDataTableStruct.StartRow;
        
        float[] columnSizes = {
                Constants.ItemsListColumn1PositionWidth * mmW(),
                Constants.ItemsListColumn2NameWidth * mmW(),
                Constants.ItemsListColumn3QuantityWidth * mmW(),
                Constants.ItemsListColumn4FootnoteWidth * mmW()};

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

        // base cells
        Cell centrAlignCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 0)
            .SetHeight(8 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1)
            .SetFontSize(Constants.ItemListFontSize);
        Cell leftPaddCell = CreateEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 2)
            .SetHeight(8 * PdfDefines.mmAXh).SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1)
            .SetFontSize(Constants.ItemListFontSize);

        int remainingPdfTableRows = (aFirstPage) ? RowNumberOnFirstPage : RowNumberOnNextPage;
        outLastProcessedRow = aStartRow;

        var Rows = aData.Rows.Cast<DataRow>().ToArray();
        DataRow row;
        for (int ind = aStartRow; ind < Rows.Length; ind++) {

            if (remainingPdfTableRows <= 0) {
                break;
            }

            row = Rows[ind];

            string GetCellString(string columnName) =>(row[columnName] == DBNull.Value) ? string.Empty : ((BasePreparer.FormattedString)row[columnName]).Value;

            BasePreparer.FormattedString GetCellStringFormatted(string columnName) => (row[columnName] == System.DBNull.Value) ? null : ((BasePreparer.FormattedString)row[columnName]);

            string position = GetCellString(Constants.ColumnPosition);
            var name = GetCellStringFormatted(Constants.ColumnName);
            string note = GetCellString(Constants.ColumnFootnote);
            int quantity = (row[Constants.ColumnQuantity] == DBNull.Value) ? 0 : (int) row[Constants.ColumnQuantity];


            string nextPosition = position;
            if (ind+1 < Rows.Length)
            { 
                nextPosition = (Rows[ind+1][Constants.ColumnPosition] == DBNull.Value) ? string.Empty : ((BasePreparer.FormattedString)Rows[ind + 1][Constants.ColumnPosition]).Value;
            }

            if (IsEmptyRow(row)) 
            {
                AddEmptyRowToPdfTable(tbl, 1, 4, leftPaddCell, remainingPdfTableRows == 1 ? true : false);
                remainingPdfTableRows--;
            }            
            //else if (IsGroupName(row)) // это наименование группы
            //{                
            //    if (remainingPdfTableRows > 4 || ((remainingPdfTableRows > 1) && !string.IsNullOrEmpty(nextPosition)))
            //    {
            //        // если есть место для записи более 4 строк то записываем группу, иначе выходим
            //        tbl.AddCell(centrAlignCell.Clone(false));
            //        tbl.AddCell(centrAlignCell.Clone(true).Add(new Paragraph(name.Value)));
            //        tbl.AddCell(centrAlignCell.Clone(false));
            //        tbl.AddCell(leftPaddCell.Clone(false));
            //        remainingPdfTableRows--;
            //    }
            //    else                 
            //        break;                
            //}
            else 
            {
                if (remainingPdfTableRows == 1)
                {
                    centrAlignCell.SetBorderBottom(THICK_BORDER);
                    leftPaddCell.SetBorderBottom(THICK_BORDER);
                }

                // просто запишем строку
                tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(position)));
                tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(name.Value)));

                if (string.IsNullOrEmpty(name.Value) || quantity == 0)
                    tbl.AddCell(centrAlignCell.Clone(false));
                else
                    tbl.AddCell(centrAlignCell.Clone(false).Add(new Paragraph(quantity.ToString())));

                tbl.AddCell(leftPaddCell.Clone(false).Add(new Paragraph(note)));

                remainingPdfTableRows--; 
            }
            outLastProcessedRow++;
        }

        // дополним таблицу пустыми строками если она не полностью заполнена
        if (remainingPdfTableRows > 0) {
            AddEmptyRowToPdfTable(tbl, remainingPdfTableRows, 4, centrAlignCell, true);
        }
        // если записали всю табица с данными, то обнуляем количество оставшихся строк
        if (outLastProcessedRow == aData.Rows.Count) {
            outLastProcessedRow = 0;
        }

        return tbl;
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
        if (string.IsNullOrEmpty(aRow[Constants.ColumnName].ToString()) &&
            string.IsNullOrEmpty(aRow[Constants.ColumnPosition].ToString()) &&
            string.IsNullOrEmpty(aRow[Constants.ColumnFootnote].ToString()))
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
        var name = ((BasePreparer.FormattedString)aRow[Constants.ColumnName]);

        //if (string.IsNullOrEmpty(aRow[Constants.ColumnPosition].ToString()) &&
        //    string.IsNullOrEmpty(aRow[Constants.ColumnQuantity].ToString()) &&
        //    name.TextAlignment == TextAlignment.CENTER)
        if (name.TextAlignment == TextAlignment.CENTER)
        {
            return true;
        }
        return false;
    }

    }
}