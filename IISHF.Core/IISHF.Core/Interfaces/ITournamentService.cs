using IISHF.Core.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface ITournamentService
    {
        int? CreateEvent(TournamentModel model);

        IContent CreateEventTeam(Team model, IPublishedContent tournament);

        IPublishedContent? GetTournament(int id);

        IPublishedContent? GetTournament(bool isChampionships, string titleEvent, string eventYear);

        IPublishedContent? GetTournamentTeamByName(string teamName, IPublishedContent tournament);

        IPublishedContent? GetTournamentTeamById(int id, IPublishedContent tournament);

        Task UpdateGameWithResults(UpdateTeamScores model, IPublishedContent tournament);

        Task CreateEventGame(CreateScheduleGames model, IPublishedContent tournament);

        Task UpdateTeamProperties(string propertyValue, string fieldName, IPublishedContent team);

        Task SetTeamInformationSubmissionDate(IPublishedContent team);

        Task SetTeamItcSubmissionDateFromTeam(IPublishedContent team);

        Task SetTeamItcNmaApprovalDate(IPublishedContent team);

        Task SetTeamItcIISHFApprovalDate(IPublishedContent team);

        Task SubmitTeamInformationToHost(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team);

        Task NotifyNmaApprover(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team);

        Task NotifyIISHFApprover(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team);
    }
}
