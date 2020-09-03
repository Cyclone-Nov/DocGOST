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
    enum PageEnum {
        FIRST_PAGE,
        NEXT_PAGES
    }

    private const string FileName = @"Перечень элементов.pdf";



    private int _currentPageNumber;

    public PdfElementListCreator() : base(DocType.ItemsList) {
    }


    /// <summary>
    /// Создать документ
    /// </summary>
    public override void Create(Project project) {
        if (project.Configurations.Count == 0)
            return;

        Configuration mainConfig = null;
        if (!project.Configurations.TryGetValue(Constants.MAIN_CONFIG_INDEX, out mainConfig))
            return;

        var dataTable = CreateDataTable(mainConfig.Specification);

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
        pdfDoc.SetDefaultPageSize(PageSize);
        doc = new iText.Layout.Document(pdfDoc, pdfDoc.GetDefaultPageSize(), true);

        int lastProcessedRow = AddFirstPage(doc, mainConfig.Graphs, dataTable);

        // TODO: remove this
        lastProcessedRow = 1;

        _currentPageNumber = 1;
        while (lastProcessedRow > 0) {
            _currentPageNumber++;
            lastProcessedRow = AddNextPage(doc, mainConfig.Graphs, dataTable, lastProcessedRow);
        }

        if (pdfDoc.GetNumberOfPages() > 2) {
            AddRegisterList(doc, mainConfig.Graphs);
        }

        doc.Close();
    }

    /// <summary>
    /// формирование таблицы данных
    /// </summary>
    /// <param name="aData"></param>
    /// <returns></returns>
    private DataTable CreateDataTable(IDictionary<string, Group> aData) {
        // только компоненты из раздела "Прочие изделия"
        Group others;
        if (aData.TryGetValue(Constants.GroupOthers, out others)) {
            DataTable table = CreateElementListDataTable("ElementListData");
            var mainсomponents = others.Components.Where(val =>
                !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));

            AddEmptyRow(table);
            FillDataTable(table, "", mainсomponents);

            foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key)) {
                // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                var сomponents = subgroup.Value.Components.Where(val =>
                    !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
                FillDataTable(table, subgroup.Value.Name, сomponents);
            }

            return table;
        }

        return null;
    }

    /// <summary>
    /// создание таблицы данных для документа Перечень элементов
    /// </summary>
    /// <param name="aDataTableName"></param>
    /// <returns></returns>
    private DataTable CreateElementListDataTable(string aDataTableName) {
        DataTable table = new DataTable(aDataTableName);
        DataColumn column = new DataColumn("id", System.Type.GetType("System.Int32"));
        column.Unique = true;
        column.AutoIncrement = true;
        column.Caption = "id";
        table.Columns.Add(column);
        table.PrimaryKey = new DataColumn[] {column};

        column = new DataColumn(Constants.ColumnPosition, System.Type.GetType("System.String"));
        column.Unique = false;
        column.Caption = "Поз. обозначение";
        column.AllowDBNull = true;
        table.Columns.Add(column);

        column = new DataColumn(Constants.ColumnName, System.Type.GetType("System.String"));
        column.Unique = false;
        column.Caption = "Наименование";
        column.AllowDBNull = true;
        table.Columns.Add(column);

        column = new DataColumn(Constants.ColumnQuantity, System.Type.GetType("System.Int32"));
        column.Unique = false;
        column.Caption = "Кол.";
        column.AllowDBNull = true;
        table.Columns.Add(column);

        column = new DataColumn(Constants.ColumnFootnote, System.Type.GetType("System.String"));
        column.Unique = false;
        column.Caption = "Примечание";
        column.AllowDBNull = true;
        table.Columns.Add(column);

        return table;
    }

    /// <summary>
    /// заполнить таблицу данных
    /// </summary>
    /// <param name="aTable"></param>
    /// <param name="aGroupName"></param>
    /// <param name="aComponents"></param>
    private void FillDataTable(DataTable aTable, string aGroupName, IEnumerable<Models.Component> aComponents) {
        // записываем компоненты в таблицу данных
        if (aComponents.Count() > 0) {
            //Cортировка компонентов по значению свойства "Позиционное обозначение"
            Models.Component[] sortComponents =
                SortFactory.GetSort(SortType.DesignatorID).Sort(aComponents.ToList()).ToArray();
            // для признаков составления наименования для данного компонента
            int[] HasStandardDoc;

            //ищем компоненты с наличием ТУ/ГОСТ в свойстве "Документ на поставку" и запоминаем номера компонентов с совпадающим значением                
            Dictionary<string /* GOST/TY string*/, List<int> /* array indexes */> StandardDic =
                FindComponentsWithStandardDoc(sortComponents, out HasStandardDoc);

            // записываем наименование группы, если есть
            AddGroupName(aTable, aGroupName);

            // записываем строки с гост/ту в начале таблицы, если они есть для нескольких компонентов
            if (!AddStandardDocsToTable(aGroupName, sortComponents, aTable, StandardDic)) {
                AddEmptyRow(aTable);
            }

            //записываем таблицу данных объединяя подрядидущие компоненты с одинаковым наименованием    
            DataRow row;
            for (int i = 0; i < sortComponents.Length;) {
                var component = sortComponents[i];
                string component_name = GetComponentName(HasStandardDoc[i] == 1, component);
                int component_count = GetComponentCount(component.GetProperty(Constants.ComponentCountDev));
                List<string> component_designators = new List<string>
                    {component.GetProperty(Constants.ComponentDesignatiorID)};

                bool same;
                int j = i + 1;
                if (j < sortComponents.Length) {
                    do {
                        var componentNext = sortComponents[j];
                        string componentNext_name = GetComponentName(HasStandardDoc[j] == 1, componentNext);

                        if (string.Equals(component_name, componentNext_name)) {
                            same = true;
                            component_count +=
                                GetComponentCount(componentNext.GetProperty(Constants.ComponentCountDev));
                            j++;
                        }
                        else
                            same = false;
                    } while (same && j < sortComponents.Length);
                }

                i = j;

                string component_designator = MakeComponentDesignatorsString(component_designators);

                row = aTable.NewRow();
                row[Constants.ColumnPosition] = component_designator;
                row[Constants.ColumnName] = component_name;
                row[Constants.ColumnQuantity] = component_count;
                row[Constants.ColumnFootnote] = component.GetProperty(Constants.ComponentNote);
                aTable.Rows.Add(row);
            }

            AddEmptyRow(aTable);
            aTable.AcceptChanges();
        }
    }

    /// <summary>
    /// поиск компонент с наличием ТУ/ГОСТ в свойстве "Документ на поставку", заполнение словаря с индексами найденных компонент для
    /// значения "Документ на поставку" и сохранение номера компонентов с совпадающим значением                
    /// </summary>
    /// <param name="aComponents">отсортированный массив компонентов</param>
    /// <param name="aHasStandardDoc">массив компонентов с отметками о наличии стандартных документов и объединения в группы</param>
    /// <returns></returns>
    private Dictionary<string, List<int>> FindComponentsWithStandardDoc(Models.Component[] aComponents,
        out int[] aHasStandardDoc) {
        Dictionary<string, List<int>> StandardDic = new Dictionary<string, List<int>>();
        aHasStandardDoc = new int[aComponents.Length];

        for (int i = 0; i < aComponents.Length; i++) {
            string docToSupply = aComponents[i].GetProperty(Constants.ComponentDoc);
            if (docToSupply == "") continue;
            if (string.Equals(docToSupply.Substring(0, 4).ToLower(), "гост") ||
                string.Equals(docToSupply.Substring(docToSupply.Length - 2, 2).ToLower(), "ту")) {
                List<int> list;
                if (StandardDic.TryGetValue(docToSupply, out list)) {
                    if (list.Count == 1) {
                        aHasStandardDoc[list.First()] = 2;
                    }

                    list.Add(i);
                    aHasStandardDoc[i] = 2;
                }
                else {
                    list = new List<int> {i};
                    aHasStandardDoc[i] = 1;
                    StandardDic.Add(docToSupply, list);
                }
            }
        }

        return StandardDic;
    }

    /// <summary>
    /// добавить пустую строку в таблицу данных
    /// </summary>
    /// <param name="aTable"></param>
    private void AddEmptyRow(DataTable aTable) {
        DataRow row = aTable.NewRow();
        row[Constants.ColumnName] = string.Empty;
        row[Constants.ColumnPosition] = string.Empty;
        row[Constants.ColumnQuantity] = 0;
        row[Constants.ColumnFootnote] = string.Empty;
        aTable.Rows.Add(row);
    }

    /// <summary>
    /// добавить имя группы в таблицу
    /// </summary>
    /// <param name="aTable"></param>
    /// <param name="aGroupName"></param>
    private void AddGroupName(DataTable aTable, string aGroupName) {
        if (!string.IsNullOrEmpty(aGroupName)) {
            DataRow row = aTable.NewRow();
            row[Constants.ColumnName] = aGroupName;
            aTable.Rows.Add(row);
        }
    }

    /// <summary>
    /// добавить в таблицу данных стандартные документы на поставку при наличии перед перечнем компонентов
    /// </summary>
    /// <param name="aGroupName">имя группы</param>
    /// <param name="aComponents">список компонентов</param>
    /// <param name="aTable">таблица данных</param>
    /// <param name="aStandardDic">словарь со стандартными документами на поставку</param>
    /// <returns>true - стандартные документы добавлены </returns>
    private bool AddStandardDocsToTable(string aGroupName, Models.Component[] aComponents, DataTable aTable,
        Dictionary<string, List<int>> aStandardDic) {
        bool isApplied = false;
        DataRow row;
        foreach (var item in aStandardDic) {
            if (item.Value.Count() > 1) {
                row = aTable.NewRow();
                var index = item.Value.First();
                string name = $"{aGroupName} {aComponents[index].GetProperty(Constants.ComponentType)} {item.Key}";
                row[Constants.ColumnName] = name;
                aTable.Rows.Add(row);
                isApplied = true;
            }
        }

        return isApplied;
    }

    /// <summary>
    /// получить имя компонента для столбца "Наименование"
    /// </summary>
    /// <param name="aHasStandardDoc">признак наличия ГОСТ/ТУ символов в документе на поставку</param>
    /// <param name="component">компонент</param>
    /// <returns></returns>
    private string GetComponentName(bool aHasStandardDoc, Models.Component component) {
        return (aHasStandardDoc)
            ? $"{component.GetProperty(Constants.ComponentName)} {component.GetProperty(Constants.ComponentDoc)}"
            : component.GetProperty(Constants.ComponentName);
    }

    /// <summary>
    /// получить количество компонентов
    /// </summary>
    /// <param name="aCountStr"></param>
    /// <returns></returns>
    private int GetComponentCount(string aCountStr) {
        int count = 1;
        if (!string.IsNullOrEmpty(aCountStr)) {
            if (!Int32.TryParse(aCountStr, out count)) {
                count = 1;
                //throw new Exception($"Не удалось распарсить значение свойства \"Количество на изд.\" для компонента с именем {component_name}");
            }
        }

        return count;
    }

    /// <summary>
    /// составить строку для столбца "Поз. обозначение"
    /// </summary>
    /// <param name="aDesignators">список позиционных обозначений всех индентичных элементов</param>
    /// <returns></returns>
    private string MakeComponentDesignatorsString(List<string> aDesignators) {
        string designator = string.Empty;
        if (aDesignators.Count() == 1)
            designator = aDesignators.First();
        else if (aDesignators.Count() == 2)
            designator = $"{aDesignators.First()},{aDesignators.Last()}";
        else
            designator = $"{aDesignators.First()} - {aDesignators.Last()}";

        return designator;
    }

    /// <summary>
    /// создать шаблон первой страницы документа
    /// (таблица данных в шаблоне не создается)
    /// </summary>
    /// <param name="aPageSize">Размер страницы</param>
    /// <returns></returns>
    internal PdfDocument CreateTemplateFirstPage(PageSize aPageSize) {
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
    internal PdfDocument CreateTemplateNextPage(PageSize aPageSize) {
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
    internal PdfDocument CreateRegistrationPage() {
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
    /// <returns>номер последней записанной строки. Если 0 - то достигнут конец таблицы данных</returns>
    internal override int AddFirstPage(iText.Layout.Document aInDoc, IDictionary<string, string> aGraphs, DataTable aData) {
        SetPageMargins(aInDoc);

        int lastProcessedRow = 0;
        // добавить таблицу с данными
        aInDoc.Add(CreateDataTable(aData, true, 0, out lastProcessedRow));

        // нарисовать недостающую линию
        var fromLeft = 19.3f * PdfDefines.mmA4 + TITLE_BLOCK_WIDTH;
        Canvas canvas = new Canvas(new PdfCanvas(pdfDoc.GetFirstPage()),
            new Rectangle(fromLeft, BOTTOM_MARGIN + (15 + 5 + 5 + 15 + 8 + 14) * PdfDefines.mmA4 + 2f, 2, 30));
        canvas.Add(new LineSeparator(new SolidLine(THICK_LINE_WIDTH)).SetWidth(15)
            .SetRotationAngle(DegreesToRadians(90)));


        // добавить таблицу с основной надписью для первой старницы
        aInDoc.Add(CreateFirstTitleBlock(PageSize, aGraphs, 0));

        // добавить таблицу с верхней дополнительной графой
        aInDoc.Add(CreateTopAppendGraph(PageSize, aGraphs));

        // добавить таблицу с нижней дополнительной графой
        aInDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));


        var style = new Style().SetItalic().SetFontSize(12).SetFont(f1).SetTextAlignment(TextAlignment.CENTER);
        var p = new Paragraph(GetGraphByName(aGraphs, Constants.GRAPH_PJOJECT)).SetRotationAngle(DegreesToRadians(90))
            .AddStyle(style).SetFixedPosition(10 * PdfDefines.mmA4 + 2,
                TOP_APPEND_GRAPH_BOTTOM_FIRST_PAGE + 45 * PdfDefines.mmA4, 100);
        aInDoc.Add(p);
        p = new Paragraph("Копировал").AddStyle(style)
            .SetFixedPosition((7 + 10 + 32 + 15 + 10 + 14) * PdfDefines.mmA4, 0, 100);
        aInDoc.Add(p);
        p = new Paragraph("Формат А4").AddStyle(style)
            .SetFixedPosition((7 + 10 + 32 + 15 + 10 + 70) * PdfDefines.mmA4 + 20, 0, 100);
        aInDoc.Add(p);

        return lastProcessedRow;
    }

    /// <summary>
    /// добавить к документу последующие страницы
    /// </summary>
    /// <param name="aInPdfDoc">a in PDF document.</param>
    /// <returns></returns>
    internal override int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs,
        DataTable aData, int aStartRow) {
        aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        SetPageMargins(aInPdfDoc);

        // добавить таблицу с данными
        int lastNextProcessedRow;
        var dataTable = CreateDataTable(aData, false, aStartRow, out lastNextProcessedRow);
        dataTable.SetFixedPosition(19.3f * PdfDefines.mmA4, BOTTOM_MARGIN + 16 * PdfDefines.mmA4,
            TITLE_BLOCK_WIDTH + 2f);
        aInPdfDoc.Add(dataTable);


        // добавить таблицу с основной надписью для последуюших старницы
        aInPdfDoc.Add(CreateNextTitleBlock(PageSize, aGraphs));

        // добавить таблицу с нижней дополнительной графой
        aInPdfDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));

        // TODO: remove this
        lastNextProcessedRow = 0;

        return lastNextProcessedRow;
    }

    private void SetPageMargins(iText.Layout.Document aDoc) {
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

        int remainingPdfTabeRows = (firstPage) ? CountStringsOnFirstPage : CountStringsOnNextPage;
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

        tbl.SetFixedPosition(19.3f * PdfDefines.mmA4, 78 * PdfDefines.mmA4 + 0.5f, TITLE_BLOCK_WIDTH + 2f);


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
    private List<string> SplitNameByWidth(float aLength, float aFontSize, PdfFont aFont, string aString) {
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
    private void GetLimitSubstring(List<string> name_strings, float maxLength, float currLength, string aFullName) {
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