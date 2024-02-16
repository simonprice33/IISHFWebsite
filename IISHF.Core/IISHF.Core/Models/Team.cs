namespace IISHF.Core.Models
{
    public class Team : TournamentBaseModel
    {
        public string TeamName { get; set; }

        public string CountryIso3 { get; set; }

        public string Group { get; set; }

        public Uri? TeamUrl { get; set; }
    }
}
