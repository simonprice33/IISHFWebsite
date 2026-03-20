using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.ExcelToPdf.Interfaces;
using SautinSoft;

namespace IISHF.ExcelToPdf.Services
{
    public class ExcelToPdf : IExcelToPdf
    {
        private readonly SautinSoft.ExcelToPdf _excelToPdf;

        public ExcelToPdf()
        {
            _excelToPdf = new SautinSoft.ExcelToPdf();
            SautinSoft.ExcelToPdf.SetLicense("50048238122");
            _excelToPdf.OutputFormat = SautinSoft.ExcelToPdf.eOutputFormat.Pdf;
            _excelToPdf.Sheets.Custom(new[] { 1 });
            _excelToPdf.PageStyle.PageOrientation.Portrait();
            _excelToPdf.PageStyle.PageScale.ScaleToOnePage();
            _excelToPdf.PageStyle.PageMarginTop.mm(5);
            _excelToPdf.PageStyle.PageMarginBottom.mm(5);
            _excelToPdf.PageStyle.PageMarginLeft.mm(10);
            _excelToPdf.PageStyle.PageMarginRight.mm(10);
            _excelToPdf.Options.PdfVersion = SautinSoft.ExcelToPdf.COptions.ePdfVersion.PDF_A3u;
        }

        public byte[] GeneratePdf(byte[] incomingFile)
        {
            return _excelToPdf.ConvertBytes(incomingFile);
        }
    }
}
