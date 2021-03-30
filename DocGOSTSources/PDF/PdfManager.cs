using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.PDF
{
    public class PdfManager
    {
        #region Singleton
        private static readonly Lazy<PdfManager> _instance = new Lazy<PdfManager>(() => new PdfManager(), true);
        public static PdfManager Instance => _instance.Value;

        private PdfCreator BillCreator;

        private PdfCreator ElementListCreator;

        private PdfCreator SpecificationCreator;

        PdfManager()
        {
            BillCreator = new PdfBillCreator();
            ElementListCreator = new PdfElementListCreator();
            SpecificationCreator = new PdfSpecificationCreator();
        }
        #endregion

        /// <summary>
        /// подготовить PDF документ
        /// </summary>
        /// <param name="aDocType">тип документа</param>
        /// <param name="aData">подготовленная к выводу таблица данных</param>
        /// <param name="aMainGraphs"></param>
        /// <returns></returns>
        public bool PreparePDF(DocType aDocType, DataTable aData, IDictionary<string,string> aMainGraphs, Dictionary<string, object> aAppParams)
        {
            GetCreator(aDocType).Create(aData, aMainGraphs, aAppParams);
            return true;
        }


        /// <summary>
        /// Gets the PDF data.
        /// </summary>
        /// <param name="aDocType">Type of a document.</param>
        /// <returns></returns>
        public byte[] GetPDFData(DocType aDocType) {
            return GetCreator(aDocType).GetData();
        }
                

        private PdfCreator GetCreator(DocType aDocType) {
            PdfCreator creator;
            switch (aDocType)
            {
                case DocType.Specification:
                    creator = SpecificationCreator;
                    break;

                case DocType.ItemsList:
                    creator = ElementListCreator;
                    break;

                case DocType.Bill:
                    creator = BillCreator;
                    break;

                case DocType.D27:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");
                default:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");
                
            }

            return creator;
        }
    }
}
