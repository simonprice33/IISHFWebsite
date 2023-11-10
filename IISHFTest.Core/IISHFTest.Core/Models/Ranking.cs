using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class Ranking : TournamentBaseModel
    {
        public string TeamName { get; set; }
        
        public string Games { get; set; }
        
        public int Wins { get; set; }
        
        public int Ties { get; set; }
        
        public int Losses { get; set; }
        
        public int? GoalsFor { get; set; }
        
        public int? GoalsAgainst { get; set; }
        
        public int? Differnce { get; set; }
        
        public decimal? TieWeight { get; set; }
    }
}
