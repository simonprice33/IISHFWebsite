namespace IISHF.Core.Models
{
    public class RankingViewModel
    {
        public string TeamName { get; set; }

        public string Group { get; set; }
        
        public int Games { get; set; }
        
        public int Wins { get; set; }
        
        public int Ties { get; set; }
        
        public int Losses { get; set; }
        
        public int? GoalsFor { get; set; }
        
        public int? GoalsAgainst { get; set; }
        
        public int? Differnce { get; set; }
        
        public decimal? TieWeight { get; set; }

        public string TeamLogoUrl { get; set; }

        public int Points { get; set; }
        public int GroupPlacement { get; set; }
    }
}
