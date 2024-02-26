using IISHF.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace IISHF.Core.Controllers.SurfaceControllers
{
    [Controller]
    public class EventsController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;

        public EventsController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services, AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery)
            : base(umbracoContextAccessor,
                databaseFactory,
                services,
                appCaches,
                profilingLogger,
                publishedUrlProvider)
        {
            _contentQuery = contentQuery;
        }

        [HttpGet]
        public IActionResult GetGroupRankings(int year, string titleEvent)
        {
            var teams = GetContent("team");
            var eventTeams = FilterData(year, titleEvent, teams);

            if (!eventTeams.Any())
            {
                // Handle not found situation
                return PartialView("~/Views/Partials/Events/EventStandings.cshtml", new GroupRankingsViewModel());
            }

            var teamPlacements = eventTeams.Where(x => !string.IsNullOrWhiteSpace(x.Value<string>("group"))).Select(placementItem => new RankingViewModel()
            {
                TeamName = placementItem.Value<string>("eventTeam"),
                Group = placementItem.Value<string>("group"),
                TeamLogoUrl = placementItem.Value<IPublishedContent>("image")?.Url() ?? string.Empty,
                Games = placementItem.Value<int>("games"),
                Wins = placementItem.Value<int>("wins"),
                Losses = placementItem.Value<int>("losses"),
                Ties = placementItem.Value<int>("tie"),
                GoalsFor = placementItem.Value<int>("goalsFor"),
                GoalsAgainst = placementItem.Value<int>("goalsAgainst"),
                Differnce = placementItem.Value<int>("difference"),
                TieWeight = placementItem.Value<decimal>("tieWeight"),
                Points = placementItem.Value<int>("points"),
                GroupPlacement = placementItem.Value<int>("groupPlacement"),
            }).ToList();

            var model = new GroupRankingsViewModel
            {
                Rankings = teamPlacements
            };

            return PartialView("~/Views/Partials/Events/EventStandings.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetSheduleAndResults(int year, string titleEvent)
        {
            var teams = GetContent("game");
            var schedule = FilterData(year, titleEvent, teams);

            var model = new ScheduleAndResultsViewModel();
            foreach (var game in schedule)
            {

                try
                {
                    var homeTeam =
                        game.Parent.Children.FirstOrDefault(x => x.Name == game.Value<string>("homeTeam")) ?? game.Parent.Children.FirstOrDefault(x => x.Name.Trim().Contains(game.Value<string>("homeTeam").Trim()));

                    var awayTeam =
                        game.Parent.Children.FirstOrDefault(x => x.Name == game.Value<string>("awayTeam")) ?? game.Parent.Children.FirstOrDefault(x => x.Name.Trim().Contains(game.Value<string>("awayTeam").Trim()));

                    var homeLogo = homeTeam.Value<IPublishedContent>("image")?.Url() ?? string.Empty;
                    var awayLogo = awayTeam.Value<IPublishedContent>("image")?.Url() ?? string.Empty;

                    var gameDateTime = game.Value<DateTime>("scheduleDateTime");

                    var remarks = game.Value<string>("remarks").Split("(").FirstOrDefault();

                    model.ScheduleAndResults.Add(new ScheduleAndResults()
                    {
                        HomeTeam = game.Value<string>("homeTeam"),
                        AwayTeam = game.Value<string>("awayTeam"),
                        HomeScore = game.Value<string>("homeScore"),
                        AwayScore = game.Value<string>("awayScore"),
                        GameNumber = game.Value<int>("gameNumber"),
                        GameDateTime = gameDateTime,
                        Group = game.Value<string>("group"),
                        Remarks = remarks,
                        HomeTeamLogoUrl = homeLogo,
                        AwayTeamLogoUrl = awayLogo,
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Game {game.Name} has data issues");
                }
            }

            return PartialView("~/Views/Partials/Events/SchedulAndResults.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetPlacements(int year, string titleEvent)
        {
            var teams = GetContent("team");
            var eventTeams = FilterData(year, titleEvent, teams);

            if (!eventTeams.Any())
            {
                // Handle not found situation
                return PartialView("~/Views/Partials/Events/EventPlacements.cshtml", new FinalPlacementsViewModel());
            }

            var teamPlacements = eventTeams.Select(placementItem => new TeamPlacement
            {
                Placement = placementItem.Value<int>("FinalRanking"),
                Iso3 = placementItem.Value<string>("countryIso3"),
                TeamName = placementItem.Value<string>("eventTeam"),
                TeamLogoUrl = placementItem.Value<IPublishedContent>("image")?.Url() ?? string.Empty,
                EventYear = year,
                TitleEvent = titleEvent
            }).ToList();

            var model = new FinalPlacementsViewModel
            {
                TeamPlacements = teamPlacements
            };

            return PartialView("~/Views/Partials/Events/EventPlacements.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetPlayerStats(int year, string titleEvent)
        {
            var content = GetContent("team");
            var teams = FilterData(year, titleEvent, content);

            if (!teams.Any())
            {
                return PartialView("~/Views/Partials/Events/PlayerStatistics.cshtml", new PlayerStatisticsViewModel());
            }

            var model = new PlayerStatisticsViewModel
            {
                PlayerStatistics = teams
                    .SelectMany(x => x.Children)
                    .Select(player => new PlayerStatistics
                    {
                        TeamName = player.Parent.Name,
                        PlayerName = player.Value<string>("playerName"),
                        Role = player.Value<string>("role"),
                        JerseyNumber = player.Value<int>("jerseyNumber"),
                        GamesPlayed = player.Value<int>("gamesPlayed"),
                        Goals = player.Value<int>("goals"),
                        Assists = player.Value<int>("assists"),
                        Penalties = player.Value<int>("penalties"),
                        TeamLogoUrl = player.Parent.Value<IPublishedContent>("image")?.Url() ?? string.Empty,
                    })
                    .ToList()
            };


            return PartialView("~/Views/Partials/Events/PlayerStatistics.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetEventInformation(int id)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournaments = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children().ToList();

            var cups = tournaments
                .FirstOrDefault(x => x.Name == "European Cups")
                .Children()
                .Select(x => x.Children.FirstOrDefault(x => x.Name == "2024"))
                .ToList();

            var championships = tournaments
                .FirstOrDefault(x => x.Name == "European Championships")
                .Children()
                .Select(x => x.Children.FirstOrDefault(x => x.Name == "2024"))
                .ToList();

            var nonTitle = tournaments
                .FirstOrDefault(x => x.Name == "None Title Events")
                .Children()
                .ToList();

            var allEvents = cups.Where(x => x != null).ToList()
                .Concat(championships.Where(x => x != null)
                    .Concat(nonTitle)
                    .ToList());



            var selectedEvent = allEvents.FirstOrDefault(x => x.Id == id);


            if (selectedEvent == null)
            {
                return PartialView("~/Views/Partials/Forms/ITC/EventInformation.cshtml", null);
            }

            var teams = selectedEvent.Children.Where(x => x.ContentType.Alias == "team").ToList();

            var model = new ITCEventInformationViewModel
            {
                EventName = selectedEvent.Parent.Name,
                AgeGroup = selectedEvent.Parent.Value<string>("AgeGroup") ?? selectedEvent.Value<string>("AgeGroup"),
                ShortCode = selectedEvent.Parent.Value<string>("EventShotCode"),
                EvenLocation = selectedEvent.Value<string>("venueAddress"),
                EventStartDate = selectedEvent.Value<DateTime>("eventStartDate"),
                EventEndDate = selectedEvent.Value<DateTime>("eventEndDate"),
                HostingClub = selectedEvent.Value<string>("hostClub"),
                HostingCountry = selectedEvent.Value<string>("hostCountry"),
                SanctionNumber = selectedEvent.Value<string>("sanctionNumber"),
                IsChampionship = false,
                Teams = teams.Select(x => x.Name).ToList()
            };

            return PartialView("~/Views/Partials/Forms/ITC/EventInformation.cshtml", model);

        }

        private static List<IPublishedContent> FilterData(int year, string titleEvent, List<IPublishedContent> rootContent)
        {
            var eventTeams = rootContent.Where(x =>
                    x.Parent != null &&
                    x.Parent.Name == year.ToString() &&
                    x.Parent.Parent != null &&
                    x.Parent.Parent.Value<string>("EventShotCode") == titleEvent)
                .ToList();
            return eventTeams;
        }

        private List<IPublishedContent> GetContent(string type)
        {
            var teams = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(type)
                .ToList();
            return teams;
        }
    }
}
