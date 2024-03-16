using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface IUserInvitationService
    {
        Task InviteUser(UserInvitationModel model);
    }
}
