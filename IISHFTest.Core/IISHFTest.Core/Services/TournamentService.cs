using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace IISHFTest.Core.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            ILogger<TournamentService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
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
    }
}
