using IISHFTest.Core.Models;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHFTest.Core.Interfaces
{
    public interface ITournamentService
    {
        int? CreateEvent(TournamentModel model);

        IContent CreateEventTeam(Team model, IPublishedContent tournament);

        IPublishedContent? GetTournament(bool isChampionships, string titleEvent, string eventYear);

        IPublishedContent? GetTournamentTeamByName(string teamName, IPublishedContent tournament);

        Task UpdateGameWithResults(UpdateTeamScores model, IPublishedContent tournament);

        Task CreateEventGame(CreateScheduleGames model, IPublishedContent tournament);

        Task UpdateTeamColours(string colourHex, string fieldName, IPublishedContent team);

        Task SetSubmissionDate(IPublishedContent team);
    }
}
