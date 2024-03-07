using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Models;
using Umbraco.Cms.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface INMAService
    {
        Task<IContent> CreatingNmaReportingYear(int reportingYear, Guid nmaKey);

        Task<IContent> AddClub(NmaClub club, int reportingYear, Guid NmaKey);

        Task AddClubTeams(NmaClub club, IContent nmaClubContent);
    }
}
