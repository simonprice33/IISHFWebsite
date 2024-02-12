using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TeamInformationSubmissionResponse
    {
    
        public List<RosterMember> ItcRosterMembers { get; set; }

        public string TeamPhotoPath { get; set; }

        public string TeamLogoPath { get; set; }

        public List<string> SponsorPaths { get; set; }
    }
}
