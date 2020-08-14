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
            //BillCreator = new PdfBillCreator();
            ElementListCreator = new PdfElementListCreator();
            //SpecificationCreator = new PdfSpecificationCreator();
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
            bool res = false;
            switch (aDocType)
            {
                case DocType.Specification:
                    break;

                case DocType.ItemsList:
                    ElementListCreator.Create(aProject);
                    res = true;
                    break;

                case DocType.Bill:
                    break;

                case DocType.D27:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");                    
            }

            return res;
        }


        public byte[] GetPDFData(DocType aDocType)
        {
            switch (aDocType)
            {
                case DocType.Specification:
                    throw new NotImplementedException();
                    break;

                case DocType.ItemsList:                    
                    return ElementListCreator.GetData();

                case DocType.Bill:
                    throw new NotImplementedException();
                    break;

                case DocType.D27:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");
                default:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");
            }
            
        }
    }
}
