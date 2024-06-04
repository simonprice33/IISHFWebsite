using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace IISHF.Core.Models
{
    public class GameSheets : TournamentBaseModel
    {
        public IFormFile GamesSheet { get; set; }

        public int GameNumber { get; set; }
    }
}
