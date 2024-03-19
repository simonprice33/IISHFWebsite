using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class UserInvitationModel
    {
        public UserInvitationModel()
        {
            NmaRoles = new List<string>();
            clubTeams = new List<Guid>();
        }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsIISHF { get; set; } = false;

        public bool IsNma { get; set; } = false;

        public bool IsClubContact { get; set; } = false;

        public bool IsTeamAdmin { get; set; } = false;

        public Guid? NmaKey { get; set; } = Guid.Empty;

        public Guid? ClubKey { get; set; } = Guid.Empty;

        public IEnumerable<Guid> clubTeams { get; set; }

        public IEnumerable<string> NmaRoles { get; set; }

        public string OtherNmaRole { get; set; } = string.Empty;
    }
}
