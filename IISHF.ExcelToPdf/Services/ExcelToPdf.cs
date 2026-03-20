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
            // No page style overrides — let SautinSoft read orientation, scale, margins
            // and print area directly from the Excel file's own page setup (portrait, 73%,
            // 10mm left/right, 5mm top/bottom, print area A1:L70).
            _excelToPdf.Options.PdfVersion = SautinSoft.ExcelToPdf.COptions.ePdfVersion.PDF_A3u;
            // Ensure all 12 columns (A–L) are rendered. The default appears to cap at a
            // lower value which causes H:K (the signoff name column) to be silently clipped.
            _excelToPdf.ColumnsToConvertLimit = 100;
        }

        public byte[] GeneratePdf(byte[] incomingFile)
        {
            // ConvertBytes wraps the array in an internal sequential stream. For ZIP-based
            // XLSX files the central directory is at the end of the file, and the print area
            // named range lives inside workbook.xml — SautinSoft may not resolve these
            // correctly without full random-access file I/O.
            // Writing to a temp file and using ConvertFile gives the library proper
            // disk-backed random access, matching how it would read a real file.
            var tempInput  = Path.Combine(Path.GetTempPath(), $"itc_in_{Guid.NewGuid()}.xlsx");
            var tempOutput = Path.Combine(Path.GetTempPath(), $"itc_out_{Guid.NewGuid()}.pdf");
            try
            {
                File.WriteAllBytes(tempInput, incomingFile);
                _excelToPdf.ConvertFile(tempInput, tempOutput);
                return File.ReadAllBytes(tempOutput);
            }
            finally
            {
                if (File.Exists(tempInput))  File.Delete(tempInput);
                if (File.Exists(tempOutput)) File.Delete(tempOutput);
            }
        }
    }
}
