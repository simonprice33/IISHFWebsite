using IISHF.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class MediaService : Interfaces.IMediaService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMediaService _umbracoMediaService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MediaService> _logger;

        public MediaService(IPublishedContentQuery contentQuery,
            IContentService contentService,
            Umbraco.Cms.Core.Services.IMediaService umbracoMediaService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MediaService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _umbracoMediaService = umbracoMediaService;
            _httpContextAccessor = httpContextAccessor;
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

        public IMedia? GetMediaTemplate(string templateName)
        {
            var existingFolder = _umbracoMediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Templates" && x.ContentType.Alias == Umbraco.Cms.Core.Constants.Conventions.MediaTypes.Folder);

            IMedia template = null!;

            if (existingFolder != null)
            {
                // 1) Find Email folder under Templates
                var emailFolder = _umbracoMediaService
                    .GetPagedChildren(existingFolder.Id, 0, int.MaxValue, out long totalRecords1)
                    .FirstOrDefault(x => x.Name == "Email"
                                         && x.ContentType.Alias == Umbraco.Cms.Core.Constants.Conventions.MediaTypes.Folder);

                if (emailFolder != null)
                {
                    // 2) Find the template file under Templates/Email
                    var children = _umbracoMediaService
                        .GetPagedChildren(emailFolder.Id, 0, int.MaxValue, out long totalRecords2);

                    template = children.FirstOrDefault(x => x.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase)
                                                            && x.ContentType.Alias != Umbraco.Cms.Core.Constants.Conventions.MediaTypes.Folder);
                }
            }

            if (template == null)
            {
                throw new InvalidOperationException($"Email template '{templateName}' not found in Media Library under 'Templates/Email'.");
            }

            return template;
        }

        public string GetTemplateUrl(IMedia template)
        {
            var umbracoFile = template.GetValue<string>("umbracoFile");
            if (string.IsNullOrWhiteSpace(umbracoFile))
                throw new InvalidOperationException($"Media item '{template.Name}' has no 'umbracoFile'.");

            var path = umbracoFile.Trim();

            // If stored as JSON, pull "src"
            if (path.StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(path);
                if (!doc.RootElement.TryGetProperty("src", out var srcProp))
                    throw new InvalidOperationException($"Media item '{template.Name}' has JSON 'umbracoFile' but no 'src'.");

                path = srcProp.GetString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"Media item '{template.Name}' has an empty file path.");

            // If already absolute, return it
            if (Uri.TryCreate(path, UriKind.Absolute, out _))
                return path;

            // Ensure leading slash
            if (!path.StartsWith("/"))
                path = "/" + path;

            var protocol = _httpContextAccessor.HttpContext?.Request?.Scheme ?? "https";
            var host = _httpContextAccessor.HttpContext?.Request?.Host.ToString()
                       ?? throw new InvalidOperationException("Missing Request.Host");

            return $"{protocol}://{host}{path}";
        }
    }
}
