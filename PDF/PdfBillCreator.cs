using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;
using Document = iText.Layout.Document;

namespace GostDOC.PDF
{
    class PdfBillCreator : PdfCreator
    {
        public PdfBillCreator() : base(DocType.Bill) {
        }

        public override void Create(Project project) {
            throw new NotImplementedException();
        }

        internal override int AddFirstPage(Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData) {
            throw new NotImplementedException();
        }

        internal override int AddNextPage(Document aInPdfDoc, IDictionary<string, string> aGraphs, DataTable aData, int aLastProcessedRow) {
            throw new NotImplementedException();
        }
    }
}
