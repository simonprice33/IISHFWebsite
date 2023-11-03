using IISHFTest.Core.Models;
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
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace IISHFTest.Core.Controllers.SurfaceControllers
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
        public IActionResult GetSheduleAndResults()
        {
            var model = new EventResultRequestViewModel();
            return PartialView("~/Views/Partials/Events/SchedulAndResults.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetPlacements(int year, string titleEvent)
        {
            var eventPlacements = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("eventPlacement")
                .ToList();

            var specificEventPlacement = eventPlacements
                .Where(ep => ep.Value<int>("eventYear") == year && ep.Value<string>("titleEvent") == titleEvent).ToList();


            if (specificEventPlacement == null)
            {
                // Handle not found situation
                return PartialView("~/Views/Partials/Events/EventPlacements.cshtml", new FinalPlacementsViewModel());
            }

            var teamPlacements = specificEventPlacement.Select(placementItem => new TeamPlacement
            {
                Placement = placementItem.Value<int>("finalPlacement"),
                Iso3 = placementItem.Value<string>("iso3"),
                TeamName = placementItem.Value<string>("teamName"),
                EventYear = placementItem.Value<int>("eventYear"),
                TitleEvent = placementItem.Value<string>("titleEvent")
            }).ToList();

            var model = new FinalPlacementsViewModel
            {
                TeamPlacements = teamPlacements
            };

            return PartialView("~/Views/Partials/Events/EventPlacements.cshtml", model);
        }
    }
}
