using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Models;
using IISHF.Core.Models.ServiceBusMessage;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface INMAService
    {
        Task<IContent> CreatingNmaReportingYear(int reportingYear, Guid nmaKey);

        Task<IContent> AddClub(NmaClub club, int reportingYear, Guid NmaKey);

        Task AddClubTeams(NmaClub club, IContent nmaClubContent);

        Task<IEnumerable<ITCApprover>> GetNMAITCApprovers(Guid nmaKey);
    }
}
