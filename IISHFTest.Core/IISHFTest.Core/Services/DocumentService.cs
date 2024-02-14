using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using Serilog.Formatting;

namespace IISHFTest.Core.Services
{
    public class DocumentService : IDocumentService
    {
       public async Task<MemoryStream> GeneratePdfToMemoryStreamAsync(SubmittedTeamInformationModel teamInformation,  byte[] iishfLogo)
        {
            using MemoryStream memoryStream = new MemoryStream();
            // Create a new PDF document.
            using var pdfDocument = new PdfDocument();
            // Add a new page.
            var page = pdfDocument.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            await AddHeaderImages(teamInformation.TeamLogo, iishfLogo, gfx);

            // Draw the "Team Roster" header.
            var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
            var headerBrush = XBrushes.Black;
            var headerX = 200; // Adjust the horizontal position of the "Team Roster" header.
            var headerY = 150; // Adjust the vertical position of the header.
            gfx.DrawString($"{teamInformation.TeamName} Team Roster", headerFont, headerBrush, new XRect(headerX, headerY, page.Width - 400, 150), XStringFormats.TopCenter);
            AddTableAndData(teamInformation, page, gfx, headerFont, headerBrush);

            page = pdfDocument.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            AddWriteup(teamInformation, gfx, headerFont, headerBrush, headerX, page);

            pdfDocument.Save(memoryStream);
            return memoryStream;
        }

        private static void AddWriteup(SubmittedTeamInformationModel teamInformation, XGraphics gfx, XFont headerFont, XSolidBrush headerBrush, int headerX, PdfPage page)
        {
            // Draw the "Team Writeup" header.
            var writeupHeaderY = 50; // Adjust the vertical position of the "Team Writeup" header.
            gfx.DrawString("Team Writeup", headerFont, headerBrush, new XRect(headerX, writeupHeaderY, page.Width - 400, 50),
                XStringFormats.TopCenter);

            // Draw the Lorem Ipsum text using XTextFormatter to handle text wrapping.
            var writeupTextY = writeupHeaderY + 30; // Adjust the vertical position of the text.
            var font = new XFont("Arial", 10, XFontStyle.Regular);

            var textFormatter = new XTextFormatter(gfx);
            var writeupRect = new XRect(50, writeupTextY, page.Width - 100, page.Height - writeupTextY - 50);
            textFormatter.DrawString(teamInformation.TeamWriteUp, font, XBrushes.Black, writeupRect);
        }

        private async Task AddHeaderImages(byte[] teamLogo, byte[] iishfLogo, XGraphics gfx)
        {
            // Add the header image to the top right of the page with a size of 100x300.
            var headerImageHeight = 300; // Adjust the header image height.
            await AddHeaderImage(gfx, iishfLogo, 100, headerImageHeight);

            // Add the second image to the top left of the page with a size of 100x100.
            double logoImageWidth = 100;
            double logoImageHeight = 100;
            await AddLogoImage(gfx, teamLogo, 50, 50, logoImageWidth, logoImageHeight);
        }
        private void AddTableAndData(SubmittedTeamInformationModel teamInformation, PdfPage page, XGraphics gfx, XFont headerFont,
            XSolidBrush headerBrush)
        {
            // Calculate the width and horizontal position of the table to make it span the entire width of the page.
            var tableWidth = page.Width - 100;
            var tableLeftX = 50; // Adjust the horizontal position of the table.

            // Calculate the vertical position to place the table.
            var tableTopY = 180; // Adjust the vertical position of the table.

            // Create a bordered table to display the player data.
            var cellWidth = tableWidth / 4; // Divide the table width equally among 4 columns.
            var cellHeight = 14;
            var dataFont = new XFont("Arial", 10, XFontStyle.Regular);

            // Draw the table headers.
            DrawTableCell(gfx, "First Name", headerFont, headerBrush, tableLeftX, tableTopY, cellWidth, cellHeight);
            DrawTableCell(gfx, "Last Name", headerFont, headerBrush, tableLeftX + cellWidth, tableTopY, cellWidth, cellHeight);
            DrawTableCell(gfx, "Jersey #", headerFont, headerBrush, tableLeftX + 2 * cellWidth, tableTopY, cellWidth, cellHeight);
            DrawTableCell(gfx, "Position", headerFont, headerBrush, tableLeftX + 3 * cellWidth, tableTopY, cellWidth, cellHeight);

            tableTopY += cellHeight;

            // Draw the player data in the table format.
            foreach (var player in teamInformation.Roster)
            {
                DrawTableCell(gfx, player.FirstName, dataFont, XBrushes.Black, tableLeftX, tableTopY, cellWidth, cellHeight);
                DrawTableCell(gfx, player.LastName ?? string.Empty, dataFont, XBrushes.Black, tableLeftX + 1 * cellWidth, tableTopY, cellWidth, cellHeight);
                DrawTableCell(gfx, player.JerseyNumber.ToString() ?? string.Empty, dataFont, XBrushes.Black, tableLeftX + 2 * cellWidth, tableTopY, cellWidth, cellHeight);
                DrawTableCell(gfx, string.IsNullOrWhiteSpace(player.Role) ? string.Empty : player.Role, dataFont, XBrushes.Black, tableLeftX + 3 * cellWidth, tableTopY, cellWidth, cellHeight);

                tableTopY += cellHeight;
            }
        }

        private async Task<XImage> LoadImageFromUriAsync(byte[] imageData)
        {
            // Convert the image data to a memory stream.
            using MemoryStream imageStream = new MemoryStream(imageData);
            // Load the image from the memory stream.
            return XImage.FromStream(() => imageStream);
        }

        private async Task AddHeaderImage(XGraphics gfx, byte[] image, double maxWidth, double maxHeight)
        {
            // Load the original image from file.
            var originalImage = await LoadImageFromUriAsync(image);

            // Calculate the new width and height while keeping the aspect ratio.
            var aspectRatio = originalImage.PointWidth / originalImage.PointHeight;

            var newWidth = maxWidth;
            var newHeight = newWidth / aspectRatio;

            if (newHeight > maxHeight)
            {
                // If the new height exceeds the maxHeight, resize the image again.
                newHeight = maxHeight;
                newWidth = newHeight * aspectRatio;
            }

            // Calculate the image position to place it at the top right corner.
            var imageX = gfx.PageSize.Width - newWidth - 50; // Adjust the horizontal position (50 is the offset from the right edge).
            var imageY = 50; // Adjust the vertical position (50 is the offset from the top edge).

            // Draw the resized image on the page.
            gfx.DrawImage(originalImage, imageX, imageY, newWidth, newHeight);
        }

        private async Task AddLogoImage(XGraphics gfx, byte[] image, double x, double y, double maxWidth, double maxHeight)
        {
            // Load the original image from file.
            var originalImage = await LoadImageFromUriAsync(image);

            // Calculate the new width and height while keeping the aspect ratio.
            var aspectRatio = originalImage.PointWidth / originalImage.PointHeight;

            var newWidth = maxWidth;
            var newHeight = newWidth / aspectRatio;

            if (newHeight > maxHeight)
            {
                // If the new height exceeds the maxHeight, resize the image again.
                newHeight = maxHeight;
                newWidth = newHeight * aspectRatio;
            }

            // Draw the resized image on the page.
            gfx.DrawImage(originalImage, x, y, newWidth, newHeight);
        }

        private void DrawTableCell(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y, double width, double height)
        {
            // Draw the cell border.
            gfx.DrawRectangle(XPens.Black, x, y, width, height);

            // Draw the text inside the cell, centered both horizontally and vertically.
            var format = new XStringFormat();
            format.Alignment = XStringAlignment.Center;
            format.LineAlignment = XLineAlignment.Center;

            gfx.DrawString(text, font, brush, new XRect(x, y, width, height), format);
        }
    }
}
