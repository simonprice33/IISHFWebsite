using IISHF.Core.Models;
using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface IUserService
    {
        Task<IMember> RegisterUser(RegisterViewModel model);

        Guid GetVerificationKey(IMember member, Guid token, bool redirectToPasswordReset);

        IMember GetMembersByPropertyValue(string token, string property);

        Task<IdentityResult> UpdatePassword(string email, string password);
    }
}
