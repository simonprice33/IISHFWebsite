using IISHF.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface IInvitationService
    {
        List<EventInvitation> GetInvitation(string email);
    }
}
