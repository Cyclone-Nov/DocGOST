using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Common
{
    /// <summary>
    /// общие вспомогательные методы
    /// </summary>
    public static class CommonUtils
    {
        /// <summary>
        /// получить количество страниц в документе в зависимости от типа документа
        /// </summary>
        /// <param name="aType">тип документа.</param>
        /// <param name="">количество строк в документе</param>
        /// <returns>количество страниц</returns>
        public static int GetCountPage(DocType aType, int aTotalRows)
        {
            int countPages = GetCurrentPage(aType, aTotalRows);
            if (countPages > Constants.MAX_PAGES_WITHOUT_CHANGELIST)
            {
                countPages++;
            }
            return countPages;
        }

        /// <summary>
        /// признак что данное количество строк занимает листы целиком
        /// </summary>
        /// <param name="aType">a type.</param>
        /// <param name="aTotalRows">a total rows.</param>
        /// <returns></returns>
        public static bool FullPage(DocType aType, int aTotalRows)
        {
            var rows = GetRowsCountsForDocument(aType); // Item1 = rows on first page, Item2 = rows on next page

            int notFirstPageRows = aTotalRows - rows.Item1;
            if (notFirstPageRows > 0)
            {
                return notFirstPageRows % rows.Item2 == 0 ;
            }

            return true;
        }
        

        /// <summary>
        /// получить номер страница для данного количества строк таблицы
        /// </summary>
        /// <param name="aType">тип документа.</param>
        /// <param name="">номер строки из таблицы данных</param>
        /// <returns>количество страниц</returns>
        public static int GetCurrentPage(DocType aType, int aCurrentRowNumber)
        {
            var rows = GetRowsCountsForDocument(aType); // Item1 = rows on first page, Item2 = rows on next page

            int countPages = 1;
            int notFirstPageRows = aCurrentRowNumber - rows.Item1;
            if (notFirstPageRows > 0)
            {
                countPages += (int)(notFirstPageRows / rows.Item2) + (notFirstPageRows % rows.Item2 > 0 ? 1 : 0);
            }            
            return countPages;
        }


        /// <summary>
        /// получить количество строк таблицы данных до конца данной страницы начиная со строки с номером aCurrentRowNumber включительно
        /// </summary>
        /// <param name="aType">тип документа.</param>
        /// <param name="aCurrentRowNumber">номер строки из таблицы данных</param>
        /// <returns>количество строк до конца текущей страницы</returns>
        public static int GetRowsToEndOfPage(DocType aType, int aCurrentRowNumber)
        {
            var rows = GetRowsCountsForDocument(aType); // Item1 = rows on first page, Item2 = rows on next page

            int lastRows = aCurrentRowNumber - rows.Item1;            
            if (lastRows > 0)
            {
                int delta = lastRows % rows.Item2;
                if (delta == 0)
                    lastRows = 1;
                else
                    lastRows = rows.Item2 - delta  + 1;
            }
            else if (lastRows < 0)
            {
                lastRows = rows.Item1 - aCurrentRowNumber + 1;
            }
            return lastRows;
        }


        /// <summary>
        /// получить количество строк таблицы данных на всех листах включая текущий aCurrentPageNumber
        /// </summary>
        /// <param name="aType">тип документа.</param>
        /// <param name="aCurrentPageNumber">номер листа документа</param>
        /// <returns>количество строк до конца текущей страницы</returns>
        public static int GetRowsForPages(DocType aType, int aCurrentPageNumber)
        {
            if (aCurrentPageNumber < 1)
                return 0;

            var rows = GetRowsCountsForDocument(aType); // Item1 = rows on first page, Item2 = rows on next page            
            if (aCurrentPageNumber == 1)
            {
                return rows.Item1;
            }

            return rows.Item1 + rows.Item2 * (aCurrentPageNumber - 1);
        }

        /// <summary>
        /// проверка что поставщик русский (на основе наличия неанглийских букв в наименовании)
        /// </summary>
        /// <param name="aSupplier">a supplier.</param>
        /// <returns>
        ///   <c>true</c> if [is russian supplier] [the specified a supplier]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRussianSupplier(string aSupplier)
        {
            if (string.IsNullOrEmpty(aSupplier))
                return true;

            char letter;
            int i = 0;
            do
            {
                letter = aSupplier[i++];
            }
            while (Char.IsDigit(letter) && i < aSupplier.Length);

            if (letter > 127)
                return true;

            return false;
        }

        /// <summary>
        /// получить количество строк для первого листа и для последующих для данного документа aType
        /// </summary>
        /// <param name="aType">тип документа</param>
        /// <returns>Количество строк для первого листа и для последующих (Tuple<первый лист, следующие листы>) для докумнта aType</returns>
        public static Tuple<int, int> GetRowsCountsForDocument(DocType aType)
        {
            int RowsOnFirstPage;
            int RowsOnNextPage;
            switch (aType)
            {
                case DocType.Bill:
                {
                    RowsOnFirstPage = Constants.BillRowsOnFirstPage;
                    RowsOnNextPage = Constants.BillRowsOnNextPage;
                }
                break;
                case DocType.D27:
                {
                    RowsOnFirstPage = Constants.DefaultRowsOnFirstPage;
                    RowsOnNextPage = Constants.DefaultRowsOnNextPage;
                }
                break;
                case DocType.Specification:
                {
                    RowsOnFirstPage = Constants.SpecificationRowsOnFirstPage;
                    RowsOnNextPage = Constants.SpecificationRowsOnNextPage;
                }
                break;
                case DocType.ItemsList:
                {
                    RowsOnFirstPage = Constants.ItemListRowsOnFirstPage;
                    RowsOnNextPage = Constants.ItemListRowsOnNextPage;
                }
                break;
                default:
                {
                    RowsOnFirstPage = Constants.DefaultRowsOnFirstPage;
                    RowsOnNextPage = Constants.DefaultRowsOnNextPage;
                }
                break;
            }
            return new Tuple<int, int>(RowsOnFirstPage, RowsOnNextPage);
        }

        /// <summary>
        /// получить строку с перечислением авторов проекта
        /// </summary>
        /// <returns></returns>
        public static string GetAuthorsString()
        {
            return "Antipov Roman, Vitkovski Victor, Fateev Ilya, Grunichev Mikhail";
        }

        /// <summary>
        /// получить строку с перечислением авторов проекта
        /// </summary>
        /// <returns></returns>
        public static string GetCreatorString()
        {
            return $"GOSTDoc ver. {ApplicationVersion}";
        }

        /// <summary>
        /// получить строку с перечислением авторов проекта
        /// </summary>
        /// <returns></returns>
        public static string GetCreatorAndVersionString()
        {            
            return $"Made by GOSTDoc ver. {ApplicationVersion}";
        }

        /// <summary>
        /// Текущая версия ПО
        /// </summary>
        /// <value>
        /// строка с версией ПО
        /// </value>
        public static string ApplicationVersion
        {
            get => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
