namespace IISHF.Core.Models
{
    public class TournamentModel : TournamentBaseModel
    {
        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public string HostClub { get; set; }

        public string HostContact { get; set; }

        public string HostPhoneNumber { get; set; }

        public string HostWebsite { get; set; }

        public string HostEmail { get; set; }

        public string? HostImage { get; set; } = string.Empty;

        public string VenueName { get; set; }

        public string VenueAddress { get; set; }

        public decimal RinkLength { get; set; }

        public decimal RinkWidth { get; set; }

        public string? RinkFloor { get; set; }
    }
}
