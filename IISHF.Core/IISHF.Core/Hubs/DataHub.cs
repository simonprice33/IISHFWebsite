using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Models;
using DocumentFormat.OpenXml.Bibliography;

namespace IISHF.Core.Hubs
{
    public class DataHub : Hub
    {
        public async Task UpdateScores(int gameNumber, int homeScore, int awayScore)
        {
            // Broadcast the updated scores to all clients
            await Clients.All.SendAsync("UpdateScores", gameNumber, homeScore, awayScore);
        }

        public async Task UpdateGamesWithTeams(int year, int shortCode)
        {
            // Broadcast the updated scores to all clients
            await Clients.All.SendAsync("UpdateGamesWithTeams", year, shortCode);
        }

        public async Task UpdatePlayerStats(int year, int shortCode)
        {
            await Clients.All.SendAsync("UpdatePlayerStats", year, shortCode);
        }
        public async Task UpdateGroupRanking(int year, int shortCode)
        {
            await Clients.All.SendAsync("UpdateGroupRanking", year, shortCode);
        }
        public async Task UpdateFinalPlacement(int year, int shortCode)
        {
            await Clients.All.SendAsync("UpdateFinalPlacement", year, shortCode);
        }
    }
}
