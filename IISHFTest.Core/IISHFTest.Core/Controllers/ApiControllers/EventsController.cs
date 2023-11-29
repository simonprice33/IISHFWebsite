using System.Text.Json;
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

        [HttpPost("Team")]
        public IActionResult PostTeam([FromBody] Team model)
        {
            var content = GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            var team = _contentService.Create(model.TeamName, content.Id, "team");
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
            return Ok();
        }

        [HttpPost("Roster")]
        public IActionResult PostRosterMember([FromBody] RosterMembers model)
        {
            var content = GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                model.EventYear.ToString());

            var team = content.Children().FirstOrDefault(x => x.Name == model.TeamName);

            if (team == null)
            {
                return NotFound();
            }

            foreach (var rosterMember in model.ItcRosterMembers)
            {
                var rosteredMember = _contentService.Create(rosterMember.PlayerName, team.Id, "roster");
                rosteredMember?.SetValue("playerName", rosterMember.PlayerName);
                rosteredMember?.SetValue("licenseNumber", rosterMember.License);
                rosteredMember?.SetValue("isBenchOfficial", rosterMember.IsBenchOfficial);
                rosteredMember?.SetValue("role", rosterMember.Role);
                rosteredMember?.SetValue("jerseyNumber", rosterMember.JerseyNumber);
                rosteredMember?.SetValue("dateOfBirth", rosterMember.DateOfBirth.ToString("yyyy-MM-dd"));
                rosteredMember?.SetValue("nmaCheck", rosterMember.NmaCheck);
                rosteredMember?.SetValue("iishfCheck", rosterMember.IISHFCheck);
                rosteredMember?.SetValue("comments", rosterMember.Comments);

                _contentService.SaveAndPublish(rosteredMember);
            }

            return Ok();
        }

        [HttpPut("Statistics")]
        public IActionResult PutStatistics([FromBody] UpdatePlayerStatistics model)
        {
            var tournament = GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

            foreach (var player in model.PlayerStatistics)
            {
                var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == player.TeamName && x.ContentType.Alias == "team");
                if (selectedTeam == null)
                {
                    continue;
                }

                var rosterMember = selectedTeam.Children()
                    .FirstOrDefault(x => x.Value<string>("licenseNumber") == player.License);

                var rosteredMember = _contentService.GetById(rosterMember.Id);
                rosteredMember?.SetValue("goals", player.Goals);
                rosteredMember?.SetValue("assists", player.Assists);
                rosteredMember?.SetValue("penalties", player.Penalties);
                rosteredMember?.SetValue("gamesPlayed", player.GamesPlayed);
                _contentService.SaveAndPublish(rosteredMember);

            }

            return NoContent();
        }

        [HttpPut("Ranking")]
        //[ApiKeyAuthorize]
        public IActionResult PutRanking([FromBody] Rankings model)
        {
            var tournament = GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

            if (tournament == null)
            {
                return NotFound();
            }

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
                teamToUpdate?.SetValue("wins", team.won);
                teamToUpdate?.SetValue("tie", team.Tied);
                teamToUpdate?.SetValue("losses", team.Lost);
                teamToUpdate?.SetValue("goalsFor", team.GoalsFor);
                teamToUpdate?.SetValue("goalsAgainst", team.GoalsAgainst);
                teamToUpdate?.SetValue("difference", team.Diff);

                if (team.TieWeight != null)
                {
                    teamToUpdate?.SetValue("tieWeight", team.TieWeight);
                }

                teamToUpdate?.SetValue("points", team.Points);
                _contentService.SaveAndPublish(teamToUpdate);
            }

            return NoContent();
        }

        [HttpPut("Placement")]
        //[ApiKeyAuthorize]
        public IActionResult PutPlacement([FromBody] TeamPlacements model)
        {
            var tournament = GetTournament(
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

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
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));
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
                model.IsChampionships,
                model.TitleEvent,
                string.Format(model.EventYear.ToString()));

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
