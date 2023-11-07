using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;

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
        public IActionResult PostPlacement([FromBody] TeamPlacement model)
        {
            var tournament = GetTournament(model.IsChampionships, model.TitleEvent, string.Format(model.EventYear.ToString()));
            if (tournament == null)
            {
                return NotFound();
            }

            var selectedTeam = tournament.Children.FirstOrDefault(x => x.Name == model.TeamName);
            if (selectedTeam == null)
            {
                return NotFound();
            }

            var teamToUpdate = _contentService.GetById(selectedTeam.Id);

            teamToUpdate?.SetValue("finalRanking", model.Placement);
            _contentService.SaveAndPublish(teamToUpdate);

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
