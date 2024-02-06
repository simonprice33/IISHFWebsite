using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Models;

namespace IISHFTest.Core.Interfaces
{
    public interface IInvitationService
    {
        List<EventInvitation> GetInvitation(string email);
    }
}
