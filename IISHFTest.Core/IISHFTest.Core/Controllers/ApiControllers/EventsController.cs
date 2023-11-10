using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace IISHFTest.Core.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;

        public EventsController(IPublishedContentQuery contentQuery, IContentService contentService)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
        }

        [HttpPut("Ranking")]
        //[ApiKeyAuthorize]
        public IActionResult PutRanking([FromBody] Rankings model)
        {
            var tournament = GetTournament(
                model.Ranking.FirstOrDefault()!.IsChampionships,
                model.Ranking.FirstOrDefault()!.TitleEvent,
                string.Format(model.Ranking.FirstOrDefault()!.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            foreach (var team in model.Ranking)
            {
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == team.TeamName && x.ContentType.Alias == "team");
                if (selectedTeam == null)
                {
                    return NotFound();
                }

                var teamToUpdate = _contentService.GetById(selectedTeam.Id);

                teamToUpdate?.SetValue("games", team.Games);
                teamToUpdate?.SetValue("wins", team.Wins);
                teamToUpdate?.SetValue("ties", team.Ties);
                teamToUpdate?.SetValue("losses", team.Losses);
                teamToUpdate?.SetValue("goalsFor", team.GoalsFor);
                teamToUpdate?.SetValue("goalsAgainst", team.GoalsAgainst);
                teamToUpdate?.SetValue("difference", team.Differnce);
                teamToUpdate?.SetValue("tieWeight", team.TieWeight);
                _contentService.SaveAndPublish(teamToUpdate);
            }

            return NoContent();
        }

        [HttpPut("Placement")]
        //[ApiKeyAuthorize]
        public IActionResult PutPlacement([FromBody] TeamPlacements model)
        {
            var tournament = GetTournament(
                model.Placements.FirstOrDefault()!.IsChampionships, 
                model.Placements.FirstOrDefault()!.TitleEvent, 
                string.Format(model.Placements.FirstOrDefault()!.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            foreach (var placement in model.Placements)
            {
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == placement.TeamName && x.ContentType.Alias == "team");
                if (selectedTeam == null)
                {
                    return NotFound();
                }

                var teamToUpdate = _contentService.GetById(selectedTeam.Id);

                teamToUpdate?.SetValue("finalRanking", placement.Placement);
                _contentService.SaveAndPublish(teamToUpdate);
            }

            return NoContent();
        }

        [HttpPut("ScheduleGame")]
        //[ApiKeyAuthorize]
        public IActionResult PutScheduleGame([FromBody] UpdateTeamScores model)
        {
            var tournament = GetTournament(
                model.Scores.FirstOrDefault()!.IsChampionships, 
                model.Scores.FirstOrDefault()!.TitleEvent, 
                string.Format(model.Scores.FirstOrDefault()!.EventYear.ToString()));
            if (tournament == null)
            {
                return NotFound();
            }

            foreach (var finalScore in model.Scores)
            {
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == finalScore.GameNumber.ToString() && x.ContentType.Alias == "game");
                if (selectedTeam == null)
                {
                    return NotFound();
                }

                var gameToUpdate = _contentService.GetById(selectedTeam.Id);

                gameToUpdate?.SetValue("homeScore", finalScore.HomeScore);
                gameToUpdate?.SetValue("awayScore", finalScore.AwayScore);
                _contentService.SaveAndPublish(gameToUpdate);
            }

            return NoContent();
        }

        [HttpPost("ScheduleGame")]
        //[ApiKeyAuthorize]
        public IActionResult PostScheduleGame([FromBody] CreateScheduleGames model)
        {
            var tournament = GetTournament(
                model.Games.FirstOrDefault()!.IsChampionships, 
                model.Games.FirstOrDefault()!.TitleEvent, 
                string.Format(model.Games.FirstOrDefault()!.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            if (tournament != null)
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
            }

            return NoContent();
        }

        private IPublishedContent? GetTournament(bool isChampionships, string titleEvent, string eventYear)
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
    }
}
