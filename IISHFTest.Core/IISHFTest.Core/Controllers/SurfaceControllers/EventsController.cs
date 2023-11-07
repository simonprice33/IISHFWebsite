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
using Lucene.Net.Index;

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
            var teams = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("team")
                .ToList();

            var eventTeams = teams.Where(x =>
                    x.Parent != null &&
                    x.Parent.Name == year.ToString() &&
                    x.Parent.Parent != null &&
                    x.Parent.Parent.Value<string>("EventShotCode") == titleEvent)
                .ToList();

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
    }
}
