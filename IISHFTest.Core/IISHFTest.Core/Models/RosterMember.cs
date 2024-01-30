using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class RosterMember
    {
        public int Id { get; set; }

        public string License { get; set; } = string.Empty;

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PlayerName => $"{FirstName.Trim()} {LastName.Trim()}";

        public string Role { get; set; } = string.Empty;

        public bool IsBenchOfficial { get; set; }

        public int JerseyNumber { get; set; }

        public DateOnly DateOfBirth { get; set; } = default;
        
        public bool NmaCheck { get; set; }

        public bool IISHFCheck { get; set; }
        
        public string Comments { get; set; } = string.Empty;
    }
}
