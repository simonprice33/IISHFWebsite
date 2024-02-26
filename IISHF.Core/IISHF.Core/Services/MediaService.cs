using IISHF.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class MediaService : Interfaces.IMediaService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly ILogger<MediaService> _logger;

        public MediaService(IPublishedContentQuery contentQuery,
            IContentService contentService,
            ILogger<MediaService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _logger = logger;
        }

        public void SetVideoFeed(VideoFeeds model, IPublishedContent tournament)
        {
            foreach (var feed in model.Feeds)
            {
                var eventVideoFeed = _contentService.Create(feed.Title, tournament.Id, "liveFeeds");

                var linkObject = new
                {
                    name = $"{feed.Title}",
                    url = feed.FeedUri,
                    target = "_blank",
                };

                var linkArray = new[] { linkObject };
                var jsonLinkArray = JsonSerializer.Serialize(linkArray);

                eventVideoFeed.SetValue("liveFeedUrl", jsonLinkArray);
                eventVideoFeed.SetValue("feedOrder", feed.FeedOrder);
                eventVideoFeed.SetValue("liveFeedDateTime", feed.LIveFeedDateTimeUtc);
                eventVideoFeed.SetValue("showInSite", true);

                _contentService.SaveAndPublish(eventVideoFeed);
            }
        }
    }
}
