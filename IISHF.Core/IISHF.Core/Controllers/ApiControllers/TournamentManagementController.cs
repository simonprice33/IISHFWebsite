using DocumentFormat.OpenXml.Office2010.Excel;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using IISHF.Core.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Encoding;
using Org.BouncyCastle.Ocsp;
using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using IISHF.Core.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.Controllers;

namespace IISHF.Core.Controllers.ApiControllers
{
    [ApiController]
    [Route("umbraco/api/tournamentmanagement")]
    public class TournamentManagementController : UmbracoApiController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly Umbraco.Cms.Core.Services.IMediaService _mediaService;
        private readonly MediaFileManager _mediaFileManager;
        private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
        private readonly ITournamentService _tournamentService;

        public TournamentManagementController(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            Umbraco.Cms.Core.Services.IMediaService mediaService,
            MediaFileManager mediaFileManager,
            MediaUrlGeneratorCollection mediaUrlGenerators,
            IShortStringHelper shortStringHelper,
            IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
            ITournamentService tournamentService)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _mediaService = mediaService;
            _mediaFileManager = mediaFileManager;
            _mediaUrlGenerators = mediaUrlGenerators;
            _shortStringHelper = shortStringHelper;
            _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
            _tournamentService = tournamentService;
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/events/current
        // Finds all events under European Cups / Championships for the current calendar year.
        // Returns:
        // - eventYearNodeKey (GUID key of the YEAR node under the specific event)
        // - category ("European Cups" / "Championships")
        // - tournamentName (e.g. "U13 European Cup")
        // - year (e.g. 2026)
        // - ageGroup (e.g. U13)
        // -----------------------------
        [HttpGet("events/current")]
        public IActionResult CurrentYearEvents()
        {
            // These aliases match your debug screenshots.
            // If they differ, adjust here.
            const string tournamentsRootAlias = "tournaments";
            const string europeanCupsAlias = "europeanCups";
            const string championshipsAlias = "championships";
            const string eventAlias = "event";

            var currentYear = DateTime.Now.Year.ToString();

            var tournamentsRoot = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault();

            if (tournamentsRoot == null)
                return Ok(new { items = Array.Empty<object>() });

            // Title events only: European Cups + Championships
            var titleEventContainers = tournamentsRoot.Children()
                .Where(x =>
                    x.ContentType.Alias.Equals(europeanCupsAlias, StringComparison.OrdinalIgnoreCase) ||
                    x.ContentType.Alias.Equals(championshipsAlias, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var items = titleEventContainers
                .SelectMany(container =>
                {
                    var category = container.ContentType.Alias.Equals(europeanCupsAlias, StringComparison.OrdinalIgnoreCase)
                        ? "European Cups"
                        : "Championships";

                    return container.Children().SelectMany(tournament =>
                    {
                        var yearNode = tournament.Children()
                            .FirstOrDefault(y => y.Name == currentYear && y.ContentType.Alias.Equals(eventAlias, StringComparison.OrdinalIgnoreCase));

                        if (yearNode == null)
                            return Enumerable.Empty<CurrentYearEventItem>();

                        var ageGroup = yearNode.Value<string>("ageGroup") ?? "";

                        return new[]
                        {
                            new CurrentYearEventItem
                            {
                                Category = category,
                                TournamentName = tournament.Name,
                                Year = int.TryParse(currentYear, out var yr) ? yr : 0,
                                AgeGroup = ageGroup,
                                EventYearNodeKey = yearNode.Key
                            }
                        };
                    });
                })
                .OrderBy(x => x.Category)
                .ThenBy(x => x.TournamentName)
                .ToList();

            return Ok(new { items });
        }

        private sealed class CurrentYearEventItem
        {
            public string Category { get; set; } = "";
            public string TournamentName { get; set; } = "";
            public int Year { get; set; }
            public string AgeGroup { get; set; } = "";
            public Guid EventYearNodeKey { get; set; }
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/nmas
        // Returns all NMAs (id, key, name, iso3)
        // -----------------------------
        [HttpGet("nmas")]
        public IActionResult Nmas()
        {
            // Adjust alias if your NMA doc type alias differs
            const string nmaAlias = "nationalMemberAssociation";

            var nmas = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(nmaAlias)
                .Select(x => new
                {
                    id = x.Id,
                    key = x.Key,
                    name = x.Name,
                    iso3 = GetIso3(x)
                })
                .OrderBy(x => x.name)
                .ToList();

            return Ok(new { items = nmas });
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/nmas/{nmaKey}/teams?ageGroup=U13&year=2025&q=...
        // Loads clubTeam items under: NMA -> {year} -> club -> ageGroup -> clubTeam
        // -----------------------------
        [HttpGet("nmas/{nmaKey:guid}/teams")]
        public IActionResult NmaTeams([FromRoute] Guid nmaKey, [FromQuery] string ageGroup, [FromQuery] int? year = null, [FromQuery] string q = "")
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
                return BadRequest("ageGroup is required (e.g. U13).");

            var y = (year ?? DateTime.Now.AddYears(-1).Year).ToString();
            q = (q ?? string.Empty).Trim();

            // Adjust alias if your club team doc type alias differs
            const string clubTeamAlias = "clubTeam";
            const string nmaAlias = "nationalMemberAssociation";

            // Find NMA node
            var nma = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(nmaAlias)
                .FirstOrDefault(x => x.Key == nmaKey);

            if (nma == null)
                return Ok(new { year = y, items = Array.Empty<object>() });

            // Find the year node under the NMA (node name is the year)
            var yearNode = nma.Children().FirstOrDefault(x => x.Name == y);
            if (yearNode == null)
                return Ok(new { year = y, items = Array.Empty<object>() });

            // Under year node: clubs -> age groups -> club teams
            var teams = yearNode
                .DescendantsOrSelfOfType(clubTeamAlias)
                .Where(t =>
                {
                    var parentAge = t.Parent?.Name;
                    if (!string.Equals(parentAge, ageGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        var ag = t.Value<string>("ageGroup") ?? t.Value<string>("AgeGroup");
                        if (!string.Equals(ag, ageGroup, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }

                    if (string.IsNullOrWhiteSpace(q)) return true;
                    return t.Name.Contains(q, StringComparison.InvariantCultureIgnoreCase);
                })
                .Select(t => new
                {
                    id = t.Id,
                    key = t.Key,
                    name = t.Name
                })
                .OrderBy(t => t.name)
                .ToList();

            return Ok(new { year = y, items = teams });
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams
        // Returns tournament team nodes for that event-year node, including mapping info if present.
        // Also returns:
        // - countryIso3 (from tournament team node if present)
        // - nmaKey (from tournament team node if present)
        // -----------------------------
        [HttpGet("events/{eventYearNodeKey:guid}/teams")]
        public IActionResult EventTeams([FromRoute] Guid eventYearNodeKey)
        {
            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var teams = yearNode.Children()
                .Where(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase))
                .Select(t => new
                {
                    id = t.Id,
                    key = t.Key,
                    name = t.Name,

                    nmaTeamKey = GetTeamMapKey(t),
                    nmaTeamId = GetTeamMapId(t),
                    nmaReportedName = GetTeamReportedName(t),

                    countryIso3 = GetTeamCountryIso3(t),
                    nmaKey = GetTeamNmaKey(t)
                })
                .OrderBy(t => t.name)
                .ToList();

            return Ok(new { items = teams });
        }

        // -----------------------------
        // POST /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/add
        // Adds the selected reported team to the event-year node, and stores mapping info.
        // -----------------------------
        public class AddTeamRequest
        {
            [JsonPropertyName("reportedTeamId")]
            public int TeamId { get; set; }

            [JsonPropertyName("reportedTeamKey")]
            public Guid TeamKey { get; set; }

            [JsonPropertyName("reportedTeamName")]
            public string TeamName { get; set; }

            [JsonPropertyName("nmaKey")]
            public Guid NmaKey { get; set; }

            [JsonPropertyName("nmaIso3")]
            public string NmaIso3 { get; set; }
        }

        [HttpPost("events/{eventYearNodeKey:guid}/teams/add")] 
        public IActionResult AddTeamToEvent([FromRoute] Guid eventYearNodeKey, [FromBody] AddTeamRequest req)
        {
            if (req == null) return BadRequest("Missing body.");
            if (req.TeamKey == Guid.Empty) return BadRequest("reportedTeamKey is required.");
            if (req.TeamId <= 0) return BadRequest("reportedTeamId is required.");
            if (string.IsNullOrWhiteSpace(req.TeamName)) return BadRequest("reportedTeamName is required.");
            if (req.NmaKey == Guid.Empty) return BadRequest("nmaKey is required.");

            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            // If already linked, do nothing
            var existingLinked = yearNode.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(GetTeamMapKey(x), req.TeamKey.ToString(), StringComparison.OrdinalIgnoreCase));

            if (existingLinked != null)
            {
                return Ok(new
                {
                    created = false,
                    message = "Team already exists/linked in this event.",
                    eventTeam = new
                    {
                        id = existingLinked.Id,
                        key = existingLinked.Key,
                        name = existingLinked.Name,
                        countryIso3 = GetTeamCountryIso3(existingLinked),
                        nmaKey = GetTeamNmaKey(existingLinked)
                    }
                });
            }

            // Create + publish new team node (tournament team name initially uses reported name)
            var content = _contentService.Create(req.TeamName.Trim(), yearNode.Id, "team");

            // store mapping (tolerant to mixed alias casing)
            SetValueIfExists(content, "eventTeam", req.TeamName.ToString());

            SetValueIfExists(content, "nMATeamKey", req.TeamKey.ToString());
            SetValueIfExists(content, "NMaTeamKey", req.TeamKey.ToString());

            SetValueIfExists(content, "teamId", req.TeamId);
            SetValueIfExists(content, "NMaTeamId", req.TeamId);

            SetValueIfExists(content, "NMaReportedName", req.TeamName.Trim());
            SetValueIfExists(content, "nmaReportedName", req.TeamName.Trim());

            // requested: persist NMA key + ISO3 onto tournament team
            SetValueIfExists(content, "nmaKey", req.NmaKey.ToString());
            SetValueIfExists(content, "nMAKey", req.NmaKey.ToString()); // just in case
            
            var iso3 = !string.IsNullOrWhiteSpace(req.NmaIso3) ? req.NmaIso3.Trim() : ResolveNmaIso3(req.NmaKey);
            if (!string.IsNullOrWhiteSpace(iso3))
            {
                SetValueIfExists(content, "countryIso3", iso3);
                SetValueIfExists(content, "iso3", iso3); // if you used that alias on the team node
            }

            var result = _contentService.SaveAndPublish(content);
            if (!result.Success)
                return BadRequest("Failed to create/publish team node.");

            return Ok(new
            {
                created = true,
                message = "Team added to event.",
                eventTeam = new
                {
                    id = content.Id,
                    key = content.Key,
                    name = content.Name,
                    countryIso3 = iso3,
                    nmaKey = req.NmaKey
                }
            });
        }

        // -----------------------------
        // POST /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/link
        // Links existing tournament team -> reported team (WITHOUT renaming tournament team).
        // -----------------------------
        public class LinkTeamRequest
        {
            [JsonPropertyName("tournamentTeamKey")]
            public Guid TournamentTeamKey { get; set; }

            [JsonPropertyName("reportedTeamId")]
            public int TeamId { get; set; }

            [JsonPropertyName("reportedTeamKey")]
            public Guid TeamKey { get; set; }

            [JsonPropertyName("nmaKey")]
            public Guid NmaKey { get; set; }

            [JsonPropertyName("nmaIso3")]
            public string NmaIso3 { get; set; }
        }

        [HttpPost("events/{eventYearNodeKey:guid}/teams/link")]
        public IActionResult LinkTournamentTeam([FromRoute] Guid eventYearNodeKey, [FromBody] LinkTeamRequest req)
        {
            if (req == null) return BadRequest("Missing body.");
            if (req.TournamentTeamKey == Guid.Empty) return BadRequest("tournamentTeamKey is required.");
            if (req.TeamKey == Guid.Empty) return BadRequest("reportedTeamKey is required.");
            if (req.TeamId <= 0) return BadRequest("reportedTeamId is required.");
            if (req.NmaKey == Guid.Empty) return BadRequest("nmaKey is required.");

            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var tournamentTeam = yearNode.Children()
                .FirstOrDefault(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) && x.Key == req.TournamentTeamKey);

            if (tournamentTeam == null) return NotFound("Tournament team not found under this event year.");

            // prevent linking two tournament teams to same NMA team
            var alreadyLinked = yearNode.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(GetTeamMapKey(x), req.TeamKey.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    x.Key != req.TournamentTeamKey);

            if (alreadyLinked != null)
                return BadRequest("That reported team is already linked to a different tournament team in this event.");

            var content = _contentService.GetById(tournamentTeam.Id);
            if (content == null) return NotFound("Tournament team content not found.");

            SetValueIfExists(content, "nMATeamKey", req.TeamKey.ToString());
            SetValueIfExists(content, "NMaTeamKey", req.TeamKey.ToString());

            SetValueIfExists(content, "teamId", req.TeamId);
            SetValueIfExists(content, "NMaTeamId", req.TeamId);

            // requested: persist NMA key + ISO3 onto tournament team
            SetValueIfExists(content, "nmaKey", req.NmaKey.ToString());
            SetValueIfExists(content, "nMAKey", req.NmaKey.ToString());

            var iso3 = !string.IsNullOrWhiteSpace(req.NmaIso3) ? req.NmaIso3.Trim() : ResolveNmaIso3(req.NmaKey);
            if (!string.IsNullOrWhiteSpace(iso3))
            {
                SetValueIfExists(content, "countryIso3", iso3);
                SetValueIfExists(content, "iso3", iso3);
            }

            var result = _contentService.SaveAndPublish(content);
            if (!result.Success)
                return BadRequest("Failed to save/publish link.");

            return Ok(new { linked = true, message = "Tournament team linked to reported team." });
        }

        [HttpDelete("events/{eventYearNodeKey:guid}/teams/{teamKey:guid}")]
        public IActionResult RemoveTeamFromEvent([FromRoute] Guid eventYearNodeKey, [FromRoute] Guid teamKey)
        {
            if (eventYearNodeKey == Guid.Empty) return BadRequest("eventYearNodeKey is required.");
            if (teamKey == Guid.Empty) return BadRequest("teamKey is required.");

            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var team = yearNode.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase)
                    && x.Key == teamKey);

            if (team == null)
                return NotFound("Team not found under this event.");

            var content = _contentService.GetById(team.Id);
            if (content == null)
                return NotFound("Team content not found.");

            var result = _contentService.Delete(content);
            if (result == null || !result.Success)
                return BadRequest("Failed to delete team.");

            return Ok(new { removed = true, message = "Team removed from event." });
        }

        // -----------------------------
        // Legacy endpoint retained (earlier iteration)
        // PUT /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/{eventTeamKey}/link
        // -----------------------------
        [HttpPut("events/{eventYearNodeKey:guid}/teams/{eventTeamKey:guid}/link")]
        public IActionResult LinkExistingTournamentTeam([FromRoute] Guid eventYearNodeKey, [FromRoute] Guid eventTeamKey, [FromBody] LinkTeamRequest req)
        {
            if (req == null) return BadRequest("Missing body.");
            req.TournamentTeamKey = eventTeamKey;
            return LinkTournamentTeam(eventYearNodeKey, req);
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/{tournamentTeamKey}
        // Returns editable fields + logo
        // -----------------------------
        [HttpGet("events/{eventYearNodeKey:guid}/teams/{tournamentTeamKey:guid}")]
        public IActionResult GetTournamentTeam([FromRoute] Guid eventYearNodeKey, [FromRoute] Guid tournamentTeamKey)
        {
            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var team = yearNode.Children()
                .FirstOrDefault(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) && x.Key == tournamentTeamKey);

            if (team == null) return NotFound("Tournament team not found under this event year.");

            // Media picker "image"
            string logoUrl = "";
            Guid? logoKey = null;
            int? logoId = null;

            var logoMedia = team.Value<IPublishedContent>("image");
            if (logoMedia != null)
            {
                logoUrl = logoMedia.Url();
                logoKey = logoMedia.Key;
                logoId = logoMedia.Id;
            }

            var links = team.Value<Link[]>("teamWebsite");
            var websiteUrl = (links != null && links.Length > 0) ? (links[0]?.Url ?? "") : "";

            // ITC status
            var itcEval = ItcStateMachine.Evaluate(team);
            var itcStatus = itcEval.State.GetDescription();

            // Team information submission status
            var teamInfoSubmitted = team.Value<bool>("teamInformationSubmitted");
            var submissionDate = team.Value<DateTime?>("teamInformationSubmissionDate");
            var submittedByRef = team.Value<IPublishedContent>("teamInformationSubmittedBy");

            return Ok(new
            {
                key = team.Key,
                id = team.Id,

                name = team.Name,

                eventTeam = team.Value<string>("eventTeam") ?? "",
                countryIso3 = team.Value<string>("countryIso3") ?? "",
                @group = team.Value<string>("group") ?? "",

                teamWebsite = websiteUrl,

                logo = new
                {
                    id = logoId,
                    key = logoKey,
                    url = logoUrl
                },

                itcStatus = itcStatus,
                teamInformationSubmitted = teamInfoSubmitted,
                teamInformationSubmissionDate = submissionDate.HasValue && submissionDate.Value != DateTime.MinValue
                    ? submissionDate.Value.ToString("dd MMM yyyy HH:mm") + " UTC"
                    : (string)null,
                teamInformationSubmittedBy = submittedByRef?.Name
            });
        }

        // -----------------------------
        // DELETE /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/submission-status
        // Resets teamInformationSubmitted, teamInformationSubmissionDate and teamInformationSubmittedBy
        // for every team under the given event year node in one go.
        // -----------------------------
        [HttpDelete("events/{eventYearNodeKey:guid}/teams/submission-status")]
        public async Task<IActionResult> ResetAllTeamSubmissionStatuses([FromRoute] Guid eventYearNodeKey)
        {
            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var teams = yearNode.Children()
                .Where(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var team in teams)
            {
                await _tournamentService.ResetTeamInformationSubmission(team);
            }

            return Ok(new { resetCount = teams.Count });
        }

        // -----------------------------
        // POST /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/{tournamentTeamKey}/update
        // Updates core fields + logo media picker
        // -----------------------------
        [HttpPost("events/{eventYearNodeKey:guid}/teams/{tournamentTeamKey:guid}/update")]
        public IActionResult UpdateTournamentTeam([FromRoute] Guid eventYearNodeKey, [FromRoute] Guid tournamentTeamKey, [FromBody] UpdateTournamentTeamRequest req)
        {
            if (req == null) return BadRequest("Missing body.");

            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var publishedTeam = yearNode.Children()
                .FirstOrDefault(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) && x.Key == tournamentTeamKey);

            if (publishedTeam == null) return NotFound("Tournament team not found under this event year.");

            var content = _contentService.GetById(publishedTeam.Id);
            if (content == null) return NotFound("Tournament team content not found.");

            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                content.Name = req.Name.Trim();
            }

            SetValueIfExists(content, "eventTeam", req.EventTeam?.Trim() ?? "");
            SetValueIfExists(content, "countryIso3", req.CountryIso3?.Trim() ?? "");
            SetValueIfExists(content, "group", req.Group?.Trim() ?? "");

            if (content.HasProperty("teamWebsite"))
            {
                var url = (req.TeamWebsite ?? "").Trim();
                if (string.IsNullOrWhiteSpace(url))
                {
                    content.SetValue("teamWebsite", "[]");
                }
                else
                {
                    var json = "[{\"name\":\"\",\"url\":\"" + JavaScriptEncoder.Default.Encode(url) + "\",\"target\":\"\",\"type\":\"external\"}]";
                    content.SetValue("teamWebsite", json);
                }
            }

            if (content.HasProperty("image"))
            {
                if (req.ClearLogo)
                {
                    content.SetValue("image", null);
                }
                else if (req.LogoKey.HasValue && req.LogoKey.Value != Guid.Empty)
                {
                    // CHANGED: set media picker directly from key (UDI), no lookup required
                    var udi = Udi.Create(Constants.UdiEntityType.Media, req.LogoKey.Value);
                    content.SetValue("image", udi.ToString());
                }
            }

            var result = _contentService.SaveAndPublish(content);
            if (!result.Success)
                return BadRequest("Failed to save/publish tournament team.");

            return Ok(new { updated = true, message = "Tournament team updated." });
        }

        // -----------------------------
        // POST /umbraco/api/tournamentmanagement/logos/upload
        // multipart/form-data with file field name "file"
        // Returns new media { id, key, name, url }
        // -----------------------------
        [HttpPost("logos/upload")]
        [RequestSizeLimit(50_000_000)]
        public IActionResult UploadLogo([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var logosFolder = _contentQuery
                .MediaAtRoot()
                .SelectMany(x => x.DescendantsOrSelf())
                .FirstOrDefault(x => x.Name.Equals("Logos", StringComparison.OrdinalIgnoreCase));

            if (logosFolder == null)
                return BadRequest("Logos folder not found in Media.");

            var safeName = System.IO.Path.GetFileNameWithoutExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "logo";

            var media = _mediaService.CreateMedia(safeName, logosFolder.Id, Constants.Conventions.MediaTypes.Image);

            using (var stream = file.OpenReadStream())
            {
                media.SetValue(
                    _mediaFileManager,
                    _mediaUrlGenerators,
                    _shortStringHelper,
                    _contentTypeBaseServiceProvider,
                    Constants.Conventions.Media.File,
                    file.FileName,
                    stream);
            }

            _mediaService.Save(media);

            var published = _contentQuery.Media(media.Key);
            var url = published?.Url() ?? "";

            return Ok(new
            {
                created = true,
                item = new
                {
                    id = media.Id,
                    key = media.Key,
                    name = media.Name,
                    url = url
                }
            });
        }

        // CHANGED: ensure route matches JS: /umbraco/api/tournamentmanagement/logos
        [HttpGet("logos")]
        public IActionResult Logos()
        {
            var logosFolder = _contentQuery.MediaAtRoot()
                .SelectMany(x => x.DescendantsOrSelf())
                .FirstOrDefault(x => x.Name.Equals("Logos", StringComparison.OrdinalIgnoreCase));

            if (logosFolder == null)
            {
                return Ok(new { items = Array.Empty<object>() });
            }

            var logos = logosFolder
                .Children()
                .Select(x => new
                {
                    id = x.Id,
                    key = x.Key,
                    name = x.Name,
                    url = x.Url()
                })
                .OrderBy(x => x.name)
                .ToList();

            return Ok(new { items = logos });
        }

        // -----------------------------
        // POST /umbraco/api/tournamentmanagement/events/create
        // Creates a new "event" under the selected tournament (by EventShotCode == TitleEvent)
        // Assigns hostImage using model.HostImage (media key GUID string)
        // -----------------------------
        [HttpPost("events/create")]
        public IActionResult CreateEvent([FromBody] IISHF.Core.Models.TournamentModel model)
        {
            if (model == null) return BadRequest("Missing model.");
            if (model.EventYear <= 0) return BadRequest("EventYear is required.");
            if (string.IsNullOrWhiteSpace(model.TitleEvent)) return BadRequest("TitleEvent is required.");
            if (string.IsNullOrWhiteSpace(model.SanctionNumber)) return BadRequest("SanctionNumber is required."); // CHANGED

            var rootContent = _contentQuery.ContentAtRoot().ToList();

            var tournament = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children()?
                .FirstOrDefault(x => x.Name.ToLower().Contains(model.IsChampionships ? "championships" : "cup"))!
                .Children()
                .FirstOrDefault(x => x.Value<string>("EventShotCode") == model.TitleEvent);

            if (tournament == null)
            {
                return BadRequest("Tournament not found for TitleEvent/EventShotCode.");
            }

            var linkObject = new
            {
                name = $"{model.HostClub} Website",
                url = model.HostWebsite,
                target = "_blank",
            };

            var jsonLinkArray = JsonSerializer.Serialize(new[] { linkObject });

            var iishfEvent = _contentService.Create(model.EventYear.ToString(), tournament.Id, "event");
            if (iishfEvent == null) return BadRequest("Failed to create event node.");

            iishfEvent.SetValue("eventStartDate", model.EventStartDate);
            iishfEvent.SetValue("eventEndDate", model.EventEndDate);
            iishfEvent.SetValue("hostClub", model.HostClub);
            iishfEvent.SetValue("hostContact", model.HostContact);
            iishfEvent.SetValue("hostPhoneNumber", model.HostPhoneNumber);
            iishfEvent.SetValue("hostEmail", model.HostEmail);
            iishfEvent.SetValue("hostWebSite", jsonLinkArray);
            iishfEvent.SetValue("venueName", model.VenueName);
            iishfEvent.SetValue("venueAddress", model.VenueAddress);
            iishfEvent.SetValue("rinkSizeLength", model.RinkLength);
            iishfEvent.SetValue("rinkSizeWidth", model.RinkWidth);
            iishfEvent.SetValue("rinkFloor", model.RinkFloor);

            // CHANGED: persist sanction number (only if property exists)
            if (iishfEvent.HasProperty("sanctionNumber"))
            {
                iishfEvent.SetValue("sanctionNumber", model.SanctionNumber);
            }

            // hostImage: model.HostImage is GUID key string (selected existing OR uploaded)
            if (iishfEvent.HasProperty("hostImage") && !string.IsNullOrWhiteSpace(model.HostImage))
            {
                if (Guid.TryParse(model.HostImage, out var mediaKey) && mediaKey != Guid.Empty)
                {
                    var udi = Udi.Create(Constants.UdiEntityType.Media, mediaKey);
                    iishfEvent.SetValue("hostImage", udi.ToString());
                }
            }

            _contentService.SaveAndPublish(iishfEvent);

            return Ok(new
            {
                created = true,
                message = "Event created.",
                eventYearNodeKey = iishfEvent.Key
            });
        }



        [HttpPut("events/team-information-submission-status/team/{teamId}/set-chase")]
        public IActionResult SetLastChasedTime([FromRoute] Guid teamId,  [FromBody] SetChase model )
        {
            if (teamId == Guid.Empty)
            {
                return BadRequest();
            }

            var tournamentTeam = _contentService.GetById(teamId);

            if (tournamentTeam == null)
            {
                return NotFound("Team Not found");
            }

            SetValueIfExists(tournamentTeam, "lastTeamInformationReminderSent", model.LastChaseTime);

            _contentService.SaveAndPublish(tournamentTeam);

            return Ok();
        }

        [HttpGet("events/team-information-submission-status")]
        public IActionResult CurrentYearTeamInformationSubmissionStatus()
        {
            var currentYear = DateTime.Now.Year.ToString();
            var today = DateTime.Today;

            // IMPORTANT: keep your known-working approach
            var tournamentsRoot = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault();

            if (tournamentsRoot == null)
                return Ok(Array.Empty<TeamInformationSubmissionStatus>());

            // These two containers exist under Tournaments in your setup (based on earlier code)
            var titleEventContainers = tournamentsRoot.Children()
                .Where(x =>
                    x.ContentType.Alias.Equals("europeanCups", StringComparison.OrdinalIgnoreCase) ||
                    x.ContentType.Alias.Equals("championships", StringComparison.OrdinalIgnoreCase))
                .ToList();

            const string nmaAlias = "nationalMemberAssociation";

            // Find NMA node
            var nma = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(nmaAlias);

            var result = titleEventContainers
                .SelectMany(container => container.Children())
                .SelectMany<IPublishedContent, TeamInformationSubmissionStatus>(tournament =>
                {
                    // under tournament: the year nodes, with alias "event" in your previous logic
                    var eventYearNode = tournament.Children()
                        .FirstOrDefault(y =>
                            y.Name == currentYear &&
                            y.ContentType.Alias.Equals("event", StringComparison.OrdinalIgnoreCase));

                    if (eventYearNode == null)
                        return Enumerable.Empty<TeamInformationSubmissionStatus>();

                    // determine completed (exclude)
                    var eventEndDate = eventYearNode.Value<DateTime?>("eventEndDate");
                    if (eventEndDate.HasValue && eventEndDate.Value.Date < today)
                        return Enumerable.Empty<TeamInformationSubmissionStatus>();

                    var sanctionNumber = eventYearNode.Value<string>("sanctionNumber") ?? "";
                    var eventStartDate = eventYearNode.Value<DateTime?>("eventStartDate");

                    // teams under event year node
                    var submissionInfo = eventYearNode.Children()
                        .Where(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase))
                        .Select(team =>
                        {
                            var linkedTeam = _contentQuery.Content(team.Value<string>("nMATeamKey"));
                            var contactEmail =
                                linkedTeam == null ||
                                string.IsNullOrWhiteSpace(linkedTeam.Value<string>("teamContactEmail"))
                                    ? "Contact email not assigned to team"
                                    : linkedTeam.Value<string>("teamContactEmail");
                            var contatName =
                                linkedTeam == null ||
                                string.IsNullOrWhiteSpace(linkedTeam.Value<string>("teamContactName"))
                                    ? "Contact name not assigned to team"
                                    : linkedTeam.Value<string>("teamContactName");

                            var submissionDate = team.Value<DateTime?>("teamInformationSubmissionDate");
                            var reminderDate = team.Value<DateTime?>("lastTeamInformationReminderSent");
                            var nmaKey = team.Value<Guid>("nmaKey");
                            var teamNma = nma.ToList().FirstOrDefault(x => x.Key == nmaKey);
                            var nmaEmail = teamNma == null || string.IsNullOrWhiteSpace(teamNma.Value<string>("email"))
                                ? "NMA not assigned to team"
                                : teamNma.Value<string>("email");

                            var nationalMemberAssication = _contentQuery.Content(nmaKey);

                            if (nationalMemberAssication != null)
                            {

                            }

                            return new TeamSubmissionInfo()
                            {
                                Id = team.Key,
                                TeamName = team.Name,
                                teamInformationSubmissionDate =
                                    submissionDate == DateTime.MinValue ? null : submissionDate,
                                lastTeamInformationReminderSent =
                                    reminderDate == DateTime.MinValue ? null : reminderDate,
                                NmaEmail = nmaEmail,
                                TeamEmail = contactEmail,
                                TeamContact = contatName,
                                Nma = nationalMemberAssication?.Name ?? "NMA not linked to team",
                            };
                        });

                    return new[]
                    {
                new TeamInformationSubmissionStatus()
                {
                    SubmissionInfo = submissionInfo.ToList(),
                    TournamentId = eventYearNode.Id,
                    TournamentKey = eventYearNode.Key,
                    SanctionNumber = sanctionNumber,
                    EventStartDate = eventStartDate,
                    EventName = tournament.Name,
                    DueDate = eventStartDate.Value.AddDays(-56),
                }
                    };
                });

            return Ok(result.ToList());
        }

        // -----------------------------
        // Helpers
        // -----------------------------
        private static void SetValueIfExists(IContent content, string alias, object value)
        {
            if (content == null) return;
            if (string.IsNullOrWhiteSpace(alias)) return;
            if (!content.HasProperty(alias)) return;

            content.SetValue(alias, value);
        }

        private string ResolveNmaIso3(Guid nmaKey)
        {
            if (nmaKey == Guid.Empty) return "";

            const string nmaAlias = "nationalMemberAssociation";

            var nma = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(nmaAlias)
                .FirstOrDefault(x => x.Key == nmaKey);

            return nma == null ? "" : GetIso3(nma);
        }

        private static string GetTeamMapKey(IPublishedContent t)
        {
            var v = t?.Value<string>("nMATeamKey");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            v = t?.Value<string>("NMaTeamKey");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            v = t?.Value<string>("nmaTeamKey");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            return "";
        }

        private static int? GetTeamMapId(IPublishedContent t)
        {
            var v = t?.Value<int?>("teamId");
            if (v.HasValue && v.Value > 0) return v;

            v = t?.Value<int?>("NMaTeamId");
            if (v.HasValue && v.Value > 0) return v;

            v = t?.Value<int?>("nmaTeamId");
            if (v.HasValue && v.Value > 0) return v;

            return null;
        }

        private static string GetTeamReportedName(IPublishedContent t)
        {
            var v = t?.Value<string>("NMaReportedName");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            v = t?.Value<string>("nmaReportedName");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            return "";
        }

        private static string GetTeamCountryIso3(IPublishedContent t)
        {
            var v = t?.Value<string>("countryIso3");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            v = t?.Value<string>("iso3");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            return "";
        }

        private static string GetTeamNmaKey(IPublishedContent t)
        {
            var v = t?.Value<string>("nmaKey");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            v = t?.Value<string>("nMAKey");
            if (!string.IsNullOrWhiteSpace(v)) return v;

            return "";
        }

        private static string GetIso3(IPublishedContent nma)
        {
            var iso3 = nma.Value<string>("iso3");
            if (!string.IsNullOrWhiteSpace(iso3)) return iso3;

            iso3 = nma.Value<string>("ISO3");
            if (!string.IsNullOrWhiteSpace(iso3)) return iso3;

            iso3 = nma.Value<string>("countryIso3");
            if (!string.IsNullOrWhiteSpace(iso3)) return iso3;

            return "";
        }

        public sealed class UpdateTournamentTeamRequest
        {
            public string Name { get; set; }
            public string EventTeam { get; set; }
            public string CountryIso3 { get; set; }
            public string Group { get; set; }
            public string TeamWebsite { get; set; }

            public Guid? LogoKey { get; set; }
            public bool ClearLogo { get; set; }
        }

        public class TeamInformationSubmissionStatus 
        {
            public TeamInformationSubmissionStatus()
            {
                SubmissionInfo = new List<TeamSubmissionInfo>();
            }

            public int TournamentId { get; set; }

            public Guid TournamentKey { get; set; }

            public string EventName { get; set; }

            public string SanctionNumber { get; set; }

            public DateTime? EventStartDate { get; set; }

            public DateTime DueDate { get; set; }
            
            public List<TeamSubmissionInfo> SubmissionInfo { get; set; }
        }

        public class TeamSubmissionInfo
        {
            public Guid Id { get; set; }
            
            public string TeamName { get; set; }

            public string TeamContact { get; set; }
            
            public string TeamEmail { get; set; }
         
            public DateTime? teamInformationSubmissionDate { get; set; }

            public DateTime? lastTeamInformationReminderSent { get; set; }

            public string NmaEmail { get; set; }

            public string Nma { get; set; }
        }

        public class SetChase
        {
            public DateTime LastChaseTime { get; set; }
        }
    }
}