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
        public RosterMembers UpsertRosterMembers(RosterMembers roster, IPublishedContent team);

        ////public RosterMembers UpsertRosterMembers(TeamInformationSubmission roster, IPublishedContent team);

        public void DeleteRosteredPlayer(int playerId);

        public IPublishedContent FindRosterMemberById(int playerId, IPublishedContent team);
    }
}
