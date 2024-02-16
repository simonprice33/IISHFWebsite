namespace IISHF.Core.Models.ServiceBusMessage
{
    public class TeamInformation
    {
        public int Id { get; set; }

        public string TeamName { get; set; } = string.Empty;

        public string EventReference { get; set; } = string.Empty;

        public string TeamWriteUp { get; set; } = string.Empty;

        public string AgeGroup { get; set; }

        public Uri ClubLogo { get; set; }

        public Uri TeamLogo { get; set; }

        public Uri TeamPhoto { get; set; }

        public IEnumerable<Roster> Roster { get; set; } = new List<Roster>();

        public string SubmittedBy { get; set; }

        public string SubmittedByEmail { get; set; }

        public DateTime SubmittedDateTime { get; set; }

        public Uri IISHFLogo { get; set; }
        public List<Sponsor> Sponsors { get; set; }
    }

    public class Sponsor
    {
        public Uri SponsorLogo { get; set; }

        public string SponsorName { get; set; }
    }
}
