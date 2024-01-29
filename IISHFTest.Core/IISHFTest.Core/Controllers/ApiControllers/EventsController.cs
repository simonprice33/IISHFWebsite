using System.Text.Json;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Lucene.Net.Index;
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
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            ITournamentService tournamentService,
            IRosterService rosterService,
            IEventResultsService eventResultsService,
            IMediaService mediaService,
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
            _logger = logger;
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

            var team = _tournamentService.GetTournamentTeam(model.TeamName, tournament);

            if (team == null)
            {
                return NotFound();
            }

            _rosterService.SetRosterForTeamInformation(model, team);

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
            var member = await _memberManager.GetCurrentMemberAsync();

            return NoContent();
        }
    }
}
