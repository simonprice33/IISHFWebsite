using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TeamInformationModel
    {
    
        public List<RosterMember> ItcRosterMembers { get; set; }

        public string TeamPhotoPath { get; set; }

        public string TeamLogoPath { get; set; }

        public List<string> SponsorPaths { get; set; }

        public string TeamHistory { get; set; }

        public string JerseyOneColour { get; set; }

        public string JerseyTwoColour { get; set; }

        public object SubmittedDate { get; set; }
    }
}
