﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Data.SQLite;
using System.IO;
using Microsoft.Win32;
using iTextSharp.text.pdf;
using iTextSharp.text;
using DocGOST.Data;



namespace DocGOST
{
    class PdfOperations
    {

        const int perech_first_page_rows_count = 23;
        const int perech_subseq_page_rows_count = 29;

        Font normal, big, veryBig;
        Database project;



        public PdfOperations(string projectPath)
        {
            BaseFont fontGostA = BaseFont.CreateFont("GOST_A.TTF", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            normal = new Font(fontGostA, 11f, Font.ITALIC, BaseColor.BLACK);
            big = new Font(fontGostA, 18f, Font.ITALIC, BaseColor.BLACK);
            veryBig = new Font(fontGostA, 22f, Font.ITALIC, BaseColor.BLACK);

            project = new Data.Database(projectPath);
        }

        public void CreatePerechen(string pdfPath, int startPage, bool addListRegistr)
        {
            var document = new Document(PageSize.A4);
            var writer = PdfWriter.GetInstance(document, new FileStream(pdfPath, FileMode.Create));

            document.Open();

           DrawCommonStampA4(document, writer);

            DrawFirstPageStampA4(document, writer, startPage);

            DrawPerechenTable(document, writer, 0);

            int numberOfValidStrings = project.GetPerechenLength();
            int totalPageCount = 1 + (numberOfValidStrings - perech_first_page_rows_count) / perech_subseq_page_rows_count;

            if (numberOfValidStrings > perech_first_page_rows_count)
                for (int i = 0; i < totalPageCount; i++)
                {
                    document.NewPage();

                    DrawCommonStampA4(document, writer);
                    DrawSubsequentStampA4(document, writer, i + 1 + startPage);
                    DrawPerechenTable(document, writer, 1 + i);

                }
            if (addListRegistr)
            {
                document.NewPage();

                DrawCommonStampA4(document, writer);
                DrawSubsequentStampA4(document, writer, totalPageCount + 1 + startPage);
                DrawListRegistrTable(document, writer);
            }
            document.Close();
            writer.Close();
        }

        private void DrawCommonStampA4(iTextSharp.text.Document doc, PdfWriter wr)
        {

            PdfContentByte cb = wr.DirectContent;

            // Черчение рамки:
            float mm_A4 = doc.PageSize.Width / 210;

            cb.MoveTo(20 * mm_A4, 5 * mm_A4);
            cb.LineTo(20 * mm_A4, 292 * mm_A4);//Левая граница
            cb.LineTo(205 * mm_A4, 292 * mm_A4);//Верхняя граница
            cb.LineTo(205 * mm_A4, 5 * mm_A4);//Правая граница
            cb.LineTo(20 * mm_A4, 5 * mm_A4);//Нижняя граница
            cb.Stroke();

            #region Рисование табицы с графами 19-23            
            PdfPTable table19_23 = new PdfPTable(2);
            table19_23.TotalWidth = 12 * mm_A4;
            table19_23.LockedWidth = true;
            float[] tbldWidths = new float[2];
            tbldWidths[0] = 5;
            tbldWidths[1] = 7;
            table19_23.SetWidths(tbldWidths);

            // Заполнение графы 19:
            PdfPCell currentCell = new PdfPCell(new Phrase("Инв. № подл.", normal));
            currentCell.BorderWidth = 1;
            currentCell.Rotation = 90;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 25 * mm_A4;
            table19_23.AddCell(currentCell);
            string gr19Text = project.GetOsnNadpisItem("19").perechenValue;
            currentCell.Phrase = new Phrase(gr19Text, normal);
            currentCell.PaddingLeft = 2;
            table19_23.AddCell(currentCell);
            table19_23.WriteSelectedRows(0, 1, 8 * mm_A4, 30 * mm_A4, cb);
            // Заполнение графы 20:
            currentCell.Phrase = new Phrase("Подп. и дата", normal);
            currentCell.PaddingLeft = 0;
            currentCell.FixedHeight = 35 * mm_A4;
            table19_23.AddCell(currentCell);
            currentCell.Phrase = new Phrase("", normal);
            currentCell.PaddingLeft = 2;
            table19_23.AddCell(currentCell);
            table19_23.WriteSelectedRows(1, 2, 8 * mm_A4, 65 * mm_A4, cb);
            // Заполнение графы 21:
            currentCell.Phrase = new Phrase("Взам. инв. №", normal);
            currentCell.PaddingLeft = 0;
            currentCell.FixedHeight = 25 * mm_A4;
            table19_23.AddCell(currentCell);
            string gr21Text = project.GetOsnNadpisItem("21").perechenValue;
            currentCell.Phrase = new Phrase(gr21Text, normal);
            currentCell.PaddingLeft = 2;
            table19_23.AddCell(currentCell);
            table19_23.WriteSelectedRows(2, 3, 8 * mm_A4, 90 * mm_A4, cb);
            // Заполнение графы 22:
            currentCell.Phrase = new Phrase("Инв. № дубл.", normal);
            currentCell.PaddingLeft = 0;
            currentCell.FixedHeight = 25 * mm_A4;
            table19_23.AddCell(currentCell);
            string gr22Text = project.GetOsnNadpisItem("22").perechenValue;
            currentCell.Phrase = new Phrase(gr22Text, normal);
            currentCell.PaddingLeft = 2;
            table19_23.AddCell(currentCell);
            table19_23.WriteSelectedRows(3, 4, 8 * mm_A4, 115 * mm_A4, cb);
            // Заполнение графы 23:
            currentCell.Phrase = new Phrase("Подп. и дата", normal);
            currentCell.PaddingLeft = 0;
            currentCell.FixedHeight = 35 * mm_A4;
            table19_23.AddCell(currentCell);
            currentCell.Phrase = new Phrase("", normal);
            currentCell.PaddingLeft = 2;
            table19_23.AddCell(currentCell);
            table19_23.WriteSelectedRows(4, 5, 8 * mm_A4, 150 * mm_A4, cb);

            #endregion


            #region Рисование табицы с графами 31, 32            
            PdfPTable table31_32 = new PdfPTable(2);
            table31_32.TotalWidth = 130 * mm_A4;
            table31_32.LockedWidth = true;
            tbldWidths[0] = 80;
            tbldWidths[1] = 50;
            table31_32.SetWidths(tbldWidths);

            // Заполнение графы 31:
            currentCell = new PdfPCell(new Phrase("Копировал", normal));
            currentCell.BorderWidth = 0;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            table31_32.AddCell(currentCell);
            // Заполнение графы 31:
            currentCell.Phrase = new Phrase("Формат А4", normal);
            currentCell.PaddingLeft = 2;
            table31_32.AddCell(currentCell);
            table31_32.WriteSelectedRows(0, 1, 75 * mm_A4, 5 * mm_A4, cb);


            #endregion
        }

        private void DrawFirstPageStampA4(iTextSharp.text.Document doc, PdfWriter wr, int pageNumber)
        {


            PdfContentByte cb = wr.DirectContent;

            float[] tbldWidths;
            float mm_A4 = doc.PageSize.Width / 210;


            #region Рисование табицы с графами 24,25            
            PdfPTable table24_25 = new PdfPTable(2);
            table24_25.TotalWidth = 12 * mm_A4;
            table24_25.LockedWidth = true;
            tbldWidths = new float[2];
            tbldWidths[0] = 5;
            tbldWidths[1] = 7;
            table24_25.SetWidths(tbldWidths);

            // Заполнение графы 24:
            PdfPCell currentCell = new PdfPCell(new Phrase("Справ. №", normal));
            currentCell.BorderWidth = 1;
            currentCell.Rotation = 90;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 60 * mm_A4;
            table24_25.AddCell(currentCell);
            string gr24Text = project.GetOsnNadpisItem("24").perechenValue;
            currentCell.Phrase = new Phrase(gr24Text, normal);
            currentCell.PaddingLeft = 2;
            table24_25.AddCell(currentCell);
            table24_25.WriteSelectedRows(0, 1, 8 * mm_A4, 232 * mm_A4, cb);
            // Заполнение графы 25:
            currentCell.Phrase = new Phrase("Перв. примен.", normal);
            currentCell.PaddingLeft = 0;
            currentCell.FixedHeight = 60 * mm_A4;
            table24_25.AddCell(currentCell);
            string gr25Text = project.GetOsnNadpisItem("25").perechenValue;
            currentCell.Phrase = new Phrase(gr25Text, normal);
            currentCell.PaddingLeft = 2;
            table24_25.AddCell(currentCell);
            table24_25.WriteSelectedRows(1, 2, 8 * mm_A4, 292 * mm_A4, cb);
            #endregion

            #region Рисование графы 1           
            PdfPTable table1 = new PdfPTable(1);
            table1.TotalWidth = 70 * mm_A4;
            table1.LockedWidth = true;

            //Определяем, сколько строчек нужно для наименования изделия:
            int kolvoStrGg1 = 2;
            int naimenovaieMaxLength = 25;
            string naimenovanieStr1 = "", naimenovanieStr2 = "";
            string gr1aText = project.GetOsnNadpisItem("1a").perechenValue;
            if (gr1aText.Length > naimenovaieMaxLength)
            {
                kolvoStrGg1 = 3;


                string[] naimenovanieStrings = gr1aText.Split(' '); 
                foreach (string currentString in naimenovanieStrings)
                {
                    if (naimenovanieStr1.Length + currentString.Length + 1 < naimenovaieMaxLength) naimenovanieStr1 += currentString + " ";
                    else naimenovanieStr2 += currentString + " ";
                }
            }

            // Заполнение графы 1:
            if (kolvoStrGg1 == 2)
            {
                currentCell = new PdfPCell(new Phrase(gr1aText, big));
                currentCell.BorderWidth = 1;
                currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
                currentCell.HasFixedHeight();
                currentCell.Padding = 0;
                currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
                currentCell.FixedHeight = 18 * mm_A4;
                table1.AddCell(currentCell);
                table1.WriteSelectedRows(0, 1, 85 * mm_A4, 30 * mm_A4, cb);
                string gr1bText = project.GetOsnNadpisItem("1b").perechenValue;
                currentCell.Phrase = new Phrase(gr1bText, normal);
                currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                currentCell.BorderWidth = 1;
                currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
                currentCell.FixedHeight = 7 * mm_A4;
                table1.AddCell(currentCell);
                table1.WriteSelectedRows(1, 2, 85 * mm_A4, 12 * mm_A4, cb);
            }
            else
            {
                currentCell = new PdfPCell(new Phrase(naimenovanieStr1, big));
                currentCell.BorderWidth = 1;
                currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
                currentCell.HasFixedHeight();
                currentCell.Padding = 0;
                currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
                currentCell.FixedHeight = 10 * mm_A4;
                table1.AddCell(currentCell);
                table1.WriteSelectedRows(0, 1, 85 * mm_A4, 30 * mm_A4, cb);
                currentCell.Phrase = new Phrase(naimenovanieStr2, big);
                currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
                currentCell.FixedHeight = 10 * mm_A4;
                table1.AddCell(currentCell);
                table1.WriteSelectedRows(1, 2, 85 * mm_A4, 20 * mm_A4, cb);
                string gr1bText = project.GetOsnNadpisItem("1b").perechenValue;
                currentCell.Phrase = new Phrase(gr1bText, normal);
                currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
                currentCell.FixedHeight = 5 * mm_A4;
                table1.AddCell(currentCell);
                table1.WriteSelectedRows(2, 3, 85 * mm_A4, 10 * mm_A4, cb);
            }

            #endregion

            #region Рисование графы 2           
            PdfPTable table2 = new PdfPTable(1);
            table2.TotalWidth = 120 * mm_A4;
            table2.LockedWidth = true;


            // Заполнение графы 2:
            string gr2Text = project.GetOsnNadpisItem("2").perechenValue;
            currentCell = new PdfPCell(new Phrase(gr2Text, veryBig));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.PaddingBottom = 6;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 15 * mm_A4;
            table2.AddCell(currentCell);
            table2.WriteSelectedRows(0, 1, 85 * mm_A4, 45 * mm_A4, cb);
            #endregion

            #region Рисование табицы с графами 4,7,8            
            PdfPTable table4_8 = new PdfPTable(3);
            table4_8.TotalWidth = 50 * mm_A4;
            table4_8.LockedWidth = true;
            tbldWidths = new float[3];
            tbldWidths[0] = 15;
            tbldWidths[1] = 15;
            tbldWidths[2] = 20;
            table4_8.SetWidths(tbldWidths);

            // Заполнение заголовков граф 4,7,8:
            currentCell = new PdfPCell(new Phrase("Лит.", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            table4_8.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Лист", normal);
            table4_8.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Листов", normal);
            table4_8.AddCell(currentCell);
            // Заполнение граф 4,7,8:
            currentCell.Phrase = new Phrase(String.Empty, normal);
            table4_8.AddCell(currentCell);
            currentCell.Phrase = new Phrase(pageNumber.ToString(), normal);
            table4_8.AddCell(currentCell);
            string gr8Text = project.GetOsnNadpisItem("8").perechenValue;
            currentCell.Phrase = new Phrase(gr8Text, normal);
            table4_8.AddCell(currentCell);
            table4_8.WriteSelectedRows(0, 2, 155 * mm_A4, 30 * mm_A4, cb);

            // Заполнение графы 4:
            PdfPTable table4 = new PdfPTable(3);
            table4.TotalWidth = 15 * mm_A4;
            table4.LockedWidth = true;
            tbldWidths = new float[3];
            tbldWidths[0] = 5;
            tbldWidths[1] = 5;
            tbldWidths[2] = 5;
            table4.SetWidths(tbldWidths);
            string gr4aText = project.GetOsnNadpisItem("4a").perechenValue;
            currentCell = new PdfPCell(new Phrase(gr4aText, normal));
            currentCell.BorderWidth = 0.5f;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            table4.AddCell(currentCell);
            string gr4bText = project.GetOsnNadpisItem("4b").perechenValue;
            currentCell.Phrase = new Phrase(gr4bText, normal);
            table4.AddCell(currentCell);
            string gr4cText = project.GetOsnNadpisItem("4c").perechenValue;
            currentCell.Phrase = new Phrase(gr4cText, normal);
            table4.AddCell(currentCell);
            table4.WriteSelectedRows(0, 1, 155 * mm_A4, 25 * mm_A4, cb);
            #endregion

            #region Рисование таблицы с графами 10-18
            //Рисование толстых линий и заполнение заголовков граф 14-18
            PdfPTable table10_18 = new PdfPTable(5);
            table10_18.TotalWidth = 65 * mm_A4;
            table10_18.LockedWidth = true;
            tbldWidths = new float[5];
            tbldWidths[0] = 7;
            tbldWidths[1] = 10;
            tbldWidths[2] = 23;
            tbldWidths[3] = 15;
            tbldWidths[4] = 10;
            table10_18.SetWidths(tbldWidths);
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 10 * mm_A4;
            for (int i = 0; i < 5; i++) table10_18.AddCell(currentCell);
            currentCell.FixedHeight = 5 * mm_A4;
            currentCell.Phrase = new Phrase("Изм.", normal);
            table10_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Лист", normal);
            table10_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("№ докум.", normal);
            table10_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Подп.", normal);
            table10_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Дата", normal);
            table10_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            currentCell.FixedHeight = 25 * mm_A4;
            currentCell.DisableBorderSide(Rectangle.RIGHT_BORDER);
            table10_18.AddCell(currentCell);
            currentCell.EnableBorderSide(Rectangle.RIGHT_BORDER);
            currentCell.DisableBorderSide(Rectangle.LEFT_BORDER);
            table10_18.AddCell(currentCell);
            currentCell.EnableBorderSide(Rectangle.LEFT_BORDER);
            for (int i = 0; i < 3; i++) table10_18.AddCell(currentCell);
            table10_18.WriteSelectedRows(0, 3, 20 * mm_A4, 45 * mm_A4, cb);
            //Рисование тонкими линиями и заполнение граф 14-18
            PdfPTable table14_18 = new PdfPTable(5);
            table14_18.TotalWidth = 65 * mm_A4;
            table14_18.LockedWidth = true;
            tbldWidths = new float[5];
            tbldWidths[0] = 7;
            tbldWidths[1] = 10;
            tbldWidths[2] = 23;
            tbldWidths[3] = 15;
            tbldWidths[4] = 10;
            table14_18.SetWidths(tbldWidths);
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 0.5f;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            table14_18.AddCell(currentCell);
            table14_18.AddCell(currentCell);
            table14_18.AddCell(currentCell);
            table14_18.AddCell(currentCell);
            table14_18.AddCell(currentCell);
            table14_18.WriteSelectedRows(0, 1, 20 * mm_A4, 45 * mm_A4, cb);
            string gr14aText = project.GetOsnNadpisItem("14a").perechenValue;
            currentCell.Phrase = new Phrase(gr14aText, normal);
            table14_18.AddCell(currentCell);
            string gr15aText = project.GetOsnNadpisItem("15a").perechenValue;
            currentCell.Phrase = new Phrase(gr15aText, normal);
            table14_18.AddCell(currentCell);
            string gr16aText = project.GetOsnNadpisItem("16a").perechenValue;
            currentCell.Phrase = new Phrase(gr16aText, normal);
            table14_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            table14_18.AddCell(currentCell);
            table14_18.AddCell(currentCell);
            table14_18.WriteSelectedRows(1, 2, 20 * mm_A4, 40 * mm_A4, cb);

            //Рисование тонкими линиями и заполнение граф 10-13
            PdfPTable table10_13 = new PdfPTable(4);
            table10_13.TotalWidth = 65 * mm_A4;
            table10_13.LockedWidth = true;
            tbldWidths = new float[4];
            tbldWidths[0] = 17;
            tbldWidths[1] = 23;
            tbldWidths[2] = 15;
            tbldWidths[3] = 10;
            table10_13.SetWidths(tbldWidths);
            currentCell = new PdfPCell(new Phrase("Разраб.", normal));
            currentCell.BorderWidth = 0.5f;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.PaddingLeft = 3;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_LEFT;
            currentCell.FixedHeight = 5 * mm_A4;
            table10_13.AddCell(currentCell);
            string gr11aText = project.GetOsnNadpisItem("11a").perechenValue;
            currentCell.Phrase = new Phrase(gr11aText, normal);
            table10_13.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            for (int i = 0; i < 2; i++) table10_13.AddCell(currentCell);
            table10_13.WriteSelectedRows(0, 1, 20 * mm_A4, 30 * mm_A4, cb);
            currentCell.Phrase = new Phrase("Пров.", normal);
            table10_13.AddCell(currentCell);
            string gr11bText = project.GetOsnNadpisItem("11b").perechenValue;
            currentCell.Phrase = new Phrase(gr11bText, normal);
            table10_13.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            for (int i = 0; i < 2; i++) table10_13.AddCell(currentCell);
            table10_13.WriteSelectedRows(1, 2, 20 * mm_A4, 25 * mm_A4, cb);
            string gr10Text = project.GetOsnNadpisItem("10").perechenValue;
            currentCell.Phrase = new Phrase(gr10Text, normal);
            table10_13.AddCell(currentCell);
            string gr11cText = project.GetOsnNadpisItem("11c").perechenValue;
            currentCell.Phrase = new Phrase(gr11cText, normal);
            table10_13.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            for (int i = 0; i < 2; i++) table10_13.AddCell(currentCell);
            table10_13.WriteSelectedRows(2, 3, 20 * mm_A4, 20 * mm_A4, cb);
            currentCell.Phrase = new Phrase("Н. контр.", normal);
            table10_13.AddCell(currentCell);
            string gr11dText = project.GetOsnNadpisItem("11d").perechenValue;
            currentCell.Phrase = new Phrase(gr11dText, normal);
            table10_13.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            for (int i = 0; i < 2; i++) table10_13.AddCell(currentCell);
            table10_13.WriteSelectedRows(3, 4, 20 * mm_A4, 15 * mm_A4, cb);
            currentCell.Phrase = new Phrase("Утв.", normal);
            table10_13.AddCell(currentCell);
            string gr11eText = project.GetOsnNadpisItem("11e").perechenValue;
            currentCell.Phrase = new Phrase(gr11eText, normal);
            table10_13.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            for (int i = 0; i < 2; i++) table10_13.AddCell(currentCell);
            table10_13.WriteSelectedRows(4, 5, 20 * mm_A4, 10 * mm_A4, cb);


            #endregion

            #region Рисование табицы с графами 27-29            
            PdfPTable table27_29 = new PdfPTable(3);
            table27_29.TotalWidth = 120 * mm_A4;
            table27_29.LockedWidth = true;
            tbldWidths = new float[3];
            tbldWidths[0] = 14;
            tbldWidths[1] = 53;
            tbldWidths[2] = 53;
            table27_29.SetWidths(tbldWidths);

            // Заполнение граф 27-29:
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 14 * mm_A4;
            table27_29.AddCell(currentCell);
            table27_29.AddCell(currentCell);
            table27_29.AddCell(currentCell);
            table27_29.WriteSelectedRows(0, 1, 85 * mm_A4, 67 * mm_A4, cb);
            #endregion

            #region Рисование табицы с графой 30            
            PdfPTable table30 = new PdfPTable(1);
            table30.TotalWidth = 120 * mm_A4;
            table30.LockedWidth = true;

            // Заполнение графы 30:
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 8 * mm_A4;
            table30.AddCell(currentCell);
            table30.WriteSelectedRows(0, 1, 85 * mm_A4, 53 * mm_A4, cb);
            #endregion
        }

        private void DrawSubsequentStampA4(iTextSharp.text.Document doc, PdfWriter wr, int pageNumber)
        {
            PdfContentByte cb = wr.DirectContent;

            float[] tbldWidths;

            float mm_A4 = doc.PageSize.Width / 210;

            #region Рисование графы 2           
            PdfPTable table2 = new PdfPTable(1);
            table2.TotalWidth = 110 * mm_A4;
            table2.LockedWidth = true;


            // Заполнение графы 2:
            string gr2Text = project.GetOsnNadpisItem("2").perechenValue;
            PdfPCell currentCell = new PdfPCell(new Phrase(gr2Text, veryBig));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.PaddingBottom = 6;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 15 * mm_A4;
            table2.AddCell(currentCell);
            table2.WriteSelectedRows(0, 1, 85 * mm_A4, 20 * mm_A4, cb);
            #endregion

            #region Рисование графы 7           
            PdfPTable table7 = new PdfPTable(1);
            table7.TotalWidth = 10 * mm_A4;
            table7.LockedWidth = true;


            // Заполнение графы 7:
            currentCell = new PdfPCell(new Phrase("Лист", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.PaddingBottom = 6;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 7 * mm_A4;
            table7.AddCell(currentCell);
            currentCell.Phrase = new Phrase(pageNumber.ToString(), normal);
            currentCell.FixedHeight = 8 * mm_A4;
            table7.AddCell(currentCell);
            table7.WriteSelectedRows(0, 2, 195 * mm_A4, 20 * mm_A4, cb);
            #endregion

            #region Рисование таблицы с графами 14-18
            //Рисование толстых линий и заполнение заголовков граф 14-18
            PdfPTable table14_18 = new PdfPTable(5);
            table14_18.TotalWidth = 65 * mm_A4;
            table14_18.LockedWidth = true;
            tbldWidths = new float[5];
            tbldWidths[0] = 7;
            tbldWidths[1] = 10;
            tbldWidths[2] = 23;
            tbldWidths[3] = 15;
            tbldWidths[4] = 10;
            table14_18.SetWidths(tbldWidths);
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 10 * mm_A4;
            for (int i = 0; i < 5; i++) table14_18.AddCell(currentCell);
            currentCell.FixedHeight = 5 * mm_A4;
            currentCell.Phrase = new Phrase("Изм.", normal);
            table14_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Лист", normal);
            table14_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("№ докум.", normal);
            table14_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Подп.", normal);
            table14_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Дата", normal);
            table14_18.AddCell(currentCell);
            table14_18.WriteSelectedRows(0, 2, 20 * mm_A4, 20 * mm_A4, cb);
            //Рисование тонких линий и заполнение граф 14-18
            table14_18 = new PdfPTable(5);
            table14_18.TotalWidth = 65 * mm_A4;
            table14_18.LockedWidth = true;
            table14_18.SetWidths(tbldWidths);
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 0.5f;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_CENTER;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            for (int i = 0; i < 5; i++) table14_18.AddCell(currentCell);
            string gr14aText = project.GetOsnNadpisItem("14a").perechenValue;
            currentCell.Phrase = new Phrase(gr14aText, normal);
            table14_18.AddCell(currentCell);
            string gr15aText = project.GetOsnNadpisItem("15a").perechenValue;
            currentCell.Phrase = new Phrase(gr15aText, normal);
            table14_18.AddCell(currentCell);
            string gr16aText = project.GetOsnNadpisItem("16a").perechenValue;
            currentCell.Phrase = new Phrase(gr16aText, normal);
            table14_18.AddCell(currentCell);
            currentCell.Phrase = new Phrase(String.Empty, normal);
            table14_18.AddCell(currentCell);
            table14_18.AddCell(currentCell);
            table14_18.WriteSelectedRows(0, 2, 20 * mm_A4, 20 * mm_A4, cb);

            #endregion
        }

        private void DrawPerechenTable(iTextSharp.text.Document doc, PdfWriter wr, int pagesCount)
        {
            float rowsHeight = 8.86f;

            PdfContentByte cb = wr.DirectContent;
            float mm_A4 = doc.PageSize.Width / 210;

            BaseFont fontGostA = BaseFont.CreateFont("GOST_A.TTF", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font normal = new Font(fontGostA, 12f, Font.ITALIC, BaseColor.BLACK);
            Font underline = new Font(fontGostA, 12f, Font.UNDERLINE | Font.ITALIC, BaseColor.BLACK);

            PdfPTable perechTable = new PdfPTable(4);
            perechTable.TotalWidth = 185 * mm_A4;
            perechTable.LockedWidth = true;
            float[] tbldWidths = new float[4];
            tbldWidths[0] = 20;
            tbldWidths[1] = 110;
            tbldWidths[2] = 10;
            tbldWidths[3] = 45;
            perechTable.SetWidths(tbldWidths);

            // Заполнение заголовков:
            PdfPCell currentCell = new PdfPCell(new Phrase("Поз. обозначение", normal));
            currentCell.BorderWidth = 0.5f;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 15 * mm_A4;
            perechTable.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Наименование", normal);
            perechTable.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Кол.", normal);
            perechTable.AddCell(currentCell);
            currentCell.Phrase = new Phrase("Примечание", normal);
            perechTable.AddCell(currentCell);
            // Заполнение граф:                        
            int startIndex = perech_first_page_rows_count * (pagesCount == 0 ? 0 : 1) + perech_subseq_page_rows_count * (pagesCount > 1 ? pagesCount - 1 : 0); //номер первой строки на странице из общего кол-ва строк
            int rowsCount = (pagesCount == 0) ? perech_first_page_rows_count : perech_subseq_page_rows_count;
            //rowsCount = Math.Min(rowsCount, numberOfValidStrings - startIndex);
            int numberOfValidStrings = project.GetPerechenLength();
            List<PerechenItem> pData = new List<PerechenItem>();            

            for (int i = 1; i <= numberOfValidStrings; i++)
            {
                pData.Add(project.GetPerechenItem(i));
            }

            for (int j = startIndex; j < startIndex + rowsCount; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (j >= numberOfValidStrings) currentCell.Phrase = new Phrase(String.Empty);
                    else
                       if (i == 0) currentCell.Phrase = new Phrase(pData[j].designator, normal);
                    else if (i == 1)
                    {
                        if (pData[j].type == "Составное устройство")
                        {
                            currentCell.Phrase = new Phrase("   ", normal);
                            currentCell.Phrase.Add(new Chunk(pData[j].name.Substring(1), underline));
                        }
                        else currentCell.Phrase = new Phrase(" " + pData[j].name, normal);

                    }
                    else if (i == 2) currentCell.Phrase = new Phrase(pData[j].quantity, normal);
                    else if (i == 3) currentCell.Phrase = new Phrase(pData[j].note, normal);


                    else currentCell.Phrase = new Phrase(String.Empty, normal);
                    currentCell.FixedHeight = rowsHeight * mm_A4;

                    //Для графы "Наименование" устанавливаем выравниванеие по левому краю:
                    if (i == 1) currentCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    else currentCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    perechTable.AddCell(currentCell);
                }
            }

            perechTable.WriteSelectedRows(0, rowsCount + 1, 20 * mm_A4, 292 * mm_A4, cb);

            //Рисование толстых линий:
            perechTable = new PdfPTable(4);
            perechTable.TotalWidth = 185 * mm_A4;
            perechTable.LockedWidth = true;
            perechTable.SetWidths(tbldWidths);
            currentCell = new PdfPCell(new Phrase(String.Empty, normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 15 * mm_A4;
            for (int j = 0; j < 4; j++) perechTable.AddCell(currentCell);
            currentCell.FixedHeight = rowsHeight * rowsCount * mm_A4;
            for (int j = 0; j < 4; j++) perechTable.AddCell(currentCell);

            perechTable.WriteSelectedRows(0, 2, 20 * mm_A4, 292 * mm_A4, cb);
        }

        private void DrawListRegistrTable(iTextSharp.text.Document doc, PdfWriter wr)
        {
            PdfContentByte cb = wr.DirectContent;
            float mm_A4 = doc.PageSize.Width / 210;

            BaseFont fontGostA = BaseFont.CreateFont("GOST_A.TTF", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font normal = new Font(fontGostA, 12f, Font.ITALIC, BaseColor.BLACK);

            #region Заполнение ячейки "Лист регистрации изменений"
            PdfPTable registrTable1 = new PdfPTable(1);
            registrTable1.TotalWidth = 185 * mm_A4;
            registrTable1.LockedWidth = true;

            // Заполнение заголовка:
            PdfPCell currentCell = new PdfPCell(new Phrase("Лист регистрации изменений", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 10 * mm_A4;
            registrTable1.AddCell(currentCell);

            registrTable1.WriteSelectedRows(0, 1, 20 * mm_A4, 292 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Изм."
            PdfPTable registrTable2 = new PdfPTable(1);
            registrTable2.TotalWidth = 10 * mm_A4;
            registrTable2.LockedWidth = true;

            // Заполнение заголовка:
            currentCell = new PdfPCell(new Phrase("Изм.", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 27 * mm_A4;
            registrTable2.AddCell(currentCell);

            registrTable2.WriteSelectedRows(0, 1, 20 * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Номера листов (страниц)"
            PdfPTable registrTable3 = new PdfPTable(1);
            registrTable3.TotalWidth = 76 * mm_A4;
            registrTable3.LockedWidth = true;

            // Заполнение заголовка:
            currentCell = new PdfPCell(new Phrase("Номера листов (страниц)", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 9 * mm_A4;
            registrTable3.AddCell(currentCell);

            registrTable3.WriteSelectedRows(0, 1, 30 * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Измененных"
            PdfPTable registrTable4 = new PdfPTable(1);
            registrTable4.TotalWidth = 19 * mm_A4;
            registrTable4.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("изменен-", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_BOTTOM;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            registrTable4.AddCell(currentCell);
            currentCell.Phrase = new Phrase("ных", normal);
            currentCell.VerticalAlignment = Element.ALIGN_TOP;
            currentCell.FixedHeight = 13 * mm_A4;
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable4.AddCell(currentCell);
            registrTable4.WriteSelectedRows(0, 2, 30 * mm_A4, 273 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Замененных"
            PdfPTable registrTable5 = new PdfPTable(1);
            registrTable5.TotalWidth = 19 * mm_A4;
            registrTable5.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("заменен-", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_BOTTOM;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            registrTable5.AddCell(currentCell);
            currentCell.Phrase = new Phrase("ных", normal);
            currentCell.VerticalAlignment = Element.ALIGN_TOP;
            currentCell.FixedHeight = 13 * mm_A4;
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable5.AddCell(currentCell);
            registrTable5.WriteSelectedRows(0, 2, 49 * mm_A4, 273 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Новых"
            PdfPTable registrTable6 = new PdfPTable(1);
            registrTable6.TotalWidth = 19 * mm_A4;
            registrTable6.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("новых", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 9 * mm_A4;
            registrTable6.AddCell(currentCell);
            currentCell.Phrase = new Phrase("", normal);
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.FixedHeight = 9 * mm_A4;
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable6.AddCell(currentCell);
            registrTable6.WriteSelectedRows(0, 2, 68 * mm_A4, 273 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Аннулированных"
            PdfPTable registrTable7 = new PdfPTable(1);
            registrTable7.TotalWidth = 19 * mm_A4;
            registrTable7.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("аннулиро-", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_BOTTOM;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 5 * mm_A4;
            registrTable7.AddCell(currentCell);
            currentCell.Phrase = new Phrase("ванных", normal);
            currentCell.VerticalAlignment = Element.ALIGN_TOP;
            currentCell.FixedHeight = 13 * mm_A4;
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable7.AddCell(currentCell);
            registrTable7.WriteSelectedRows(0, 2, 87 * mm_A4, 273 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Всего листов (страниц) в документе"
            PdfPTable registrTable8 = new PdfPTable(1);
            registrTable8.TotalWidth = 19 * mm_A4;
            registrTable8.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("Всего", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 4.3f * mm_A4;
            registrTable8.AddCell(currentCell);
            currentCell.Phrase = new Phrase("листов", normal);
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            registrTable8.AddCell(currentCell);
            currentCell.Phrase = new Phrase("(страниц)", normal);
            registrTable8.AddCell(currentCell);
            currentCell.Phrase = new Phrase("в доку-", normal);
            registrTable8.AddCell(currentCell);
            currentCell.Phrase = new Phrase("менте", normal);
            registrTable8.AddCell(currentCell);
            currentCell.Phrase = new Phrase(" ", normal);
            currentCell.FixedHeight = 5.5f * mm_A4;
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable8.AddCell(currentCell);
            registrTable8.WriteSelectedRows(0, 6, 106 * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Номер документа"
            PdfPTable registrTable9 = new PdfPTable(1);
            registrTable9.TotalWidth = 19 * mm_A4;
            registrTable9.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("Номер", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 4.3f * mm_A4;
            registrTable9.AddCell(currentCell);
            currentCell.Phrase = new Phrase("доку-", normal);
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            registrTable9.AddCell(currentCell);
            currentCell.Phrase = new Phrase("мента", normal);
            registrTable9.AddCell(currentCell);
            currentCell.Phrase = new Phrase(" ", normal);
            currentCell.FixedHeight = 14.1f * mm_A4;
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable9.AddCell(currentCell);
            registrTable9.WriteSelectedRows(0, 4, 125 * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Входящий номер сопроводительного документа"
            PdfPTable registrTable10 = new PdfPTable(1);
            registrTable10.TotalWidth = 24 * mm_A4;
            registrTable10.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("Входящий", normal));
            currentCell.BorderWidth = 1;
            currentCell.DisableBorderSide(Rectangle.BOTTOM_BORDER);
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_TOP;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 4.3f * mm_A4;
            registrTable10.AddCell(currentCell);
            currentCell.Phrase = new Phrase("номер", normal);
            currentCell.DisableBorderSide(Rectangle.TOP_BORDER);
            registrTable10.AddCell(currentCell);
            currentCell.Phrase = new Phrase("сопроводи-", normal);
            registrTable10.AddCell(currentCell);
            currentCell.Phrase = new Phrase("тельного", normal);
            registrTable10.AddCell(currentCell);
            currentCell.Phrase = new Phrase("документа и", normal);
            registrTable10.AddCell(currentCell);
            currentCell.Phrase = new Phrase("дата", normal);
            currentCell.FixedHeight = 5.5f * mm_A4;
            currentCell.EnableBorderSide(Rectangle.BOTTOM_BORDER);
            registrTable10.AddCell(currentCell);
            registrTable10.WriteSelectedRows(0, 6, 144 * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Подпись"
            PdfPTable registrTable11 = new PdfPTable(1);
            registrTable11.TotalWidth = 18.5f * mm_A4;
            registrTable11.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("Подпись", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 27 * mm_A4;
            registrTable11.AddCell(currentCell);
            registrTable11.WriteSelectedRows(0, 1, 168 * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Заполнение ячейки "Дата"
            PdfPTable registrTable12 = new PdfPTable(1);
            registrTable12.TotalWidth = 18.5f * mm_A4;
            registrTable12.LockedWidth = true;
            currentCell = new PdfPCell(new Phrase("Дата", normal));
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 27 * mm_A4;
            registrTable12.AddCell(currentCell);
            registrTable12.WriteSelectedRows(0, 1, 186.5f * mm_A4, 282 * mm_A4, cb);
            #endregion

            #region Черчение всех остальных ячеек
            //Черчение тонких линий
            PdfPTable registrTable13 = new PdfPTable(1);
            registrTable13.TotalWidth = 10 * mm_A4;
            registrTable13.LockedWidth = true;
            currentCell = new PdfPCell();
            currentCell.BorderWidth = 0.5f;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = (235f / 15f) * mm_A4;
            for (int j = 0; j < 29; j++)
                registrTable13.AddCell(currentCell);
            registrTable13.WriteSelectedRows(0, 15, 20 * mm_A4, 255 * mm_A4, cb);
            registrTable13.TotalWidth = 19 * mm_A4;
            registrTable13.WriteSelectedRows(0, 15, 30 * mm_A4, 255 * mm_A4, cb);
            registrTable13.WriteSelectedRows(0, 15, 49 * mm_A4, 255 * mm_A4, cb);
            registrTable13.WriteSelectedRows(0, 15, 68 * mm_A4, 255 * mm_A4, cb);
            registrTable13.WriteSelectedRows(0, 15, 87 * mm_A4, 255 * mm_A4, cb);
            registrTable13.WriteSelectedRows(0, 15, 106 * mm_A4, 255 * mm_A4, cb);
            registrTable13.WriteSelectedRows(0, 15, 125 * mm_A4, 255 * mm_A4, cb);
            registrTable13.TotalWidth = 24 * mm_A4;
            registrTable13.WriteSelectedRows(0, 15, 144 * mm_A4, 255 * mm_A4, cb);
            registrTable13.TotalWidth = 18.5f * mm_A4;
            registrTable13.WriteSelectedRows(0, 15, 168 * mm_A4, 255 * mm_A4, cb);
            registrTable13.WriteSelectedRows(0, 15, 186.5f * mm_A4, 255 * mm_A4, cb);
            //Черчение толстых линий
            PdfPTable registrTable14 = new PdfPTable(1);
            registrTable14.TotalWidth = 10 * mm_A4;
            registrTable14.LockedWidth = true;
            currentCell = new PdfPCell();
            currentCell.BorderWidth = 1;
            currentCell.HasFixedHeight();
            currentCell.Padding = 0;
            currentCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            currentCell.HorizontalAlignment = Element.ALIGN_CENTER;
            currentCell.FixedHeight = 235 * mm_A4;
            registrTable14.AddCell(currentCell);
            registrTable14.WriteSelectedRows(0, 1, 20 * mm_A4, 255 * mm_A4, cb);
            registrTable14.TotalWidth = 19 * mm_A4;
            registrTable14.WriteSelectedRows(0, 1, 30 * mm_A4, 255 * mm_A4, cb);
            registrTable14.WriteSelectedRows(0, 1, 49 * mm_A4, 255 * mm_A4, cb);
            registrTable14.WriteSelectedRows(0, 1, 68 * mm_A4, 255 * mm_A4, cb);
            registrTable14.WriteSelectedRows(0, 1, 87 * mm_A4, 255 * mm_A4, cb);
            registrTable14.WriteSelectedRows(0, 1, 106 * mm_A4, 255 * mm_A4, cb);
            registrTable14.WriteSelectedRows(0, 1, 125 * mm_A4, 255 * mm_A4, cb);
            registrTable14.TotalWidth = 24 * mm_A4;
            registrTable14.WriteSelectedRows(0, 1, 144 * mm_A4, 255 * mm_A4, cb);
            registrTable14.TotalWidth = 18.5f * mm_A4;
            registrTable14.WriteSelectedRows(0, 1, 168 * mm_A4, 255 * mm_A4, cb);
            registrTable14.WriteSelectedRows(0, 1, 186.5f * mm_A4, 255 * mm_A4, cb);
            #endregion



        }

    }
}
