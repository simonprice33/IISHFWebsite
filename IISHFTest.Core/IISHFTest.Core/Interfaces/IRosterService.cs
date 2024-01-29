using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHFTest.Core.Interfaces
{
    public interface IRosterService
    {
        public Task SetRosterForTeamInformation(RosterMembers roster, IPublishedContent team);


        public Task UpdateRosterWithItcValues(RosterMembers roster, IPublishedContent team);
    }
}
