using System.Text.Json;
using System.Web;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Filters;
using IMediaService = IISHF.Core.Interfaces.IMediaService;

namespace IISHF.Core.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly ITournamentService _tournamentService;
        private readonly IRosterService _rosterService;
        private readonly IEventResultsService _eventResultsService;
        private readonly IMediaService _mediaService;
        private readonly ITeamService _teamService;
        private readonly ILogger<EventsController> _logger;
        private readonly JsonSerializerOptions _options;

        public EventsController(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            ITournamentService tournamentService,
            IRosterService rosterService,
            IEventResultsService eventResultsService,
            IMediaService mediaService,
            ITeamService teamService,
            ILogger<EventsController> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
            _tournamentService = tournamentService;
            _rosterService = rosterService;
            _eventResultsService = eventResultsService;
            _mediaService = mediaService;
            _teamService = teamService;
            _logger = logger;

            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [HttpPost("Team")]
        public IActionResult PostTeam([FromBody] Team model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            if (tournament == null)
            {
                return NotFound();
            }

            var teamId = _tournamentService.CreateEventTeam(model, tournament);

            return Created(new Uri(teamId.Id.ToString(), UriKind.RelativeOrAbsolute), teamId.Id.ToString());
        }

        [HttpPost("Roster")]
        public async Task<IActionResult> PostRosterMember([FromBody] RosterMembers model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamByName(model.TeamName, tournament);

            if (team == null)
            {
                return NotFound();
            }

            await _rosterService.UpsertRosterMembers(model, team);

            return Ok();
        }

        [HttpPut("Statistics")]
        public IActionResult PutStatistics([FromBody] UpdatePlayerStatistics model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            _eventResultsService.UpdatePlayerStatistics(model, tournament);

            return NoContent();
        }

        [HttpPut("Ranking")]
        //[ApiKeyAuthorize]
        public IActionResult PutRanking([FromBody] Rankings model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            _eventResultsService.UpdateGroupRanking(model, tournament);

            return NoContent();
        }

        [HttpPut("Placement")]
        //[ApiKeyAuthorize]
        public IActionResult PutPlacement([FromBody] TeamPlacements model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            _eventResultsService.UpdateFinalPlacement(model, tournament);

            return NoContent();
        }

        [HttpPut("ScheduleGame")]
        //[ApiKeyAuthorize]
        public IActionResult PutScheduleGame([FromBody] UpdateTeamScores model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));
            if (tournament == null)
            {
                return NotFound("Tournament not found");
            }

            try
            {
                _tournamentService.UpdateGameWithResults(model, tournament);
            }
            catch (NullReferenceException nEx)
            {
                return NotFound(nEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has been thrown");
                throw;
            }

            return NoContent();
        }

        [HttpPost("ScheduleGame")]
        //[ApiKeyAuthorize]
        public IActionResult PostScheduleGame([FromBody] CreateScheduleGames model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament != null)
            {
                _tournamentService.CreateEventGame(model, tournament);
            }

            return NoContent();
        }

        [HttpPut("VideoFeed")]
        public IActionResult PutVideoFeeed([FromBody] VideoFeeds model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            _mediaService.SetVideoFeed(model, tournament);

            return NoContent();
        }

        [HttpPost("SanctionedEvent")]
        public IActionResult PostTournament([FromBody] TournamentModel model)
        {
            var iishfEventId = _tournamentService.CreateEvent(model);

            return iishfEventId == null
                ? NotFound()
                : Created(new Uri(iishfEventId.ToString(), UriKind.RelativeOrAbsolute), iishfEventId.ToString());
        }

        [HttpPut("itc")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> PutItcRoster([FromBody] RosterMembers model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamByName(model.TeamName, tournament);

            if (team == null)
            {
                return NotFound();
            }

            var result = await _rosterService.UpsertRosterMembers(model, team);
            var json = JsonSerializer.Serialize(result);
            return Ok(json);
        }

        [HttpPut("team-information-submission")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> PutTeamInformationSubmission(IFormCollection formCollection)
        {
            var files = formCollection.Files;
            var teamPhoto = files["teamPhotoDropzone"];
            var teamLogo = files["teamLogoDropzone"];
            var sponsors = files.Where(x => x.Name == "multipleFilesDropzone").ToList();

            var model = JsonSerializer.Deserialize<TeamInformationSubmission>(formCollection["json"].ToString(), _options);

            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamByName(model.TeamName, tournament);

            if (team == null)
            {
                return NotFound();
            }

            var result = await _rosterService.UpsertRosterMembers(model, team);

            var nmaTeam = _contentQuery.Content(team.Value<Guid>("nMATeamKey"));
            await _teamService.UpdateNmaTeamHistory(model.TeamHistory, nmaTeam);

            if (teamPhoto != null)
            {
                var teamPhotoMedia = await _teamService.UploadTeamPhoto(teamPhoto, "TeamPhotos");
                await _teamService.AddImageToTeam(teamPhotoMedia, nmaTeam, "teamPhoto");
            }

            if (teamLogo != null)
            {
                var teamLogoMedia = await _teamService.UploadTeamPhoto(teamLogo, "Unsanitised Logos");
                await _teamService.AddImageToTeam(teamLogoMedia, nmaTeam, "teamLogo");
            }

            if (sponsors != null && sponsors.Any())
            {
                await _teamService.UploadSponsors(sponsors, nmaTeam, "Sponsors");
            }
            
            // update jersey colours for event team
            await _tournamentService.UpdateTeamColours(model.JerseyOne, "jerseyOneColour", team);
            await _tournamentService.UpdateTeamColours(model.JerseyTwo, "jerseyTwoColour", team);

            // Re-get nma team object and return saved \ published information
            nmaTeam = _contentQuery.Content(team.Value<Guid>("nMATeamKey"));
            var sponsorList = nmaTeam.Children.Where(x => x.ContentType.Alias == "sponsor")
                .Select(x => x.Value<IPublishedContent>("sponsorImage").Url().ToString()).ToList();

            var teamPhotoUrl = nmaTeam.Value<IPublishedContent>("teamPhoto");
            var teamLogoUrl = nmaTeam.Value<IPublishedContent>("teamLogo");

            var responseModel = new TeamInformationModel
            {
                ItcRosterMembers = result.ItcRosterMembers,
                TeamPhotoPath = teamPhotoUrl == null ? string.Empty : teamPhotoUrl.Url(),
                TeamLogoPath = teamLogoUrl == null ? string.Empty : teamLogoUrl.Url(),
                SponsorPaths = sponsorList,
                TeamHistory = model.TeamHistory,
                JerseyOneColour = model.JerseyOne,
                JerseyTwoColour = model.JerseyTwo,
                SubmittedDate = model.SubmitToHost ? DateTime.UtcNow : null
            };

            if (model.SubmitToHost)
            {
                await _tournamentService.SetSubmissionDate(team);

                team = _contentQuery.Content(team.Id);
                await _tournamentService.SubmitTeamInformationToHost(tournament, nmaTeam, team);
            }

            return Ok(responseModel);
        }


        [HttpDelete]
        [Route("itc/team/titel-event/{titleEvent}/championship/{isChampionship}/year/{eventYear}/team/{teamName}/roster-member/{rosterId}/")]
        [Route("team-submission/team/titel-event/{titleEvent}/championship/{isChampionship}/year/{eventYear}/team/{teamName}/roster-member/{rosterId}/")]
        public async Task<IActionResult> DeleteFromRoster(string titleEvent, bool isChampionship, int eventYear, string teamName, int rosterId)
        {
            // Check we have the tournament
            var tournament = _tournamentService.GetTournament(
                isChampionship,
                titleEvent,
                eventYear.ToString());

            if (tournament == null)
            {
                return NotFound();
            }

            teamName = HttpUtility.UrlDecode(teamName);

            // Check team is in that tournament
            var team = _tournamentService.GetTournamentTeamByName(teamName, tournament);

            if (team == null)
            {
                return NotFound();
            }

            // Check roster member belongs to this team
            var rosterMember = _rosterService.FindRosterMemberById(rosterId, team);

            if (rosterMember == null)
            {
                return NotFound();
            }

            // delete roster member
            _rosterService.DeleteRosteredPlayer(rosterMember.Id);
            return NoContent();
        }
    }
}
