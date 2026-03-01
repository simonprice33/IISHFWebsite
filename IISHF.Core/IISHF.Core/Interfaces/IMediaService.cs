using IISHF.Core.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface IMediaService
    {
        void SetVideoFeed(VideoFeeds model, IPublishedContent tournament);

        IMedia? GetMediaTemplate(string templateName);

        string GetTemplateUrl(IMedia template);
    }
}
