using IISHF.Core.Models;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface ITournamentService
    {
        int? CreateEvent(TournamentModel model);

        IContent CreateEventTeam(Team model, IPublishedContent tournament);

        Task<IContent>? AddGameSheetToGame(IFormFile file, string name, IPublishedContent game);


        IPublishedContent? GetTournament(int id);

        IPublishedContent? GetTournament(bool isChampionships, string titleEvent, string eventYear);

        IPublishedContent? GetTournamentTeamByName(string teamName, IPublishedContent tournament);

        IPublishedContent? GetTournamentTeamById(int id, IPublishedContent tournament);

        IPublishedContent? GetTournamentTeamByKey(Guid key, IPublishedContent tournament);

        Task UpdateGameWithResults(UpdateTeamScores model, IPublishedContent tournament);

        Task CreateEventGame(CreateScheduleGames model, IPublishedContent tournament);

        Task UpdateEventGame(CreateScheduleGames model, IPublishedContent tournament);

        Task UpdateTeamProperties(string propertyValue, string fieldName, IPublishedContent team);

        Task SetTeamInformationSubmissionDate(IPublishedContent team);

        Task SetTeamItcSubmissionDateFromTeam(IPublishedContent team);

        Task SetSelectTeamCreator(IPublishedContent team);

        Task SetTeamItcNmaApprovalDate(IPublishedContent team);

        Task SetTeamItcIISHFApprovalDate(IPublishedContent team);

        Task SubmitTeamInformationToHost(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team);

        Task NotifyNmaApprover(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team);

        Task NotifyIISHFApprover(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team);

        Task UnsubmitItc(IPublishedContent team);

        Task CopyItc(IPublishedContent team, bool rejected = false);

        Task SetNmaCheckValue(IEnumerable<RosterApproval> rosterMembers, bool isNma);

        Task<RejectedRosterMembersModel> GetRejectedRosterMembers(int[] playerIds);

        Task ResetNmaApproval(IPublishedContent team);

        Task SetItcRejectionReason(IPublishedContent team);

        byte[] GenerateItcAsPdfFile(byte[] itcBytes);

        Task<byte[]> GenerateItcAsExcelFile(IPublishedContent team, IPublishedContent tournament);
    }
}
