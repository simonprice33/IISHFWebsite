using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class EventResultsService : IEventResultsService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly ILogger<EventResultsService> _logger;
        private readonly ITournamentService _tournamentService;

        public EventResultsService(IPublishedContentQuery contentQuery,
            IContentService contentService,
            ILogger<EventResultsService> logger,
            ITournamentService tournamentService)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _logger = logger;
            _tournamentService = tournamentService;
        }


        public void UpdatePlayerStatistics(UpdatePlayerStatistics model, IPublishedContent tournament)
        {
            foreach (var player in model.PlayerStatistics)
            {
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == player.TeamName && x.ContentType.Alias == "team");
                if (selectedTeam == null)
                {
                    continue;
                }

                var rosterMember = selectedTeam.Children()
                    .FirstOrDefault(x => x.Value<string>("licenseNumber") == player.License);

                if (rosterMember == null)
                {
                    continue;
                }

                var rosteredMember = _contentService.GetById(rosterMember.Id);
                rosteredMember?.SetValue("goals", player.Goals);
                rosteredMember?.SetValue("assists", player.Assists);
                rosteredMember?.SetValue("penalties", player.Penalties);
                rosteredMember?.SetValue("gamesPlayed", player.GamesPlayed);
                _contentService.SaveAndPublish(rosteredMember);
            }
        }

        public void UpdateGroupRanking(Rankings model, IPublishedContent tournament)
        {
            foreach (var team in model.Ranking)
            {
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == team.TeamName && x.ContentType.Alias == "team");
                if (selectedTeam == null)
                {
                    continue;
                }

                var teamToUpdate = _contentService.GetById(selectedTeam.Id);

                teamToUpdate?.SetValue("games", team.Games);
                teamToUpdate?.SetValue("groupPlacement", team.Place);
                teamToUpdate?.SetValue("wins", team.Won);
                teamToUpdate?.SetValue("tie", team.Tied);
                teamToUpdate?.SetValue("losses", team.Lost);
                teamToUpdate?.SetValue("goalsFor", team.GoalsFor);
                teamToUpdate?.SetValue("goalsAgainst", team.GoalsAgainst);
                teamToUpdate?.SetValue("difference", team.Diff);

                if (team.TieWeight != null)
                {
                    teamToUpdate?.SetValue("tiedWeight", team.TieWeight);
                }

                teamToUpdate?.SetValue("points", team.Points);
                _contentService.SaveAndPublish(teamToUpdate);
            }
        }

        public void UpdateFinalPlacement(TeamPlacements model, IPublishedContent tournament)
        {
            foreach (var placement in model.Placements)
            {
                // look at using item from tournament service
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == placement.TeamName && x.ContentType.Alias == "team");
                if (selectedTeam == null)
                {
                    continue;
                }

                var teamToUpdate = _contentService.GetById(selectedTeam.Id);

                teamToUpdate?.SetValue("finalRanking", placement.Placement);
                _contentService.SaveAndPublish(teamToUpdate);
            }
        }
    }
}
