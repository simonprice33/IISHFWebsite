using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class RejectedRosterMembersModel
    {
        public RejectedRosterMembersModel()
        {
            
        }

        public IEnumerable<RejectedRosterMember> RejectedRosterMembers { get; set; }
    }

}
