using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class Ranking
    {
        public string TeamName { get; set; }
        
        public string Games { get; set; }
        
        public int won { get; set; }
        
        public int Tied { get; set; }
        
        public int Lost { get; set; }
        
        public int? GoalsFor { get; set; }
        
        public int? GoalsAgainst { get; set; }
        
        public int? Diff { get; set; }

        public int? Points { get; set; }
        
        public decimal? TieWeight { get; set; }

        public int Place { get; set; }
    }
}
