namespace IISHF.DocumentManagement.Models
{
    public class SubmittedTeamInformationModel
    {
        public int Id { get; set; }

        public string TeamName { get; set; } = string.Empty;

        public string EventReference { get; set; } = string.Empty;

        public string TeamWriteUp { get; set; } = string.Empty;

        public string AgeGroup { get; set; }

        public byte[] TeamLogo { get; set; }

        public byte[] TeamPhoto { get; set; }

        public IEnumerable<RosterMember> Roster { get; set; } = new List<RosterMember>();

        public DateTime SubmittedDateTime { get; set; }
        
        public string SubmittedBy { get; set; }

        public string SubmittedByEmail { get; set;}
    }
}
