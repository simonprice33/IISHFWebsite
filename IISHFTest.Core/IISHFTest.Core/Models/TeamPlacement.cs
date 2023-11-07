using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TeamPlacement
    {
        public int Placement { get; set; }

        public string Iso3 { get; set; } = string.Empty;

        public string TeamName { get; set; } = string.Empty;

        public int EventYear { get; set; }

        public string TitleEvent { get; set; } = string.Empty;

        public string? TeamLogoUrl { get; set; }

        public bool IsChampionships { get; set; }
    }
}
