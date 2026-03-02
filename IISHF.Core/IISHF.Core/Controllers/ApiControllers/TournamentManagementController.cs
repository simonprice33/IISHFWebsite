using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
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
        // GET /umbraco/api/tournamentmanagement/events?year=2026
        // -----------------------------
        [HttpGet("events")]
        public IActionResult Events([FromQuery] int? year = null)
        {
            var y = (year ?? DateTime.UtcNow.Year).ToString();

            var tournamentsRoot = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault();

            if (tournamentsRoot == null)
                return Ok(new { year = y, items = Array.Empty<object>() });

            // Per your tree: Tournaments -> europeanCups / championships -> events -> year (event) -> team
            var categoryAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "europeanCups",
                "championships"
            };

            var items = new List<object>();

            foreach (var category in tournamentsRoot.Children().Where(x => categoryAliases.Contains(x.ContentType.Alias)))
            {
                foreach (var ev in category.Children().Where(x => x.ContentType.Alias.Equals("events", StringComparison.OrdinalIgnoreCase)))
                {
                    var yearNode = ev.Children()
                        .FirstOrDefault(x => x.ContentType.Alias.Equals("event", StringComparison.OrdinalIgnoreCase) && x.Name == y);

                    if (yearNode == null) continue;

                    // AgeGroup appears on the yearNode in your debug (property: AgeGroup)
                    var ageGroup = yearNode.Value<string>("AgeGroup") ?? yearNode.Value<string>("ageGroup");

                    items.Add(new
                    {
                        categoryName = category.Name,
                        categoryAlias = category.ContentType.Alias,
                        eventName = ev.Name,
                        eventKey = ev.Key,
                        year = y,
                        yearNodeKey = yearNode.Key,
                        ageGroup = ageGroup
                    });
                }
            }

            return Ok(new { year = y, items });
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/nmas
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
        // GET /umbraco/api/tournamentmanagement/nmas/{nmaKey}/teams?ageGroup=U13&year=2025&search=...
        // Loads clubTeam items under: NMA -> {year} -> club -> ageGroup -> clubTeam
        // -----------------------------
        [HttpGet("nmas/{nmaKey:guid}/teams")]
        public IActionResult NmaTeams([FromRoute] Guid nmaKey, [FromQuery] string ageGroup, [FromQuery] int? year = null, [FromQuery] string search = "")
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
                return BadRequest("ageGroup is required (e.g. U13).");

            var y = (year ?? DateTime.Now.AddYears(-1).Year).ToString();
            search = (search ?? string.Empty).Trim();

            // Adjust alias if your club team doc type alias differs
            const string clubTeamAlias = "clubTeam";
            const string nmaAlias = "nationalMemberAssociation";

            // Find NMA node
            var nma = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(nmaAlias)
                .FirstOrDefault(x => x.Key == nmaKey);

            if (nma == null)
                return Ok(new { year = y, items = Array.Empty<object>() });

            // Find the year node under the NMA (node name is the year, per your URLs)
            var yearNode = nma.Children().FirstOrDefault(x => x.Name == y);
            if (yearNode == null)
                return Ok(new { year = y, items = Array.Empty<object>() });

            // Under year node: clubs -> age groups -> club teams
            // We'll grab all clubTeam descendants and filter by age group parent name/value
            var teams = yearNode
                .DescendantsOrSelfOfType(clubTeamAlias)
                .Where(t =>
                {
                    // Your URL suggests: .../{ageGroup}/{team}/, so parent could be the ageGroup node
                    var parentAge = t.Parent?.Name;
                    if (!string.Equals(parentAge, ageGroup, StringComparison.OrdinalIgnoreCase))
                    {
                        // fallback: sometimes stored as property on team
                        var ag = t.Value<string>("ageGroup");
                        if (!string.Equals(ag, ageGroup, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        if (!t.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                    }

                    return true;
                })
                .Select(t => new
                {
                    id = t.Id,      // int
                    key = t.Key,    // guid
                    name = t.Name,
                    ageGroup = t.Value<string>("ageGroup") ?? t.Parent?.Name,
                    clubName = t.Parent?.Parent?.Name, // likely club node
                })
                .OrderBy(t => t.name)
                .ToList();

            return Ok(new { year = y, items = teams });
        }

        // -----------------------------
        // GET /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams
        // Returns tournament team nodes for that event-year node, including mapping info if present.
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

                    // mapping fields
                    nmaTeamKey = t.Value<string>("NMaTeamKey"),
                    nmaTeamId = t.Value<int?>("NMaTeamId"),
                    nmaReportedName = t.Value<string>("NMaReportedName")
                })
                .OrderBy(t => t.name)
                .ToList();

            return Ok(new { items = teams });
        }

        // -----------------------------
        // POST /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/add
        // Creates a new tournament team node under the event-year node AND sets mapping.
        // Keeps the node name as the NMA-reported name initially.
        // -----------------------------
        public class AddTeamRequest
        {
            public int TeamId { get; set; }          // int id (reported team id)
            public Guid TeamKey { get; set; }        // guid key (reported team key)
            public string TeamName { get; set; }     // reported team name
        }

        [HttpPost("events/{eventYearNodeKey:guid}/teams/add")]
        public IActionResult AddTeamToEvent([FromRoute] Guid eventYearNodeKey, [FromBody] AddTeamRequest req)
        {
            if (req == null) return BadRequest("Missing body.");
            if (req.TeamKey == Guid.Empty) return BadRequest("TeamKey is required.");
            if (req.TeamId <= 0) return BadRequest("TeamId is required.");
            if (string.IsNullOrWhiteSpace(req.TeamName)) return BadRequest("TeamName is required.");

            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            // If already linked, do nothing
            var existingLinked = yearNode.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Value<string>("NMaTeamKey"), req.TeamKey.ToString(), StringComparison.OrdinalIgnoreCase));

            if (existingLinked != null)
            {
                return Ok(new
                {
                    created = false,
                    message = "Team already exists/linked in this event.",
                    eventTeam = new { id = existingLinked.Id, key = existingLinked.Key, name = existingLinked.Name }
                });
            }

            // Create + publish new team node
            var content = _contentService.Create(req.TeamName.Trim(), yearNode.Id, "team");

            content.SetValue("NMaTeamKey", req.TeamKey.ToString());
            content.SetValue("NMaTeamId", req.TeamId);
            content.SetValue("NMaReportedName", req.TeamName.Trim());

            var result = _contentService.SaveAndPublish(content);
            if (!result.Success)
                return BadRequest("Failed to create/publish team node.");

            return Ok(new
            {
                created = true,
                message = "Team added to event.",
                eventTeam = new { id = content.Id, key = content.Key, name = content.Name }
            });
        }

        // -----------------------------
        // PUT /umbraco/api/tournamentmanagement/events/{eventYearNodeKey}/teams/{eventTeamKey}/link
        // Links an existing manually-created tournament team to the NMA team (without renaming).
        // -----------------------------
        public class LinkTeamRequest
        {
            public int TeamId { get; set; }
            public Guid TeamKey { get; set; }
            public string TeamName { get; set; } // reported name (stored, but tournament name remains unchanged)
        }

        [HttpPut("events/{eventYearNodeKey:guid}/teams/{eventTeamKey:guid}/link")]
        public IActionResult LinkExistingTournamentTeam([FromRoute] Guid eventYearNodeKey, [FromRoute] Guid eventTeamKey, [FromBody] LinkTeamRequest req)
        {
            if (req == null) return BadRequest("Missing body.");
            if (req.TeamKey == Guid.Empty) return BadRequest("TeamKey is required.");
            if (req.TeamId <= 0) return BadRequest("TeamId is required.");

            var yearNode = _contentQuery.Content(eventYearNodeKey);
            if (yearNode == null) return NotFound("Event year node not found.");

            var tournamentTeam = yearNode.Children()
                .FirstOrDefault(x => x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) && x.Key == eventTeamKey);

            if (tournamentTeam == null) return NotFound("Tournament team not found under this event year.");

            // prevent linking two tournament teams to same NMA team
            var alreadyLinked = yearNode.Children()
                .FirstOrDefault(x =>
                    x.ContentType.Alias.Equals("team", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Value<string>("nMATeamKey"), req.TeamKey.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    x.Key != eventTeamKey);

            if (alreadyLinked != null)
                return BadRequest("That NMA team is already linked to a different tournament team in this event.");

            // Update existing tournament team node via IContentService
            var content = _contentService.GetById(tournamentTeam.Id);
            if (content == null) return NotFound("Tournament team content not found.");

            content.SetValue("nMATeamKey", req.TeamKey.ToString());
            content.SetValue("teamId", req.TeamId);
            if (!string.IsNullOrWhiteSpace(req.TeamName))
                content.SetValue("eventTeam", req.TeamName.Trim());

            var result = _contentService.SaveAndPublish(content);
            if (!result.Success)
                return BadRequest("Failed to save/publish link.");

            return Ok(new { linked = true, message = "Tournament team linked to NMA team." });
        }

        private static string GetIso3(IPublishedContent nma)
        {
            // tolerant alias reads (based on your backoffice label “ISO3”)
            // Most likely alias is "iso3"
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