using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using IISHF.ExcelToPdf.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace IISHF.ExcelToPdf.Services
{
    /// <summary>
    /// Converts Excel files to PDF by delegating to LibreOffice running in headless mode.
    ///
    /// LibreOffice faithfully reproduces all Excel page-setup settings (print area, scale,
    /// margins, orientation) and produces output identical to "Save as PDF" from within
    /// a spreadsheet application.
    ///
    /// Installation:
    ///   Windows (dev):  https://www.libreoffice.org/download/libreoffice/
    ///   Linux (server): sudo apt-get install -y libreoffice
    ///
    /// The executable path is auto-detected for Windows and Linux.  Override via the
    /// LIBREOFFICE_PATH environment variable if needed.
    /// </summary>
    public class ExcelToPdfLibreOffice : IExcelToPdf
    {
        public byte[] GeneratePdf(byte[] incomingFile)
        {
            var sofficePath = ResolveSofficePath();

            // LibreOffice writes its output as <inputFileNameWithoutExt>.pdf in the
            // directory specified by --outdir.  We control the input filename so we
            // know exactly what the output file will be called.
            var tempDir    = Path.Combine(Path.GetTempPath(), $"lo_{Guid.NewGuid():N}");
            var tempInput  = Path.Combine(tempDir, "itc.xlsx");
            var tempOutput = Path.Combine(tempDir, "itc.pdf");

            Directory.CreateDirectory(tempDir);

            try
            {
                File.WriteAllBytes(tempInput, incomingFile);

                var psi = new ProcessStartInfo
                {
                    FileName               = sofficePath,
                    Arguments              = $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{tempInput}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                };

                // LibreOffice uses a per-user profile directory.  When running as a service
                // account that profile may not exist; point it at our temp dir to avoid
                // conflicts when multiple requests run concurrently.
                psi.EnvironmentVariables["HOME"]             = tempDir;   // Linux
                psi.EnvironmentVariables["USERPROFILE"]      = tempDir;   // Windows

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException($"Failed to start LibreOffice process at '{sofficePath}'.");

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();

                process.WaitForExit(60_000);

                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        $"LibreOffice exited with code {process.ExitCode}.\nstdout: {stdout}\nstderr: {stderr}");

                if (!File.Exists(tempOutput))
                    throw new InvalidOperationException(
                        $"LibreOffice did not produce the expected output file '{tempOutput}'.\nstdout: {stdout}\nstderr: {stderr}");

                return ExtractFirstPage(File.ReadAllBytes(tempOutput));
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
            }
        }

        private static byte[] ExtractFirstPage(byte[] pdfBytes)
        {
            using var inputStream = new MemoryStream(pdfBytes);
            using var source = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

            if (source.PageCount <= 1)
                return pdfBytes;

            using var output = new PdfDocument();
            output.AddPage(source.Pages[0]);

            using var outputStream = new MemoryStream();
            output.Save(outputStream);
            return outputStream.ToArray();
        }

        private static string ResolveSofficePath()
        {
            // Allow explicit override via environment variable
            var fromEnv = Environment.GetEnvironmentVariable("LIBREOFFICE_PATH");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return fromEnv;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Standard install locations for LibreOffice on Windows
                var candidates = new[]
                {
                    @"C:\Program Files\LibreOffice\program\soffice.exe",
                    @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                };

                foreach (var path in candidates)
                    if (File.Exists(path))
                        return path;

                throw new InvalidOperationException(
                    "LibreOffice not found. Install it from https://www.libreoffice.org/download/libreoffice/ " +
                    "or set the LIBREOFFICE_PATH environment variable.");
            }

            // Linux / macOS — soffice should be on PATH
            return "soffice";
        }
    }
}
