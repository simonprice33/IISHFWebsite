using IISHF.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface IInvitationService
    {
        Task<List<EventInvitation>> GetInvitation(string email);
    }
}
