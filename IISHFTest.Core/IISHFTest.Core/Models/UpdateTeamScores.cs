using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class UpdateTeamScores : TournamentBaseModel
    {
        public UpdateTeamScores()
        {
            Scores = new List<UpdateTeamScore>();
        }

        public IEnumerable<UpdateTeamScore> Scores { get; set; }
    }
}
