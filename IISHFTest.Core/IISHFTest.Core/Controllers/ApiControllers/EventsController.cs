using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
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

        [HttpPost("Placement")]
        //[ApiKeyAuthorize]
        public IActionResult PostPlacement([FromBody] TeamPlacement model)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var placementItems = rootContent.FirstOrDefault(x => x.Name == "Data")!.Children.FirstOrDefault(x => x.Name == "Event Placements");

            if (placementItems != null)
            {
                var newContact = _contentService.Create("Placement", placementItems.Id, "eventPlacement");
                newContact.SetValue("teamName", model.TeamName);
                newContact.SetValue("iso3", model.Iso3);
                newContact.SetValue("finalPlacement", model.Placement);
                newContact.SetValue("titleEvent", model.TitleEvent);
                newContact.SetValue("eventYear", model.EventYear);

                _contentService.SaveAndPublish(newContact);
            }

            return Ok();
        }
    }
}
