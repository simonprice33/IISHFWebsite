using System.Text.Json;
using System.Web;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Lucene.Net.Index;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Extensions;
using IMediaService = IISHFTest.Core.Interfaces.IMediaService;

namespace IISHFTest.Core.Controllers.ApiControllers
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
        public IActionResult PostRosterMember([FromBody] RosterMembers model)
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

            _rosterService.UpsertRosterMembers(model, team);

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
        public IActionResult PutItcRoster([FromBody] RosterMembers model)
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

            var result = _rosterService.UpsertRosterMembers(model, team);
            var json = JsonSerializer.Serialize(result);
            return Ok(json);
        }

        [HttpPut("team-information-submission")]
        [UmbracoMemberAuthorize]
        public IActionResult PutTeamInformationSubmission(IFormCollection formCollection)
        {

            // ToDo
            // Get files from collection
            var files = formCollection.Files;
           var teamPhoto = files["teamPhotoDropzone"];

            // Get json by key of json into model
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

            // save players and coaches to roster
            var result = _rosterService.UpsertRosterMembers(model, team);

            // save hd version of club image 

            // save club \ team history as the html from the rich text box
            var nmaTeam = _contentQuery.Content(team.Value<Guid>("nMATeamKey"));
            _teamService.UpdateNmaTeamHistory(model.TeamHistory, nmaTeam);
            var imageUrl = _teamService.UploadTeamPhoto(files["teamPhotoDropzone"]).Result;
            _teamService.AddImageToTeam(imageUrl, nmaTeam);

            var club = nmaTeam.Parent;
            
            // update jersey colours for event team
            _tournamentService.UpdateTeamColours(model.JerseyOne, "jerseyOneColour", team);
            _tournamentService.UpdateTeamColours(model.JerseyTwo, "jerseyTwoColour", team);

            // check status - is being submitted or saved as draft?

            var json = JsonSerializer.Serialize(result);
            return Ok(json);
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
