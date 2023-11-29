using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class Rankings : TournamentBaseModel
    {

        public Rankings()
        {
            Ranking = new List<Ranking>();
        }
        public IEnumerable<Ranking> Ranking { get; set; }
    }
}
