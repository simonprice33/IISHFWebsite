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

        [HttpPut("Placement")]
        //[ApiKeyAuthorize]
        public IActionResult PostPlacement([FromBody] TeamPlacements model)
        {
            var tournament = GetTournament(model.IsChampionships, model.TitleEvent, string.Format(model.EventYear.ToString()));
            if (tournament == null)
            {
                return NotFound();
            }

            var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == model.TeamName && x.ContentType.Alias == "team");
            if (selectedTeam == null)
            {
                return NotFound();
            }

            var teamToUpdate = _contentService.GetById(selectedTeam.Id);

            teamToUpdate?.SetValue("finalRanking", model.Placement);
            _contentService.SaveAndPublish(teamToUpdate);

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
