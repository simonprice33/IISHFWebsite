using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class CreateScheduleGames : TournamentBaseModel
    {
        public CreateScheduleGames()
        {
            Games = new List<ScheduleGame>();
        }

        public IEnumerable<ScheduleGame> Games { get; set; }
    }
}
