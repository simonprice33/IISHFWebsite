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
        Task<RosterMembers> UpsertRosterMembers(RosterMembers model, IPublishedContent team);

        public void DeleteRosteredPlayer(int playerId);

        public IPublishedContent FindRosterMemberById(int playerId, IPublishedContent team);
    }
}
