using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SendGrid.Helpers.Errors.Model;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static PdfSharpCore.Pdf.PdfDictionary;
using File = System.IO.File;
using IMediaService = Umbraco.Cms.Core.Services.IMediaService;

namespace IISHFTest.Core.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDocumentService _documentService;
        private readonly IMediaService _umbracoMediaService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            IHttpContextAccessor httpContextAccessor,
            IDocumentService documentService,
            IMediaService umbracoMediaService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TournamentService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
            _httpContextAccessor = httpContextAccessor;
            _documentService = documentService;
            _umbracoMediaService = umbracoMediaService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public int? CreateEvent(TournamentModel model)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournament = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children()?
                .FirstOrDefault(x => x.Name.ToLower().Contains(model.IsChampionships ? "championships" : "cup"))!
                .Children()
                .FirstOrDefault(x => x.Value<string>("EventShotCode") == model.TitleEvent);

            if (tournament == null)
            {
                return null;
            }

            var linkObject = new
            {
                name = $"{model.HostClub} Website",
                url = model.HostWebsite,
                target = "_blank",
            };

            var linkArray = new[] { linkObject };
            var jsonLinkArray = JsonSerializer.Serialize(linkArray);

            var iishfEvent = _contentService.Create(model.EventYear.ToString(), tournament.Id, "event");
            iishfEvent?.SetValue("eventStartDate", model.EventStartDate);
            iishfEvent?.SetValue("eventEndDate", model.EventEndDate);
            iishfEvent?.SetValue("hostClub", model.HostClub);
            iishfEvent?.SetValue("hostContact", model.HostContact);
            iishfEvent?.SetValue("hostPhoneNumber", model.HostPhoneNumber);
            iishfEvent?.SetValue("hostEmail", model.HostEmail);
            iishfEvent?.SetValue("hostWebSite", jsonLinkArray);
            iishfEvent?.SetValue("venueName", model.VenueName);
            iishfEvent?.SetValue("venueAddress", model.VenueAddress);
            iishfEvent?.SetValue("rinkSizeLength", model.RinkLength);
            iishfEvent?.SetValue("rinkSizeWidth", model.RinkWidth);
            iishfEvent?.SetValue("rinkFloor", model.RinkFloor);

            _contentService.SaveAndPublish(iishfEvent);

            return iishfEvent.Id;
        }

        public IPublishedContent? GetTournament(bool isChampionships, string titleEvent, string eventYear)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournaments = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children()?
                .FirstOrDefault(x => x.Name.ToLower().Contains(isChampionships ? "championships" : "cup"))!.Children();

            return tournaments == null || !tournaments.Any()
                ? null
                : tournaments.FirstOrDefault(x => x.Value<string>("EventShotCode") == titleEvent).Children()
                    .FirstOrDefault(x => x.Name == eventYear) ?? null;
        }

        public IPublishedContent? GetTournamentTeamByName(string teamName, IPublishedContent tournament)
        {
            var team = tournament.Children().FirstOrDefault(x => x.Name == teamName);
            return team;
        }

        public Task UpdateGameWithResults(UpdateTeamScores model, IPublishedContent tournament)
        {
            foreach (var finalScore in model.Scores)
            {
                var game = tournament.Children.FirstOrDefault(x => x.Name == finalScore.GameNumber.ToString() && x.ContentType.Alias == "game");
                if (game == null)
                {
                    var exception = new NullReferenceException($"Game number {finalScore.GameNumber} not found");
                    _logger.LogError(exception, "Unable to find game number {gameId}", finalScore.GameNumber);
                    throw exception;
                }

                var gameToUpdate = _contentService.GetById(game.Id);

                gameToUpdate?.SetValue("homeScore", finalScore.HomeScore);
                gameToUpdate?.SetValue("awayScore", finalScore.AwayScore);
                _contentService.SaveAndPublish(gameToUpdate);
            }

            return Task.CompletedTask;
        }

        public Task CreateEventGame(CreateScheduleGames model, IPublishedContent tournament)
        {
            foreach (var scheduledGame in model.Games)
            {
                var game = _contentService.Create(scheduledGame.GameNumber.ToString(), tournament.Id, "game");
                game?.SetValue("homeTeam", scheduledGame.HomeTeam);
                game?.SetValue("awayTeam", scheduledGame.AwayTeam);
                game?.SetValue("gameNumber", scheduledGame.GameNumber);
                game?.SetValue("scheduleDateTime", scheduledGame.GameDateTime);
                game?.SetValue("group", scheduledGame.Group);
                game?.SetValue("remarks", scheduledGame.Remarks);
                _contentService.SaveAndPublish(game);
            }

            return Task.CompletedTask;
        }

        public IContent CreateEventTeam(Team model, IPublishedContent tournament)
        {
            var team = _contentService.Create(model.TeamName, tournament.Id, "team");
            team?.SetValue("eventTeam", model.TeamName);
            team?.SetValue("countryIso3", model.CountryIso3);
            team?.SetValue("group", model.Group);

            // Assuming you have a URL and a name for the link
            if (model.TeamUrl != null)
            {
                var linkObject = new
                {
                    name = $"{model.TeamName} Website",
                    url = model.TeamUrl,
                    target = "_blank",
                };

                var linkArray = new[] { linkObject };
                var jsonLinkArray = JsonSerializer.Serialize(linkArray);

                team.SetValue("teamWebsite", jsonLinkArray);
            }

            _contentService.SaveAndPublish(team);

            return team;
        }

        public async Task UpdateTeamColours(string colourHex, string fieldName, IPublishedContent team)
        {
            await Task.Run(() =>
            {
                if (team.Value<string>(fieldName) == colourHex)
                {
                    return Task.FromResult(Task.CompletedTask);
                }
                var teamToUpdate = _contentService.GetById(team.Id);
                teamToUpdate?.SetValue(fieldName, colourHex);
                return Task.CompletedTask;
            });
        }

        public async Task SetSubmissionDate(IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var tournamentTeam = _contentService.GetById(team.Id);
            if (tournamentTeam == null)
            {
                return;
            }

            var udi = Udi.Create(Constants.UdiEntityType.Member, user.Key);

            tournamentTeam.SetValue("teamInformationSubmissionDate", DateTime.UtcNow);
            tournamentTeam.SetValue("teamInformationSubmitted", true);
            tournamentTeam.SetValue("teamInformationSubmittedBy", udi);
            _contentService.SaveAndPublish(tournamentTeam);
        }

        public async Task SubmitTeamInformationToHost(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team)
        {
            // Logos from nma team - done
            // Photo from nma team - done
            // Sponsor from tournament team - done
            // Roster from tournament team - dine
            // Team write up from nma team - done
            // Age group from tournament - done
            // Host information from tournament - done

            var protocol = _httpContextAccessor.HttpContext.Request.Scheme;
            var baseUrl = _httpContextAccessor.HttpContext.Request.Host;

            var sponsorList = nmaTeam.Children.Where(x => x.ContentType.Alias == "sponsor")
                .Select(x => x.Value<IPublishedContent>("sponsorImage").Url().ToString()).ToList();

            var teamPhotoUrl = nmaTeam.Value<IPublishedContent>("teamPhoto");
            var teamPhotoMediaItem = GetImageByteArray(teamPhotoUrl.Id, teamPhotoUrl.Url());
            var teamLogoUrl = nmaTeam.Value<IPublishedContent>("teamLogo");
            var teamLogoMediaItem = GetImageByteArray(teamLogoUrl.Id, teamLogoUrl.Url());

            var logos = _umbracoMediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Logos");

            var iishfLogo = _umbracoMediaService.GetPagedChildren(logos.Id, 0, int.MaxValue, out var totalChildren).FirstOrDefault(x => x.Name == "IISHF");

            var value = iishfLogo.Properties[0].Values
                .FirstOrDefault(v => !string.IsNullOrEmpty(v.EditedValue.ToString()) &&
                                     v.EditedValue.ToString().Contains("\"src\"") &&
                                     v.EditedValue.ToString().Contains("iishf"));
            var src = string.Empty;
            if (value != null)
            {
                var jsonDocument = JsonDocument.Parse(value.EditedValue.ToString());
                src = jsonDocument.RootElement.GetProperty("src").GetString();
                // Use the src value as needed
            }

            var iishfLogoBytes = GetImageByteArray(iishfLogo.Id, src);

            var teamWriteUp = nmaTeam.Value<string>("teamHistory");
            ;

            var roster = team.Children().Where(x => x.ContentType.Alias == "roster").ToList();

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

            var rosteredMembers = sortedRoster.Select(x => new RosterMember()
            {
                Id = x.Id,
                Role = x.Value<string>("role"),
                FirstName = x.Value<string>("firstName"),
                LastName = x.Value<string>("lastName"),
                JerseyNumber = x.Value<int?>("jerseyNumber"),
                IsBenchOfficial = x.Value<bool>("isBenchOfficial")
            }).ToList();

            var ageGroup = tournament.Parent.Value<string>("AgeGroup");

            var sanction = tournament.Value<string>("eventReference");

            var teamInformationSubmissionDate = team.Value<DateTime>("teamInformationSubmissionDate");
            var teamInformationSubmittedBy = team.Value<IPublishedContent>("teamInformationSubmittedBy");
            var submittedByMember = _memberService.GetById(teamInformationSubmittedBy.Id);

            var hostName = tournament.Value<string>("hostContact");
            var hostEmail = tournament.Value<string>("hostEmail");

            var submittedTeamInformationModel = new SubmittedTeamInformationModel
            {
                Id = team.Id,
                EventReference = sanction,
                AgeGroup = ageGroup,
                Roster = rosteredMembers, // need to map the roster
                TeamName = team.Name,
                TeamWriteUp = teamWriteUp,
                TeamPhoto = teamPhotoMediaItem,
                TeamLogo = teamLogoMediaItem,
                SubmittedBy = teamInformationSubmittedBy.Name,
                SubmittedDateTime = teamInformationSubmissionDate,
                SubmittedByEmail = submittedByMember.Email
            };


            // ToDo - get IISHF logo and add to GeneratePdfToMemoryStreamAsync
            var document = await _documentService.GeneratePdfToMemoryStreamAsync(submittedTeamInformationModel, iishfLogoBytes);
            using FileStream file = new FileStream(@$"c:\temp\teamInfo-{team.Name}-{ageGroup}.pdf",
                FileMode.Create, FileAccess.Write);
            document.WriteTo(file);
        }

        private List<IPublishedContent> GetEventTeams(int year, int tournamentId, int teamId)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournaments = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children().ToList();

            var cups = tournaments
                .FirstOrDefault(x => x.Name == "European Cups")
                .Children()
                .Select(x => x.Children.FirstOrDefault(x => x.Name == year.ToString()))
                .ToList();

            var championships = tournaments
                .FirstOrDefault(x => x.Name == "European Championships")
                .Children()
                .Select(x => x.Children.FirstOrDefault(x => x.Name == year.ToString()))
                .ToList();

            var selectedEvent = cups.Where(x => x != null).ToList().Concat(championships.Where(x => x != null).ToList())
                .FirstOrDefault(x => x.Id == tournamentId);

            if (selectedEvent == null)
            {
                return new List<IPublishedContent>();
            }

            var team = selectedEvent.Children.Where(x => x.ContentType.Alias == "team").ToList();

            return team;
        }

        public byte[] GetImageByteArray(int mediaId, string filePath)
        {
            // Retrieve the media item
            var mediaItem = _umbracoMediaService.GetById(mediaId);
            if (mediaItem == null) return null;
           
            // Combine with the root path if it's stored locally
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));

            // Read the file into a byte array
            return System.IO.File.Exists(fullPath) ? System.IO.File.ReadAllBytes(fullPath) : null;
        }

    }
}
