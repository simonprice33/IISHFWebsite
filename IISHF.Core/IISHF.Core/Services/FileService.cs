using System.Data;
using ClosedXML.Excel;
using IISHF.Core.Models.ServiceBusMessage;
using IISHF.ExcelToPdf.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using IFileService = IISHF.Core.Interfaces.IFileService;

namespace IISHF.Core.Services;

public class FileService : IFileService
{
    private readonly IPublishedContentQuery _contentQuery;
    private readonly IContentService _contentService;
    private readonly IMediaService _umbracoMediaService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IExcelToPdf _excelToPdf;

    public FileService(
        IPublishedContentQuery contentQuery,
        IContentService contentService,
            IMediaService umbracoMediaService,
        IWebHostEnvironment webHostEnvironment,
        IExcelToPdf excelToPdf
        )
    {
        _contentQuery = contentQuery;
        _contentService = contentService;
        _umbracoMediaService = umbracoMediaService;
        _webHostEnvironment = webHostEnvironment;
        _excelToPdf = excelToPdf;
    }

    public async Task<byte[]> GenerateItcAsExcelFile(IPublishedContent team, IPublishedContent tournament)
    {
        // Generate the Excel file in memory
        var memoryStream = await GenerateExcelFile(team, tournament);

        // Convert the memory stream to byte array
        byte[] bytes = memoryStream.ToArray();

        ////// Send the Excel file as an attachment via SendGrid
        ////await SendEmailWithAttachment(bytes, "itc.xlsx");
        ///

        return bytes;
    }

    public byte[] GenerateItcAsPdfFile(byte[] itcAsByteArray)
    {
        return _excelToPdf.GeneratePdf(itcAsByteArray);
    }

    private async Task<MemoryStream> GenerateExcelFile(IPublishedContent team, IPublishedContent tournament)
    {
        // First, find the specific folder
        var existingFolder = _umbracoMediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Documents" && x.ContentType.Alias == Umbraco.Cms.Core.Constants.Conventions.MediaTypes.Folder);

        MemoryStream itcFile = null!;

        if (existingFolder != null)
        {
            // Query the children of the found folder for a media item with the specific name
            var mediaItems = _umbracoMediaService.GetPagedChildren(existingFolder.Id, 0, int.MaxValue, out long totalRecords).FirstOrDefault(x => x.Name == "ITC");
            itcFile = await GetFileAsStreamAsync(mediaItems.Id);
        }

        using var workbook = new XLWorkbook(itcFile);
        var worksheet = workbook.Worksheet("ITC");

        var mergeRanges = new List<EventInformation> {
            new EventInformation { Range = "F2:K2", Value = "123" }, // ITC Reference Number
            new EventInformation { Range = "F3:K3", Value = "A2024-00" }, // Sanction Number
            new EventInformation { Range = "F4:K4", Value = "U13" }, // Age Group
            new EventInformation { Range = "F5:K5", Value = "U13 European Cup" }, // Event Name
            new EventInformation { Range = "F10:K10", Value = "Some signatory" }, // Team Signatory
            new EventInformation { Range = "F11:K11", Value = "Germany" }, // Hosting Country
            new EventInformation { Range = "F12:K12", Value = "#ffffff" }, // Jersey Colour one
            new EventInformation { Range = "F13:K13", Value = "#987acb" }, // Jersey Colour Two
            new EventInformation { Range = "C15:E15", Value = "Some Made up team" } // Team Name
        };

        foreach (var range in mergeRanges)
        {
            // Merge each range
            worksheet.Range(range.Range).Merge();

            // Write to the merged cell (the top-left cell of the merged range)
            // For example, if the range is "F2:K2", we write to "F2"
            worksheet.Cell(range.Range.Split(':')[0]).Value = range.Value; // Replace "Your Value Here" with your actual value
        }


        // Assume we have a DataTable
        DataTable dataTable = CreateDataTable(team, tournament);

        // Define starting cell row and column (B19)
        int startRow = 19;
        int startColumn = 2; // B column

        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            for (int column = 0; column < dataTable.Columns.Count; column++)
            {
                var cell = worksheet.Cell(startRow + row, startColumn + column);
                var value = dataTable.Rows[row][column];

                if (column == 5)
                {
                    if (value is DateTime dateTimeValue)
                    {
                        cell.Value = dateTimeValue;
                        cell.Style.DateFormat.Format = "dd.mm.yyyy";
                    }
                }
                if (column != 6)
                {

                    if (value is DateTime)
                    {
                        cell.SetValue((DateTime)value);
                    }
                    else if (value is int)
                    {
                        cell.SetValue((int)value);
                    }
                    else if (value != null)
                    {
                        cell.SetValue(value.ToString());
                    }
                    else
                    {
                        cell.Clear();
                    }
                }
            }

            if (startRow + row <= 48)
            {
                string dateOfBirthCellAddress = worksheet.Cell(startRow + row, startColumn + 5).Address.ToString();
                worksheet.Cell(startRow + row, startColumn + 6).FormulaA1 = $"=IF(LEN({dateOfBirthCellAddress}) = 0, 0, YEAR({dateOfBirthCellAddress}))";
            }
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task<MemoryStream> GetFileAsStreamAsync(int mediaItemId)
    {
        var mediaItem = _umbracoMediaService.GetById(mediaItemId);
        if (mediaItem != null)
        {
            var umbracoFilePath = mediaItem.GetValue<string>(Umbraco.Cms.Core.Constants.Conventions.Media.File);

            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, umbracoFilePath.TrimStart('/'));

            if (File.Exists(physicalPath))
            {
                var memoryStream = new MemoryStream();
                await using (var fileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        return null;
    }

    DataTable CreateDataTable(IPublishedContent team, IPublishedContent tournament)
    {
        // Create and populate your DataTable
        DataTable table = new DataTable();
        table.Columns.Add("LICENCE NUMBER");
        table.Columns.Add("POSITION");
        table.Columns.Add("LAST NAME");
        table.Columns.Add("First Name");
        table.Columns.Add("Shirt No.", typeof(int));
        table.Columns.Add("Date of Birth", typeof(DateTime));
        table.Columns.Add("Year of birth", typeof(int));
        table.Columns.Add("Gender");
        table.Columns.Add("Nationality");
        table.Columns.Add("NMA Check");
        table.Columns.Add("Comments");

        // Add rows to the table as needed

        var playersOnRoster = team.Children().Where(x => x.ContentType.Alias == "roster" && !x.Value<bool>("isBenchOfficial")).ToList();
        var sortedRoster = SortRoster(playersOnRoster);
        for (var i = 0; i < sortedRoster.Count; i++)
        {
            var rosterMember = sortedRoster[i];

            var nmaApproved = rosterMember.Value<bool>("nmaCheck");

            table.Rows.Add(
                rosterMember.Value<string>("licenseNumber"),
                rosterMember.Value<string>("role"),
                rosterMember.Value<string>("lastName").ToUpper(),
                rosterMember.Value<string>("firstName"),
                rosterMember.Value<int>("jerseyNumber"),
                rosterMember.Value<DateTime>("dateOfBirth").ToString("dd.MM.yyyy"),
                rosterMember.Value<DateTime>("dateOfBirth").Year,
                rosterMember.Value<string>("gender"),
                rosterMember.Value<string>("iso3"),
                nmaApproved ? "OK" : "Rejected",
                rosterMember.Value<string>("comments")
                );
        }

        // Return the populated DataTable
        return table;
    }

    public List<IPublishedContent> SortRoster(List<IPublishedContent> roster)
    {
        var roleOrder = new Dictionary<string, int>
        {
            {"Captain", 11},
            {"Assistant Captain", 12},
            {"Netminder", 13},

            {"Head Coach", 24},
            {"Assistant Coach", 25},
            {"Training Staff", 26},
            {"Equipment Manager", 27},
            {"Physio", 28},
            {"Photographer", 29},
        };

        var sortedRoster = roster
            .OrderBy(player => player.Value<bool>("isBenchOfficial") ? 1 : 0) // First, separate bench officials from players
            .ThenBy(player => roleOrder.ContainsKey(player.Value<string>("role")) ? roleOrder[player.Value<string>("role")] : int.MaxValue)
            .ThenBy(player => player.Value<int>("jerseyNumber"))
            .ThenBy(player => player.Value<bool>("isBenchOfficial") && roleOrder.ContainsKey(player.Value<string>("role")) ? roleOrder[player.Value<string>("role")] : int.MaxValue) // Additional sorting for bench officials by role
            .ToList();

        return sortedRoster;
    }


    public class EventInformation
    {

        public string Range { get; set; }
        public string Value { get; set; }

    }
}