using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Models;
using Umbraco.Cms.Core.Models;

namespace IISHFTest.Core.Interfaces
{
    public interface IUserService
    {
        Task<IMember> RegisterUser(RegisterViewModel model);

        Guid GetVerificationKey(IMember member, Guid token);

        IMember GetMembersByPropertyValue(string token, string property);
    }
}
