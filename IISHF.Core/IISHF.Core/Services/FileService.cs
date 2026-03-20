using System.Data;
using System.Globalization;
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

        if (existingFolder == null)
            throw new InvalidOperationException("Could not locate the 'Documents' media folder. Ensure it exists at the root of the media library.");

        // Query the children of the found folder for a media item with the specific name (case-insensitive)
        var mediaItems = _umbracoMediaService.GetPagedChildren(existingFolder.Id, 0, int.MaxValue, out long totalRecords)
            .FirstOrDefault(x => x.Name.Equals("ITC", StringComparison.OrdinalIgnoreCase));

        if (mediaItems == null)
            throw new InvalidOperationException("Could not locate the 'ITC' template file inside the 'Documents' media folder.");

        var itcFile = await GetFileAsStreamAsync(mediaItems.Id);

        using var workbook = new XLWorkbook(itcFile);
        var worksheet = workbook.Worksheet("ITC");

        var eventStartDate = tournament.Value<DateTime?>("eventStartDate");
        var eventEndDate = tournament.Value<DateTime?>("eventEndDate");
        var eventDateRange = eventStartDate.HasValue && eventEndDate.HasValue
            ? $"{eventStartDate.Value.ToString("dd.MM.yyyy")} - {eventEndDate.Value.ToString("dd.MM.yyyy")}"
            : eventStartDate.HasValue
                ? eventStartDate.Value.ToString("dd.MM.yyyy")
                : string.Empty;

        var mergeRanges = new List<EventInformation> {
            new EventInformation { Range = "F2:K2", Value = $"{team.Id}-{tournament.Id}" }, // ITC Reference Number
            new EventInformation { Range = "F3:K3", Value = tournament.Value<string>("sanctionNumber") }, // Sanction Number
            new EventInformation { Range = "F4:K4", Value = tournament.Parent.Value<string>("ageGroup") }, // Age Group
            new EventInformation { Range = "F5:K5", Value = tournament.Parent.Name }, // Event Name
            new EventInformation { Range = "F6:K6", Value = eventDateRange }, // Start & End Event Date
            new EventInformation { Range = "F7:K7", Value = tournament.Value<string>("venueAddress") }, // Event Location
            new EventInformation { Range = "F8:K8", Value = tournament.Value<string>("hostClub") }, // Name of Hosting Club
            new EventInformation { Range = "F9:K9", Value = tournament.Value<string>("hostCountry") }, // Hosting Country
            new EventInformation { Range = "F10:K10", Value = team.Value<string>("teamSignatory") }, // Team Signatory
            new EventInformation { Range = "F11:K11", Value = team.Value<string>("countryIso3") }, // Issuing Country
            new EventInformation { Range = "F12:K12", Value = team.Value<string>("jerseyOneColour") }, // Jersey Colour One
            new EventInformation { Range = "F13:K13", Value = team.Value<string>("jerseyTwoColour") }, // Jersey Colour Two
            new EventInformation { Range = "C15:E15", Value = $"{team.Name}" } // Team Name
        };

        foreach (var range in mergeRanges)
        {
            // Merge each range
            worksheet.Range(range.Range).Merge();

            // Write to the merged cell (the top-left cell of the merged range)
            // For example, if the range is "F2:K2", we write to "F2"
            worksheet.Cell(range.Range.Split(':')[0]).Value = range.Value;
        }

        // F15:K15 contains a formula that classifies the event as Title or Non-Title based on
        // the sanction number in F3. ClosedXML may not retain template formulas when saving back
        // to a stream, so we explicitly re-apply the merge and formula here.
        worksheet.Range("F15:K15").Merge();
        worksheet.Cell("F15").FormulaA1 = "=IF(LEFT(F3, 1) = \"A\",\"IISHF Title-Event\", \"IISHF Non-Title-Event\")";

        // Assume we have a DataTable
        DataTable playersDataTable = CreatePlayersDataTable(team, tournament);
        DataTable officialsDataTable = CreateOfficialsDataTable(team, tournament);
        var signOffsDataTable = GetItcSignApprovals(team);

        AddToItc(playersDataTable, worksheet, 19, 2);
        AddToItc(officialsDataTable, worksheet, 50, 2);
        // Signoffs: DATE is a merged F:G cell (col 6); NAME starts at H (col 8) — not col 7 which is inside the merge
        AddToItc(signOffsDataTable, worksheet, 61, new[] { 6, 8 });

        // Explicitly re-assert the print area so that the PDF converter always uses the full
        // A1:L70 region as the render boundary. Without this, ClosedXML may not carry the
        // Print_Area named range through to the saved stream, causing SautinSoft to infer
        // per-row widths from cell content — which clips the signoff rows at column K.
        worksheet.PageSetup.PrintAreas.Clear();
        worksheet.PageSetup.PrintAreas.Add("A1:L70");

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private DataTable GetItcSignApprovals(IPublishedContent team)
    {
        var submittedDate        = team.Value<DateTime>("iTCSubmissionDate");
        var submittedByName      = team.Value<IPublishedContent>("iTCSubmittedBy")?.Name ?? string.Empty;
        var nmaApprovedDateTime  = team.Value<DateTime>("nMAApprovedDate");
        var nmaApproverName      = team.Value<IPublishedContent>("iTCNMAApprover")?.Name ?? string.Empty;
        var iishfApprovedDate    = team.Value<DateTime>("iISHFApprovedDate");
        var iishfApproverName    = team.Value<IPublishedContent>("iISHFITCApprover")?.Name ?? string.Empty;

        var dt = new DataTable();
        dt.Columns.Add("Date", typeof(DateTime));
        dt.Columns.Add("Person", typeof(string));

        // Row 61: Team submitted
        dt.Rows.Add(submittedDate, submittedByName);
        // Row 62: NMA received from team
        dt.Rows.Add(submittedDate, nmaApproverName);
        // Row 63: NMA sent to IISHF
        dt.Rows.Add(nmaApprovedDateTime, nmaApproverName);
        // Row 64: IISHF received from NMA
        dt.Rows.Add(nmaApprovedDateTime, iishfApproverName);
        // Row 65: IISHF checked and approved
        dt.Rows.Add(iishfApprovedDate, iishfApproverName);
        // Row 66: IISHF sent approved ITC to NMA + Host
        dt.Rows.Add(iishfApprovedDate, iishfApproverName);

        return dt;
    }

    private static void AddToItc(DataTable playersDataTable, IXLWorksheet worksheet, int startRow, int startColumn)
    {
        for (int row = 0; row < playersDataTable.Rows.Count; row++)
        {
            for (int column = 0; column < playersDataTable.Columns.Count; column++)
            {
                // Column 6 (year of birth) is driven by a formula — skip writing a value to it
                if (column == 6)
                    continue;

                var cell = worksheet.Cell(startRow + row, startColumn + column);
                var value = playersDataTable.Rows[row][column];

                if (value is DateTime dateTimeValue)
                {
                    cell.Value = dateTimeValue;
                    cell.Style.DateFormat.Format = "dd.MM.yyyy";
                }
                else if (value is int intValue)
                {
                    cell.SetValue(intValue);
                }
                else if (value != null && value != DBNull.Value)
                {
                    cell.SetValue(value.ToString());
                }
                // For null/DBNull: leave the cell value blank but do NOT call cell.Clear(),
                // which would strip template background colours (e.g. signoff rows)
            }

            if (startRow + row <= 48)
            {
                string dateOfBirthCellAddress = worksheet.Cell(startRow + row, startColumn + 5).Address.ToString();
                worksheet.Cell(startRow + row, startColumn + 6).FormulaA1 =
                    $"=IF(LEN({dateOfBirthCellAddress}) = 0, 0, YEAR({dateOfBirthCellAddress}))";
            }
        }
    }

    /// <summary>
    /// Overload for sections where DataTable columns do not map to contiguous Excel columns —
    /// e.g. the signoff rows where the DATE cell is a merged F:G range and the NAME cell
    /// starts at H, meaning column indices are [6, 8] rather than [6, 7].
    /// </summary>
    private static void AddToItc(DataTable dataTable, IXLWorksheet worksheet, int startRow, int[] excelColumns)
    {
        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            for (int column = 0; column < dataTable.Columns.Count && column < excelColumns.Length; column++)
            {
                var cell = worksheet.Cell(startRow + row, excelColumns[column]);
                var value = dataTable.Rows[row][column];

                if (value is DateTime dateTimeValue)
                {
                    cell.Value = dateTimeValue;
                    cell.Style.DateFormat.Format = "dd.MM.yyyy";
                }
                else if (value is int intValue)
                {
                    cell.SetValue(intValue);
                }
                else if (value != null && value != DBNull.Value)
                {
                    cell.SetValue(value.ToString());
                }
                // For null/DBNull: leave the cell value blank, do NOT call cell.Clear()
                // as that would strip template background colours
            }
        }
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

    DataTable CreatePlayersDataTable(IPublishedContent team, IPublishedContent tournament)
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

            // Use Value<DateTime?> to let Umbraco handle the date parsing, same as officials
            var dobRaw = rosterMember.Value<DateTime?>("dateOfBirth");
            // 1753-01-01 is the Umbraco "no date" default — treat as missing
            DateTime? dob = dobRaw.HasValue && dobRaw.Value.Year > 1753 ? dobRaw : null;

            table.Rows.Add(
                rosterMember.Value<string>("licenseNumber"),
                rosterMember.Value<string>("role"),
                rosterMember.Value<string>("lastName").ToUpper(),
                rosterMember.Value<string>("firstName"),
                rosterMember.Value<int>("jerseyNumber"),
                dob.HasValue ? (object)dob.Value : DBNull.Value,
                dob.HasValue ? (object)dob.Value.Year : DBNull.Value,
                rosterMember.Value<string>("gender"),
                rosterMember.Value<string>("iso3"),
                nmaApproved ? "OK" : "Rejected",
                rosterMember.Value<string>("comments")
                );
        }

        // Return the populated DataTable
        return table;
    }

    DataTable CreateOfficialsDataTable(IPublishedContent team, IPublishedContent tournament)
    {
        // Create and populate your DataTable
        DataTable table = new DataTable();
        table.Columns.Add("LICENCE NUMBER");
        table.Columns.Add("POSITION");
        table.Columns.Add("LAST NAME");
        table.Columns.Add("First Name");
        table.Columns.Add("Intentionally blank");
        table.Columns.Add("Date of Birth", typeof(DateTime));
        table.Columns.Add("Year of birth", typeof(int));
        table.Columns.Add("Gender");
        table.Columns.Add("Nationality");
        table.Columns.Add("NMA Check");
        table.Columns.Add("Comments");

        // Add rows to the table as needed

        var playersOnRoster = team.Children().Where(x => x.ContentType.Alias == "roster" && x.Value<bool>("isBenchOfficial")).ToList();
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
                rosterMember.Value<string>(string.Empty),
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