using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TeamPlacements : TournamentBaseModel
    {
        public TeamPlacements()
        {
            Placements = new List<TeamPlacement>();
        }

        public IEnumerable<TeamPlacement> Placements { get; set; }
    }
}
