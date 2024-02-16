using IISHF.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface IRosterService
    {
        Task<RosterMembers> UpsertRosterMembers(RosterMembers model, IPublishedContent team);

        public void DeleteRosteredPlayer(int playerId);

        public IPublishedContent FindRosterMemberById(int playerId, IPublishedContent team);
    }
}
