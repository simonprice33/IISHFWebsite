using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class RosterMembers : TournamentBaseModel
    {
        public RosterMembers()
        {
            ItcRosterMembers = new List<RosterMember>();
        }

        public string TeamName { get; set; }

        public IEnumerable<RosterMember> ItcRosterMembers { get; set; }
    }
}
