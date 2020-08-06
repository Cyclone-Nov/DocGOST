using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GostDOC.Common;

namespace GostDOC.PDF
{
    public class PdfManager
    {
        #region Singleton
        private static readonly Lazy<PdfManager> _instance = new Lazy<PdfManager>(() => new PdfManager(), true);
        public static PdfManager Instance => _instance.Value;

        private PdfBillCreator BillCreator;

        private PdfElementListCreator ElementListCreator;

        private PdfSpecificationCreator SpecificationCreator;

        PdfManager()
        {
        }
        #endregion

        public void CreateDocument(DocType aType)
        {
            switch(aType)
            {
                case DocType.Specification:
                    break;

                case DocType.ItemsList:
                    break;

                case DocType.Bill:
                    break;

                case DocType.D27:
                    throw new NotSupportedException("Экспорт в pdf документа Д27 не поддерживается");
                    break;
            }
        }


        public string GetFileName(DocType aDocType)
        {
            throw new NotImplementedException();
        }




    }
}
