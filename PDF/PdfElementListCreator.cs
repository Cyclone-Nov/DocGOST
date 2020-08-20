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
using System.ComponentModel;

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

            int lastProcessedRow = AddFirstPage(doc, mainConfig.Graphs, dataTable);
            while (lastProcessedRow > 0)
            {
                lastProcessedRow = AddNextPage(doc, mainConfig.Graphs, dataTable, lastProcessedRow);
            }

            if (pdfDoc.GetNumberOfPages() > 2)
            {
                AddRegisterList(doc, mainConfig.Graphs);
            }

            doc.Close();            
        }

        /// <summary>
        /// формирование таблицы данных
        /// </summary>
        /// <param name="aData"></param>
        /// <returns></returns>
        private DataTable CreateDataTable(IDictionary<string, Group> aData)
        {
            // только компоненты из раздела "Прочие изделия"
            Group others;
            if (aData.TryGetValue(Constants.GroupOthers, out others))
            {
                DataTable table = CreateElementListDataTable("ElementListData");                
                var mainсomponents = others.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));

                AddEmptyRow(table);
                FillDataTable(table, "" ,mainсomponents);
                
                foreach (var subgroup in others.SubGroups.OrderBy(key => key.Key))
                {
                    // выбираем только компоненты с заданными занчением для свойства "Позиционое обозначение"
                    var сomponents = subgroup.Value.Components.Where(val => !string.IsNullOrEmpty(val.GetProperty(Constants.ComponentDesignatiorID)));
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
        private DataTable CreateElementListDataTable(string aDataTableName)
        {
            DataTable table = new DataTable(aDataTableName);
            DataColumn column = new DataColumn("id", System.Type.GetType("System.Int32"));            
            column.Unique = true;
            column.AutoIncrement = true;                        
            column.Caption = "id";
            table.Columns.Add(column);
            table.PrimaryKey = new DataColumn[] { column };

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
        private void FillDataTable(DataTable aTable, string aGroupName,IEnumerable<Models.Component> aComponents)
        {            
            // записываем компоненты в таблицу данных
            if (aComponents.Count() > 0)
            {
                //Cортировка компонентов по значению свойства "Позиционное обозначение"
                Models.Component[] sortComponents = SortFactory.GetSort(SortType.DesignatorID).Sort(aComponents.ToList()).ToArray();
                // для признаков составления наименования для данного компонента
                int[] HasStandardDoc;

                //ищем компоненты с наличием ТУ/ГОСТ в свойстве "Документ на поставку" и запоминаем номера компонентов с совпадающим значением                
                Dictionary<string /* GOST/TY string*/, List<int> /* array indexes */> StandardDic = FindComponentsWithStandardDoc(sortComponents, out HasStandardDoc);

                // записываем наименование группы, если есть
                AddGroupName(aTable, aGroupName);

                // записываем строки с гост/ту в начале таблицы, если они есть для нескольких компонентов
                AddStandardDocsToTable(aGroupName, sortComponents, aTable, StandardDic);
                // AddEmptyRow(aTable);

                //записываем таблицу данных объединяя подрядидущие компоненты с одинаковым наименованием    
                DataRow row;
                for (int i = 0; i < sortComponents.Length;)
                {
                    var component = sortComponents[i];
                    string component_name = GetComponentName(HasStandardDoc[i] == 1, component);
                    int component_count = GetComponentCount(component.GetProperty(Constants.ComponentCountDev));                    
                    List<string> component_designators = new List<string> { component.GetProperty(Constants.ComponentDesignatiorID) };
                    
                    bool same;
                    int j = i + 1;
                    if (j < sortComponents.Length)
                    {
                        do
                        {
                            var componentNext = sortComponents[j];
                            string componentNext_name = GetComponentName(HasStandardDoc[j] == 1, componentNext);

                            if (string.Equals(component_name, componentNext_name))
                            {
                                same = true;
                                component_count += GetComponentCount(componentNext.GetProperty(Constants.ComponentCountDev));
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
        private Dictionary<string, List<int>> FindComponentsWithStandardDoc(Models.Component[] aComponents, out int[] aHasStandardDoc)
        {
            Dictionary<string, List<int>> StandardDic = new Dictionary<string, List<int>>();
            aHasStandardDoc = new int[aComponents.Length];

            for (int i = 0; i < aComponents.Length; i++)
            {
                string docToSupply = aComponents[i].GetProperty(Constants.ComponentDoc);
                if (string.Equals(docToSupply.Substring(0, 4).ToLower(), "гост") ||
                    string.Equals(docToSupply.Substring(docToSupply.Length - 2, 2).ToLower(), "ту"))
                {
                    List<int> list;
                    if (StandardDic.TryGetValue(docToSupply, out list))
                    {
                        if (list.Count == 1)
                        {
                            aHasStandardDoc[list.First()] = 2;
                        }
                        list.Add(i);
                        aHasStandardDoc[i] = 2;
                    }
                    else
                    {
                        list = new List<int> { i };
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
        private void AddEmptyRow(DataTable aTable)
        {            
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
        private void AddGroupName(DataTable aTable, string aGroupName)
        {
            if (!string.IsNullOrEmpty(aGroupName))
            {
                DataRow row = aTable.NewRow();
                row[Constants.ColumnName] = aGroupName;
                aTable.Rows.Add(row);
            }
        }

        /// <summary>
        /// добавить в таблиц данных стандартные документы на поставку при наличии перед перечнем компонентов
        /// </summary>
        /// <param name="aGroupName">имя группы</param>
        /// <param name="aComponents">список компонентов</param>
        /// <param name="aTable">таблица данных</param>
        /// <param name="aStandardDic">словарь со стандартными документами на поставку</param>
        private void AddStandardDocsToTable(string aGroupName, Models.Component[] aComponents, DataTable aTable, Dictionary<string, List<int>> aStandardDic)
        {
            DataRow row;
            foreach (var item in aStandardDic)
            {
                if (item.Value.Count() > 1)
                {
                    row = aTable.NewRow();
                    var index = item.Value.First();
                    string name = $"{aGroupName} {aComponents[index].GetProperty(Constants.ComponentType)} {item.Key}";
                    row[Constants.ColumnName] = name;
                    aTable.Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// получить имя компонента для столбца "Наименование"
        /// </summary>
        /// <param name="aHasStandardDoc">признак наличия ГОСТ/ТУ символов в документе на поставку</param>
        /// <param name="component">компонент</param>
        /// <returns></returns>
        private string GetComponentName(bool aHasStandardDoc, Models.Component component)
        {
            return (aHasStandardDoc) ? $"{component.GetProperty(Constants.ComponentName)} {component.GetProperty(Constants.ComponentDoc)}" 
                                     : component.GetProperty(Constants.ComponentName);
        }

        /// <summary>
        /// получить количество компонентов
        /// </summary>
        /// <param name="aCountStr"></param>
        /// <returns></returns>
        private int GetComponentCount(string aCountStr)
        {
            int count = 1;
            if (!string.IsNullOrEmpty(aCountStr))
            {
                if (!Int32.TryParse(aCountStr, out count))
                {
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
        private string MakeComponentDesignatorsString(List<string> aDesignators)
        {
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

            int lastProcessedRow = 0;
            // добавить таблицу с данными
            aInDoc.Add(CreateDataTable(aData, true, 0, out lastProcessedRow));

            // добавить таблицу с основной надписью для первой старницы
            aInDoc.Add(CreateFirstTitleBlock(PageSize, aGraphs, 0));

            // добавить таблицу с верхней дополнительной графой
            aInDoc.Add(CreateTopAppendGraph(PageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));
                        

            return lastProcessedRow;
        }

        /// <summary>
        /// добавить к документу последующие страницы
        /// </summary>
        /// <param name="aInPdfDoc">a in PDF document.</param>
        /// <returns></returns>
        internal override int AddNextPage(iText.Layout.Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aStartRow)
        {            
            aInPdfDoc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            SetPageMargins(aInPdfDoc);

            // добавить таблицу с основной надписью для последуюших старницы
            aInPdfDoc.Add(CreateFirstTitleBlock(PageSize, aGraphs, 0));

            // добавить таблицу с верхней дополнительной графой
            aInPdfDoc.Add(CreateTopAppendGraph(PageSize, aGraphs));

            // добавить таблицу с нижней дополнительной графой
            aInPdfDoc.Add(CreateBottomAppendGraph(PageSize, aGraphs));

            // добавить таблицу с данными
            int lastNextProcessedRow;
            aInPdfDoc.Add(CreateDataTable(aData, false, aStartRow, out lastNextProcessedRow));

            return lastNextProcessedRow;
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

        private static Cell ApplyAppendGraph(Cell c, string text) {
            c.Add(
                new Paragraph(text).
                    SetFont(f1).
                    SetFontSize(14).
                    SetRotationAngle(DegreesToRadians(90)).
                    SetFixedLeading(10).
                    SetPadding(0).
                    SetPaddingRight(-10).
                    SetPaddingLeft(-10).
                    SetMargin(0).
                    SetItalic().
                    SetWidth(60 * PdfDefines.mmA4).
                    SetTextAlignment(TextAlignment.CENTER));

            c.SetHorizontalAlignment(HorizontalAlignment.CENTER).
             SetVerticalAlignment(VerticalAlignment.MIDDLE).
             SetMargin(0).
             SetPadding(0).
             SetHeight(60 * PdfDefines.mmA4).
             SetBorder(new SolidBorder(2));

            return c;
        }

        /// <summary>
        /// создать таблицу для верхней дополнительной графы
        /// </summary>
        /// <returns></returns>
        private Table CreateTopAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs)
        {
            float[] columnSizes = {5 * PdfDefines.mmA4, 7 * PdfDefines.mmA4 };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            
            var tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Перв. примен."));
            tbl.AddCell(new Cell());

            tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Справ. №"));
            tbl.AddCell(new Cell());

            return tbl;
        }

        /// <summary>
        /// создать таблицу для нижней дополнительной графы
        /// </summary>
        /// <returns></returns>
        private Table CreateBottomAppendGraph(PageSize aPageSize, IDictionary<string, string> aGraphs)
        {
            float[] columnSizes = {5 * PdfDefines.mmA4, 7 * PdfDefines.mmA4 };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));

            var tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Подп. и дата"));
            tbl.AddCell(new Cell());

            tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Инв. № дубл."));
            tbl.AddCell(new Cell());
            
            tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Взам. инв. №"));
            tbl.AddCell(new Cell());
            
            tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Подп. и дата"));
            tbl.AddCell(new Cell());
            
            tmpCell = new Cell();
            tbl.AddCell(ApplyAppendGraph(tmpCell, "Инв № подл."));
            tbl.AddCell(new Cell());

            return tbl;
        }

        /// <summary>
        /// создание таблицы данных
        /// </summary>
        /// <param name="aData">таблица данных</param>
        /// <param name="firstPage">признак первой или последующих страниц</param>
        /// <param name="aStartRow">строка таблицы данных с которой надо начинать запись в PDF страницу</param>
        /// <param name="outLastProcessedRow">последняя обработанная строка таблицы данных</param>
        /// <returns></returns>
        private Table CreateDataTable(DataTable aData, bool firstPage, int aStartRow, out int outLastProcessedRow)
        {            
            float[] columnSizes = { 20 * PdfDefines.mmA4, 110 * PdfDefines.mmA4, 10 * PdfDefines.mmA4, 45 * PdfDefines.mmA4 };
            Table tbl = new Table(UnitValue.CreatePointArray(columnSizes));
            tbl.SetMargin(0).SetPadding(0);

            // add header
            var headerTextStyle = new Style().SetTextAlignment(TextAlignment.CENTER).SetItalic().SetFont(f1).SetFontSize(16);
            Cell cell = AddEmptyCell(1, 1).AddStyle(headerTextStyle).SetMargin(0).SetPaddings(-2, -2, -2, -2).SetHeight(15 * PdfDefines.mmA4h);            
            tbl.AddHeaderCell(cell.Clone(false).Add(new Paragraph("Поз. обозначение")));
            tbl.AddHeaderCell(cell.Clone(false).Add(new Paragraph("Наименование")));
            tbl.AddHeaderCell(cell.Clone(false).Add(new Paragraph("Кол.")));
            tbl.AddHeaderCell(cell.Clone(false).Add(new Paragraph("Примечание")));

            // fill table
            Cell groupCell = AddEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 0).SetHeight(8* PdfDefines.mmA4h).
                                                            SetTextAlignment(TextAlignment.CENTER).SetItalic().SetBold().SetUnderline().SetFont(f1).SetFontSize(14);
            Cell mainCell = AddEmptyCell(1, 1, 2, 2, 0, 1).SetMargin(0).SetPaddings(0, 0, 0, 0).SetHeight(8 * PdfDefines.mmA4h).
                                                           SetTextAlignment(TextAlignment.LEFT).SetItalic().SetFont(f1).SetFontSize(14);
            //UnitValue uFontSize = mainCell.GetProperty<UnitValue>(24); // 24 - index for FontSize property
            //float fontSize = uFontSize.GetValue();
            float fontSize = 14;
            PdfFont font = mainCell.GetProperty<PdfFont>(20); // 20 - index for Font property

            int remainingPdfTabeRows = (firstPage) ? CountStringsOnFirstPage : CountStringsOnNextPage;
            outLastProcessedRow = aStartRow;            

            foreach (DataRow row in aData.Rows)
            {
                string position = (row[Constants.ColumnPosition] == System.DBNull.Value) ? string.Empty : (string)row[Constants.ColumnPosition];
                string name = (row[Constants.ColumnName] == System.DBNull.Value) ? string.Empty : (string)row[Constants.ColumnName];
                int quantity = (row[Constants.ColumnQuantity] == System.DBNull.Value) ? 0 : (int)row[Constants.ColumnQuantity];
                string note = (row[Constants.ColumnFootnote] == System.DBNull.Value) ? string.Empty : (string)row[Constants.ColumnFootnote];

                if (string.IsNullOrEmpty(name))
                {
                    // это пустая строка   
                    AddEmptyRowToPdfTable(tbl,1, 4, mainCell);
                    remainingPdfTabeRows--;                    
                }
                else if (string.IsNullOrEmpty(position))
                {
                    // это наименование группы
                    if (remainingPdfTabeRows > 4) // если есть место для записи более 4 строк то записываем группу, иначе выходим
                    {
                        tbl.AddCell(mainCell.Clone(false));
                        tbl.AddCell(groupCell.Clone(true).Add(new Paragraph(name)));
                        tbl.AddCell(mainCell.Clone(false));
                        tbl.AddCell(mainCell.Clone(false));
                        remainingPdfTabeRows--;
                    }
                    else
                        break;
                }
                else
                {
                    // оценим длину наименования
                    string[] namestrings = GetNameStrings(110 * PdfDefines.mmA4, fontSize, font, name).ToArray();
                    if (namestrings.Length <= remainingPdfTabeRows)
                    {
                        tbl.AddCell(mainCell.Clone(false).Add(new Paragraph(position)));
                        tbl.AddCell(mainCell.Clone(false).Add(new Paragraph(namestrings[0])));
                        tbl.AddCell(mainCell.Clone(false).Add(new Paragraph(quantity.ToString())));
                        tbl.AddCell(mainCell.Clone(false).Add(new Paragraph(note)));
                        remainingPdfTabeRows--;

                        if (namestrings.Length > 1)
                        {
                            for (int i = 1; i < namestrings.Length; i++)
                            {
                                tbl.AddCell(mainCell.Clone(false));
                                tbl.AddCell(mainCell.Clone(false).Add(new Paragraph(namestrings[i])));
                                tbl.AddCell(mainCell.Clone(false));
                                tbl.AddCell(mainCell.Clone(false));
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
            {
                AddEmptyRowToPdfTable(tbl, remainingPdfTabeRows, 4, mainCell, true);
            }

            if(outLastProcessedRow == aData.Rows.Count)
            {
                outLastProcessedRow = 0;
            }

            tbl.SetFixedPosition(20 * PdfDefines.mmA4, 77 * PdfDefines.mmA4, 185 * PdfDefines.mmA4);

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
        private void AddEmptyRowToPdfTable(Table aTable, int aRows, int aColumns, Cell aTemplateCell, bool aLastRowIsFinal = false)
        {
            int bodyRowsCnt = aRows - 1;
            for (int i = 0; i < bodyRowsCnt*aColumns; i++)
            {
                aTable.AddCell(aTemplateCell.Clone(false));
            }

            int borderWidth = (aLastRowIsFinal) ? 2 : 1;
            for (int i = 0; i < aColumns;i++)
                aTable.AddCell(aTemplateCell.Clone(false).SetBorderBottom(new SolidBorder(borderWidth)));
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
               

        /// <summary>
        /// Gets the name strings.
        /// </summary>
        /// <param name="aLength">a length.</param>
        /// <param name="aFontSize">Size of a font.</param>
        /// <param name="aFont">a font.</param>
        /// <param name="aName">a name.</param>
        /// <returns></returns>
        private List<string> GetNameStrings(float aLength, float aFontSize, PdfFont aFont, string aName)
        {
            List<string> name_strings = new List<string>();
            int default_padding = 4;
            float maxLength = aLength - default_padding;
            float currLength = aFont.GetWidth(aName, aFontSize);

            GetLimitSubstring(name_strings, maxLength, currLength, aName );
            

            return name_strings;
        }


        private void GetLimitSubstring(List<string> name_strings, float maxLength, float currLength, string aFullName)
        {
            if (currLength < maxLength)
            {
                name_strings.Add(aFullName);
            }
            else
            {
                string fullName = aFullName;
                int symbOnMaxLength = (int)((fullName.Length / currLength) * maxLength);
                string partName = fullName.Substring(0, symbOnMaxLength);
                int index = partName.LastIndexOfAny(new char[] { ' ', '-', '.' });
                name_strings.Add(fullName.Substring(0, index));
                fullName = fullName.Substring(index + 1);
                currLength = fullName.Length;
                GetLimitSubstring(name_strings, maxLength, currLength, fullName);
            }
        }

    }
}
