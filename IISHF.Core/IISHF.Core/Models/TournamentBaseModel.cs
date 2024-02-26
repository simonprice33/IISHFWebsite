namespace IISHF.Core.Models
{
    public class TournamentBaseModel
    {
        public int EventId { get; set; }

        public bool IsChampionships { get; set; }

        public int EventYear { get; set; }

        public string TitleEvent { get; set; } = string.Empty;

    }
}
