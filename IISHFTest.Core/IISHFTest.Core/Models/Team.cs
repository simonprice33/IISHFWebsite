using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class Team : TournamentBaseModel
    {
        public string TeamName { get; set; }

        public string CountryIso3 { get; set; }

        public string Group { get; set; }

        public Uri? TeamUrl { get; set; }
    }
}
