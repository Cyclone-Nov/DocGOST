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
            
            int notFirstPageRows = aTotalRows - RowsOnFirstPage;
            if (notFirstPageRows > 0)
            {
                return notFirstPageRows % RowsOnNextPage == 0 ;
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

            int countPages = 1;
            int notFirstPageRows = aCurrentRowNumber - RowsOnFirstPage;
            if (notFirstPageRows > 0)
            {
                countPages += (int)(notFirstPageRows / RowsOnNextPage) + (notFirstPageRows % RowsOnNextPage > 0 ? 1 : 0);
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

            int lastRows = aCurrentRowNumber - RowsOnFirstPage;            
            if (lastRows > 0)
            {
                int delta = lastRows % RowsOnNextPage;
                if (delta == 0)
                    lastRows = 1;
                else
                    lastRows = RowsOnNextPage - delta  + 1;
            }
            else if (lastRows < 0)
            {
                lastRows = RowsOnFirstPage - aCurrentRowNumber + 1;
            }
            return lastRows;
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

    }
}
