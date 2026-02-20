using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Math;
using IISHF.Core.Hubs;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using IISHF.Core.Models.ServiceBusMessage;
using Lucene.Net.Index;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly INMAService _nmaService;
        private readonly ILogger<EventsController> _logger;
        private readonly IHubContext<DataHub> _hubContext;
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
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            INMAService nmaService,
            ILogger<EventsController> logger,
            IHubContext<DataHub> hubContext)
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
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _nmaService = nmaService;
            _logger = logger;
            _hubContext = hubContext;

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

        [HttpPut("Group")]
        public async Task<IActionResult> GroupTeams([FromBody]GroupInformation model )
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            if (tournament == null)
            {
                return NotFound();
            }

            await _tournamentService.SetTeamsInGroup(model, tournament);

            return Ok();
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
        public async Task<IActionResult> PutStatistics([FromBody] UpdatePlayerStatistics model)
        {
            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            await _eventResultsService.UpdatePlayerStatistics(model, tournament);
            await _hubContext.Clients.All.SendAsync("UpdatePlayerStats", model.EventYear, model.TitleEvent);

            return NoContent();
        }

        [HttpPut("Ranking")]
        //[ApiKeyAuthorize]
        public async Task<IActionResult> PutRanking([FromBody] Rankings model)
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
            await _hubContext.Clients.All.SendAsync("UpdateGroupRanking", model.EventYear, model.TitleEvent);

            return NoContent();
        }

        [HttpPut("Placement")]
        //[ApiKeyAuthorize]
        public async Task<IActionResult> PutPlacement([FromBody] TeamPlacements model)
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
            await _hubContext.Clients.All.SendAsync("UpdateFinalPlacement", model.EventYear, model.TitleEvent);


            return NoContent();
        }

        [HttpPut("ScheduleGame")]
        //[ApiKeyAuthorize]
        public async Task<IActionResult> PutScheduleGame([FromBody] UpdateTeamScores model)
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
                await _tournamentService.UpdateGameWithResults(model, tournament);
                foreach (var score in model.Scores)
                {
                    await _hubContext.Clients.All.SendAsync("UpdateScores", score.GameNumber, score.HomeScore, score.AwayScore);
                }
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

        [HttpPut("ScheduleGame/placement")]
        //[ApiKeyAuthorize]
        public async Task<IActionResult> PutSchedulePlacementGame([FromBody] CreateScheduleGames model)
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
                await _tournamentService.UpdateEventGame(model, tournament);

                var outModel = await _tournamentService.GetScheduleAndResults(model.EventYear, model.TitleEvent, 0, 0, false);

                var gameNumbers = model.Games.Select(x => x.GameNumber).ToList();

                outModel.ScheduleAndResults = outModel.ScheduleAndResults
                    .Where(x => gameNumbers.Contains(x.GameNumber))
                    .ToList();

                await _hubContext.Clients.All.SendAsync("UpdateSelectedGamesWithTeams", outModel);
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

        [HttpPut("Gamesheet")]
        public async Task<IActionResult> PutGameSheetLetters([FromForm] GameSheets model)
        {

            var file = model.GamesSheet;
            if (file.Length == 0)
            {
                return BadRequest("File not valid");
            }

            var tournament = _tournamentService.GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            var game = tournament.Children.FirstOrDefault(x => x.Name == model.GameNumber.ToString() && x.ContentType.Alias == "game");

            if (game == null)
            {
                return NotFound();
            }

            await _tournamentService.AddGameSheetToGame(model.GamesSheet, model.GameNumber.ToString(), game);
            await _hubContext.Clients.All.SendAsync("UpdateGamesWithTeams", model.EventYear, model.TitleEvent);


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
        [HttpPut("itc/submit")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> PutItcRoster([FromBody] TeamInformationSubmission model)
        {
            var tournament = _tournamentService.GetTournament(model.EventId);

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamByName(model.TeamName, tournament);

            if (team == null && tournament.Value<string>("sanctionNUmber").StartsWith('A'))
            {
                return NotFound();

            }

            // Not specifically looking for B events as this could expand in the future
            if (team == null && !tournament.Value<string>("sanctionNUmber").StartsWith('A'))
            {
                var eventTeamModel = new Team
                {
                    TeamName = model.TeamName,
                    EventId = model.EventId,
                    CountryIso3 = string.Empty, // Need to get this from the form.
                    EventYear = model.EventYear,
                    IsChampionships = false,
                    TitleEvent = string.Empty, // Not a title event,
                    Group = string.Empty, // Not in our control and not known at the point of data entry
                    TeamUrl = null, // can try and get this from submitted information
                };

                var eventTeam = _tournamentService.CreateEventTeam(eventTeamModel, tournament);

                // Get updated tournament information
                tournament = _tournamentService.GetTournament(model.EventId);
                team = _tournamentService.GetTournamentTeamByName(eventTeam.PublishName, tournament);


                if (team == null)
                {
                    return NotFound();
                }

                await _tournamentService.SetSelectTeamCreator(team);

            }

            // update jersey colours for event team
            await _tournamentService.UpdateTeamProperties(string.IsNullOrWhiteSpace(model.JerseyOne) ? "#000000" : model.JerseyOne, "jerseyOneColour", team);
            await _tournamentService.UpdateTeamProperties(string.IsNullOrWhiteSpace(model.JerseyTwo) ? "#ffffff" : model.JerseyTwo, "jerseyTwoColour", team);

            await _tournamentService.UpdateTeamProperties(model.TeamSignatory, "teamSignatory", team);
            await _tournamentService.UpdateTeamProperties(model.IssuingCountry, "countryIso3", team);

            var result = await _rosterService.UpsertRosterMembers(model, team);

            var responseModel = new ItcModel()
            {
                ItcRosterMembers = result.ItcRosterMembers,
                JerseyOneColour = model.JerseyOne,
                JerseyTwoColour = model.JerseyTwo,
                TeamId = team.Id
            };

            if (model.SubmitToHost)
            {
                await _tournamentService.SetTeamItcSubmissionDateFromTeam(team);
                var nmaTeam = _contentQuery.Content(team.Value<Guid>("nMATeamKey"));
                team = _contentQuery.Content(team.Id);
                await _tournamentService.NotifyNmaApprover(tournament, nmaTeam, team);
            }

            var json = JsonSerializer.Serialize(responseModel);
            return Ok(json);
        }

        [HttpPut("itc/unsubmit/tournament/{tournamentId}/team/{teamKey}")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> UnsubmitItc(int tournamentId, Guid teamKey)
        {
            var tournament = _tournamentService.GetTournament(tournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamByKey(teamKey, tournament);

            if (team == null && tournament.Value<string>("sanctionNUmber").StartsWith('A'))
            {
                return NotFound();

            }

            await _tournamentService.UnsubmitItc(team);
            await _tournamentService.CopyItc(team);

            return NoContent();
        }

        [HttpPut("itc/approve/{approver}/tournament/{tournamentId}/team/{teamName}")]
        [HttpPut("itc/approve/tournament/{tournamentId}/team/{teamName}")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> ItcApprove(int tournamentId, string teamName, [FromBody] ITCRosterMemberpproval model, string approver = "")
        {
            var setAsApproved = model.RosterApprovals.All(x => x.Approved);
            var isNma = approver.ToLower() == "nma";
            var isIISHF = approver.ToLower() == "iishf";

            if (!string.IsNullOrWhiteSpace(approver) && !setAsApproved)
            {
                return BadRequest("Not all roster members approved");
            }

            var tournament = _tournamentService.GetTournament(tournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamByName(teamName, tournament);

            if (team == null)
            {
                return NotFound();
            }

            await _tournamentService.SetNmaCheckValue(model.RosterApprovals, isNma);

            if (isNma && setAsApproved)
            {
                await _tournamentService.SetTeamItcNmaApprovalDate(team);
                return NoContent();
            }

            if (isIISHF && setAsApproved)
            {
                await _tournamentService.SetTeamItcIISHFApprovalDate(team);

                var pdfFileName =
                    $"ITC_${tournament.Parent.Name}_{team.Name}_{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.pdf";
                var excelFileName =
                    $"ITC_${tournament.Parent.Name}_{team.Name}_{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.pdf";

                var itcExcel = await _tournamentService.GenerateItcAsExcelFile(team, tournament);
                await _emailService.SendItc("thf@iishf.com", new List<string>(), "THF Manager", "IISHF Event Name", "ITCApprovedInternalInternalTemplate.html",
                    $"{tournament.Name} ITC Approved - {team.Name}", team.Name, itcExcel, excelFileName);

                var excelStream = new MemoryStream(itcExcel);
                await _teamService.AddItcToTeam(excelStream, $"ITC_${tournament.Parent.Name}_{team.Name}_{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.xlsx", team);


                var itcPdf = _tournamentService.GenerateItcAsPdfFile(itcExcel);
                var pfdStream = new MemoryStream(itcPdf);

                await _teamService.AddItcToTeam(pfdStream, pdfFileName, team);

                var nmaKey = team.Value<string>("nmaKey");
                var itcApprovers = await _nmaService.GetNMAITCApprovers(Guid.Parse(nmaKey));

                var cc = new List<string>()
                {
                    "itc@iishf.com"
                };

                cc.AddRange(itcApprovers.Select(itcApprover => itcApprover.NmaApproverEmail));

                await _emailService.SendItc(tournament.Value<string>("hostEmail"), cc, tournament.Value<string>("hostContact"), "IISHF Event Name", "ITCApprovedSendToHost.html",
                    $"{tournament.Name} ITC Approved - {team.Name}", team.Name, itcPdf, pdfFileName);

                return NoContent();
            }

            await _tournamentService.ResetNmaApproval(team);
            await _tournamentService.SetItcRejectionReason(team);

            return NoContent();
        }

        [HttpPost("itc/guest-permission-letters/tournament/{tournamentId}/team/{teamId}")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> PostPersmisssionLetters([FromRoute] int tournamentId, [FromRoute] int teamId, [FromForm] IFormCollection formCollection)
        {

            var files = formCollection.Files;
            var lables = formCollection.Keys.ToList();

            var tournament = _tournamentService.GetTournament(tournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var team = _tournamentService.GetTournamentTeamById(teamId, tournament);

            if (team == null)
            {
                return NotFound();
            }

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var label = lables[i];
                var content = await _teamService.AddPlayerPermissionDocumentToTeam(file, label, team);
            }

            return NoContent();
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
            await _tournamentService.UpdateTeamProperties(model.JerseyOne, "jerseyOneColour", team);
            await _tournamentService.UpdateTeamProperties(model.JerseyTwo, "jerseyTwoColour", team);

            // Re-get nma team object and return saved \ published information
            nmaTeam = _contentQuery.Content(team.Value<Guid>("nMATeamKey"));
            var sponsorList = nmaTeam.Children.Where(x => x.ContentType.Alias == "sponsor")
                .Select(x => new SponsorImages
                {
                    Id = x.Id,
                    MediaId = x.Value<IPublishedContent>("sponsorImage").Id,
                    path = x.Value<IPublishedContent>("sponsorImage").Url()
                }).ToList();

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
                await _tournamentService.SetTeamInformationSubmissionDate(team);

                team = _contentQuery.Content(team.Id);
                await _tournamentService.SubmitTeamInformationToHost(tournament, nmaTeam, team);
            }

            return Ok(responseModel);
        }

        [HttpDelete]
        [Route("itc/guest-permission-letters/document/{contentId}/media/{mediaId}")]
        public async Task<IActionResult> DeletePermissionDocument(int contentId, int mediaId)
        {
            await _teamService.DeleteMedia(contentId, mediaId);

            return NoContent();
        }


        [HttpDelete]
        [Route("itc/team/title-event/{titleEvent}/championship/{isChampionship}/year/{eventYear}/team/{teamName}/roster-member/{rosterId}/")]
        [Route("team-submission/team/title-event/{titleEvent}/championship/{isChampionship}/year/{eventYear}/team/{teamName}/roster-member/{rosterId}/")]
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


        [HttpDelete]
        [Route("itc/team/tournament/{tournamentId}/team/{teamName}/roster-member/{rosterId}/")]
        public async Task<IActionResult> DeleteFromRoster(int tournamentId, string teamName, int rosterId)
        {
            // Check we have the tournament
            var tournament = _tournamentService.GetTournament(
                tournamentId);

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

        [HttpDelete]
        [Route("team-submission/team/title-event/{titleEvent}/championship/{isChampionship}/year/{eventYear}/team/{teamName}/sponsor/{sponsorId}/media/{mediaId}")]
        public async Task<IActionResult> DeleteSponsorImage(string titleEvent, bool isChampionship, int eventYear, string teamName, int sponsorId, int mediaId)
        {

            // ToDo : Refactor tournament and team out and refine the url
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
            await _teamService.DeleteMedia(sponsorId, mediaId);

            return NoContent();
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> Search(string searchText = "", string ageGroup = "")
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                var json = new
                {
                    Id = 0,
                    Key = Guid.Empty,
                    Name = "Event Selection required"
                };

                return Ok(new List<object> { json });
            }

            var teams = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("clubTeam")
                .Where(x => x.Value<string>("ageGroup") == ageGroup && x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new
                {
                    Id = x.Id,
                    Key = x.Key,
                    Name = x.Name,
                    Country = x.Parent.Parent.Parent.Parent.Value<string>("iSO3")
                })
                .ToList();
            _logger.LogInformation(searchText);
            return Ok(teams);
        }

        [HttpPost]
        [Route("tournament/{tournamentId}/nma-team/{teamId}/tournament-team/{tournamentTeamId}")]
        public async Task<IActionResult> LinkTeamToTournamentTeam([FromRoute] int tournamentId, [FromRoute] int teamId, [FromRoute] int tournamentTeamId, [FromBody] TournamentBaseModel model)
        {
            var tournament = _tournamentService.GetTournament(tournamentId);
            if (tournament == null)
            {
                return NotFound("Tournament not found");
            }

            try
            {
                await _tournamentService.LinkNmaTeamToTournamentTeam(tournament, teamId, tournamentTeamId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Ok();
        }
    }
}
