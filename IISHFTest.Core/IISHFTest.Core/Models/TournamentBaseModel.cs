using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TournamentBaseModel
    {
        public bool IsChampionships { get; set; }

        public int EventYear { get; set; }

        public string TitleEvent { get; set; } = string.Empty;

    }
}
