using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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




        public void CreateDocument(DocType aType)
        {
            switch (aType)
            {
                case DocType.Specification:
                    break;

                case DocType.ItemsList:
                    break;

                case DocType.Bill:
                    break;

                case DocType.D27:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");                    
            }
            throw new NotImplementedException();
        }


        public string GetFileName(DocType aDocType)
        {
            switch (aDocType)
            {
                case DocType.Specification:                    
                    break;

                case DocType.ItemsList:
                    break;

                case DocType.Bill:
                    break;

                case DocType.D27:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");                    
            }
            throw new NotImplementedException();
        }


        public bool SaveChanges(DocType aDocType, Project aProject)
        {
            GetCreator(aDocType).Create(aProject);
            return true;
        }


        public byte[] GetPDFData(DocType aDocType) {
            return GetCreator(aDocType).GetData();
        }

        PdfCreator GetCreator(DocType aDocType) {
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
