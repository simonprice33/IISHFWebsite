namespace IISHF.Core.Models
{
    public class TeamPlacement : TournamentBaseModel
    {
        public int Placement { get; set; }

        public string Iso3 { get; set; } = string.Empty;

        public string TeamName { get; set; } = string.Empty;

        public string? TeamLogoUrl { get; set; }
    }
}
