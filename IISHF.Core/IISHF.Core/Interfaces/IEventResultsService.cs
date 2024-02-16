using IISHF.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface IEventResultsService
    {
        void UpdatePlayerStatistics(UpdatePlayerStatistics model, IPublishedContent tournament);

        void UpdateGroupRanking(Rankings model, IPublishedContent tournament);

        void UpdateFinalPlacement(TeamPlacements model, IPublishedContent tournament);
    }
}
