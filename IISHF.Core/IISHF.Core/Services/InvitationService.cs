using IISHF.Core.Extensions;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using IISHF.Core.State;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberManager _memberManager;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberManager memberManager,
            ILogger<InvitationService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberManager = memberManager;
            _logger = logger;
        }

        public async Task<List<EventInvitation>> GetInvitation(string email)
        {
            var invitations = new List<EventInvitation>();

            var loggedInMember = await _memberManager.GetCurrentMemberAsync();

            var teams = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("clubTeam")
                .Where(x => x.Value<string>("TeamContactEmail") == email).ToList();

            var tournamentsRoot = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault();

            var publishedEvents = tournamentsRoot != null
                ? tournamentsRoot.Children().ToList()
                : new List<IPublishedContent>();

            if (teams != null)
            {
                foreach (var myteam in teams)
                {
                    foreach (var iishfEvent in publishedEvents.Where(x => x.ContentType.Alias != "noneTitleEvents"))
                    {
                        foreach (var ageGroup in iishfEvent.Children().ToList())
                        {
                            try
                            {
                                // ToDo
                                // Fix this to that is will find the appropriate age group for title event.

                                var titleEvents = ageGroup.Children().Where(x =>
                                    x.Name == DateTime.Now.Year.ToString() &&
                                    x.Parent.Value<string>("ageGroup") == myteam.Value<string>("ageGroup")).ToList();

                                foreach (var evt in titleEvents)
                                {
                                    var team = evt.Children().Where(x => x.ContentType.Alias == "team")
                                        .FirstOrDefault(x => x.Value<string>("NMaTeamKey") == myteam.Key.ToString());

                                    if (team != null)
                                    {
                                        var requiredByDate = GetRequiredByDate(team, evt);

                                        DateTime? itcStatusChangeDate;
                                        var itcState = GetItcStatus(team, out itcStatusChangeDate);

                                        var teamInformationSubmitted = team.Value<bool>("teamInformationSubmitted");
                                        DateTime? teamInformationSubmittedDate = null;

                                        if (teamInformationSubmitted)
                                        {
                                            teamInformationSubmittedDate = team.Value<DateTime>("teamInformationSubmissionDate");
                                        }

                                        var invitation = new EventInvitation()
                                        {
                                            EventId = evt.Key,
                                            EventTeamId = team.Key,
                                            EventName = evt.Parent.Name,
                                            ITCStatus = itcState,
                                            ItcStatusChangeDate = itcStatusChangeDate,
                                            TeamInformationRequired = true,
                                            TeamInformationSubmitted = teamInformationSubmitted,
                                            TeamInformationSubmittedDate = teamInformationSubmittedDate,
                                            TeamInformationRequiredBy = requiredByDate,
                                            EventStartDate = evt.Value<DateTime>("eventStartDate"),
                                            EventEndDate = evt.Value<DateTime>("eventEndDate")
                                        };

                                        invitations.Add(invitation);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e,
                                    "Error building title-event invitations. TeamKey={TeamKey}, AgeGroupKey={AgeGroupKey}",
                                    myteam?.Key,
                                    ageGroup?.Key);
                            }
                        }
                    }

                    foreach (var iishfEvent in publishedEvents.Where(x => x.ContentType.Alias == "noneTitleEvents"))
                    {
                        foreach (var evt in iishfEvent.Children().ToList())
                        {
                            var team = evt.Children().Where(x => x.ContentType.Alias == "team")
                                .FirstOrDefault(x => x.Name == myteam.Name);

                            if (team == null)
                            {
                                continue;
                            }

                            var requiredByDate = GetRequiredByDate(team, evt);

                            DateTime? itcStatusChangeDate;
                            var itcState = GetItcStatus(team, out itcStatusChangeDate);

                            var invitation = new EventInvitation()
                            {
                                EventId = evt.Key,
                                EventTeamId = team.Key,
                                EventName = evt.Name,
                                ITCStatus = itcState,
                                ItcStatusChangeDate = itcStatusChangeDate,
                                TeamInformationRequired = false,
                                TeamInformationRequiredBy = requiredByDate,
                                TeamInformationSubmitted = false,
                                TeamInformationSubmittedDate = null,
                                EventStartDate = evt.Value<DateTime>("eventStartDate"),
                                EventEndDate = evt.Value<DateTime>("eventEndDate")
                            };

                            invitations.Add(invitation);
                        }
                    }

                    foreach (var iishfEvent in publishedEvents.Where(x => x.ContentType.Alias == "noneTitleEvents"))
                    {
                        foreach (var evt in iishfEvent.Children().ToList())
                        {
                            IPublishedContent team = null;
                            var selectTeam = evt.Children()
                                .Where(X => X.Value<IPublishedContent>("selectTeamCreatedBy") != null);

                            if (selectTeam != null && selectTeam.Any())
                            {
                                // Note: keeping your original logic and variables.
                                // If loggedInMember could be null, this will throw; leaving as-is to avoid behaviour change.
                                var mySelectTeam = selectTeam.FirstOrDefault(x =>
                                    x.Value<IPublishedContent>("selectTeamCreatedBy").Id.ToString() == loggedInMember.Id);

                                team = mySelectTeam;
                            }

                            if (team == null)
                            {
                                continue;
                            }

                            var requiredByDate = GetRequiredByDate(team, evt);

                            DateTime? itcStatusChangeDate;
                            var itcState = GetItcStatus(team, out itcStatusChangeDate);

                            var invitation = new EventInvitation()
                            {
                                EventId = evt.Key,
                                EventTeamId = team.Key,
                                EventName = evt.Name,
                                ITCStatus = itcState,
                                ItcStatusChangeDate = itcStatusChangeDate,
                                TeamInformationRequired = false,
                                TeamInformationRequiredBy = requiredByDate,
                                TeamInformationSubmitted = false,
                                TeamInformationSubmittedDate = null,
                                EventStartDate = evt.Value<DateTime>("eventStartDate"),
                                EventEndDate = evt.Value<DateTime>("eventEndDate")
                            };

                            invitations.Add(invitation);
                        }
                    }
                }
            }

            return invitations;
        }

        private static DateTime GetRequiredByDate(IPublishedContent team, IPublishedContent evt)
        {
            var requiredByDate = team.Value<DateTime>("teamSubmissionRequiredBy");

            if (requiredByDate == DateTime.MinValue)
            {
                requiredByDate = evt.Value<DateTime>("eventStartDate").AddDays(-56);
            }

            return requiredByDate;
        }

        private static string GetItcStatus(IPublishedContent team, out DateTime? itcStatusChangeDate)
        {
            // This method is the only place that knows how to map Umbraco props -> ITC state.
            // It assumes you have created separate files for:
            // - ItcStateMachine (Evaluate)
            // - ItcEvaluation / result type (State + StatusChangeDate)
            // - ItcState enum with [Description]
            // - EnumExtensions.GetDescription()

            // Expected shape:
            // var eval = ItcStateMachine.Evaluate(team);
            // itcStatusChangeDate = eval.StatusChangeDate;
            // return eval.State.GetDescription();

            var eval = ItcStateMachine.Evaluate(team);

            itcStatusChangeDate = eval.StatusChangeDate;
            return eval.State.GetDescription();
        }
    }
}
