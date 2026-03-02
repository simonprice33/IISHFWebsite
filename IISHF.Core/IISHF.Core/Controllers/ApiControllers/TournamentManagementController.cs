using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;

namespace IISHF.Core.Controllers.ApiControllers
{
    [ApiController]
    [Route("umbraco/api/tournamentmanagement")]
    public class TournamentManagementController : UmbracoApiController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;

        public TournamentManagementController(
            IPublishedContentQuery contentQuery,
            IContentService contentService)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
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
            // NOTE: keep existing property names, but bind to the CURRENT JSON payload
            // from TournamentManagement.cshtml without renaming client-side objects.

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

            [JsonPropertyName("reportedTeamName")]
            public string TeamName { get; set; } // reported name (stored; tournament name remains unchanged)

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

            // store reported name only; do not overwrite tournament team name fields
            if (!string.IsNullOrWhiteSpace(req.TeamName))
            {
                SetValueIfExists(content, "NMaReportedName", req.TeamName.Trim());
                SetValueIfExists(content, "nmaReportedName", req.TeamName.Trim());
            }

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

            // DO NOT use "eventTeam" here: that is the tournament team's display name in your schema.
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
    }
}