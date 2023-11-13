using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class RosterMember
    {
        public string License { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsBenchOfficial { get; set; }

        public int JerseyNumber { get; set; }

        public DateOnly DateOfBirth { get; set; }
        
        public bool NmaCheck { get; set; }

        public bool IISHFCheck { get; set; }
        
        public string Comments { get; set; } = string.Empty;
    }
}
