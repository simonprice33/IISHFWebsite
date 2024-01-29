using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHFTest.Core.Interfaces
{
    public interface IEventResultsService
    {
        void UpdatePlayerStatistics(UpdatePlayerStatistics model, IPublishedContent tournament);

        void UpdateGroupRanking(Rankings model, IPublishedContent tournament);

        void UpdateFinalPlacement(TeamPlacements model, IPublishedContent tournament);
    }
}
