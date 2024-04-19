using IISHF.Core.Interfaces;
using IISHF.Core.Models;
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

            var publishedEvents = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault().Children().ToList();

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
                                        .FirstOrDefault(x => x.Name == myteam.Name || myteam.Name.Contains(x.Name));

                                    if (team != null)
                                    {
                                        var requiredByDate = team.Value<DateTime>("teamSubmissionRequiredBy");

                                        if (requiredByDate == DateTime.MinValue)
                                        {
                                            requiredByDate = evt.Value<DateTime>("eventStartDate").AddDays(-56);
                                        }

                                        var itcState = "Not submitted";
                                        DateTime itcStatusChangeDate = new DateTime();
                                        if (team.Value<bool>("iTCSubmitted"))
                                        {
                                            var nmaApprover = team.Value<IPublishedContent>("iTCNMAApprover");
                                            if (nmaApprover == null)
                                            {
                                                itcState = "Pending NMA Approval";
                                                itcStatusChangeDate = team.Value<DateTime>("iTCSubmissionDate");
                                            }

                                            var iishfItcApprover = team.Value<IPublishedContent>("iISHFITCApprover");
                                            if (nmaApprover != null && team.Value<DateTime>("nMAApprovedDate") != DateTime.MinValue)
                                            {
                                                itcState = "NMA Approved - Pending IISHF Approval";
                                                itcStatusChangeDate = team.Value<DateTime>("nMAApprovedDate");

                                            }
                                        }

                                        if (!string.IsNullOrWhiteSpace(team.Value<string>("iTCRejectionReason")))
                                        {
                                            itcState = "Changes required";
                                        }

                                        if (team.Value<bool>("iTCSubmitted") && !string.IsNullOrWhiteSpace(team.Value<string>("iTCRejectionReason")))
                                        {
                                            itcState = "Changes made";
                                        }

                                        if (team.Value<bool>("iTCSubmitted")
                                            && !string.IsNullOrWhiteSpace(team.Value<string>("iTCRejectionReason"))
                                            && team.Value<IPublishedContent>("nMAApprover") != null)
                                        {
                                            itcState = "NMA Approved - Pending IISHF Approval";
                                            itcStatusChangeDate = team.Value<DateTime>("iTCSubmissionDate");
                                        }



                                        var invitation = new EventInvitation()
                                        {
                                            EventId = evt.Key,
                                            EventTeamId = team.Key,
                                            EventName = evt.Parent.Name,
                                            ITCStatus = itcState,
                                            ItcStatusChangeDate = itcStatusChangeDate == DateTime.MinValue ? null : itcStatusChangeDate,
                                            TeamInformationRequired = true,
                                            TeamInformationSubmitted = team.Value<bool>("teamInformationSubmitted"),
                                            TeamInformationSubmittedDate = team.Value<DateTime>("teamInformationSubmissionDate"),
                                            TeamSubmissionRequiredBy = requiredByDate,
                                            EventStartDate = evt.Value<DateTime>("eventStartDate"),
                                            EventEndDate = evt.Value<DateTime>("eventEndDate")
                                        };

                                        invitations.Add(invitation);
                                    }

                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
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

                            var requiredByDate = team.Value<DateTime>("teamSubmissionRequiredBy");

                            if (requiredByDate == DateTime.MinValue)
                            {
                                requiredByDate = evt.Value<DateTime>("eventStartDate").AddDays(-56);
                            }

                            var itcState = "Not submitted";
                            DateTime itcStatusChangeDate = new DateTime();
                            if (team.Value<bool>("iTCSubmitted"))
                            {
                                var nmaApprover = team.Value<IPublishedContent>("iTCNMAApprover");
                                if (nmaApprover == null)
                                {
                                    itcState = "Pending NMA Approval";
                                    itcStatusChangeDate = team.Value<DateTime>("iTCSubmissionDate");
                                }

                                var iishfItcApprover = team.Value<IPublishedContent>("iISHFITCApprover");
                                if (nmaApprover != null && team.Value<DateTime>("nMAApprovedDate") != DateTime.MinValue)
                                {
                                    itcState = "NMA Approved - Pending IISHF Approval";
                                    itcStatusChangeDate = team.Value<DateTime>("nMAApprovedDate");

                                }
                            }

                            if (!string.IsNullOrWhiteSpace(team.Value<string>("iTCRejectionReason")))
                            {
                                itcState = "Changes required";
                            }

                            var invitation = new EventInvitation()
                            {
                                EventId = evt.Key,
                                EventTeamId = team.Key,
                                EventName = evt.Name,
                                ITCStatus = itcState,
                                ItcStatusChangeDate = itcStatusChangeDate == DateTime.MinValue ? null : itcStatusChangeDate,
                                TeamInformationRequired = false,
                                TeamInformationRequiredBy = requiredByDate,
                                TeamInformationSubmitted = false,
                                TeamInformationSubmittedDate = null,
                                TeamSubmissionRequiredBy = null,
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
                                var mySelectTeam = selectTeam.FirstOrDefault(x =>
                                    x.Value<IPublishedContent>("selectTeamCreatedBy").Id.ToString() == loggedInMember.Id);
                                team = mySelectTeam;

                            }
                            if (team == null)
                            {
                                continue;
                            }

                            var requiredByDate = team.Value<DateTime>("teamSubmissionRequiredBy");

                            if (requiredByDate == DateTime.MinValue)
                            {
                                requiredByDate = evt.Value<DateTime>("eventStartDate").AddDays(-56);
                            }

                            var itcState = "Not submitted";
                            DateTime itcStatusChangeDate = new DateTime();
                            if (team.Value<bool>("iTCSubmitted"))
                            {
                                var nmaApprover = team.Value<IPublishedContent>("iTCNMAApprover");
                                if (nmaApprover == null)
                                {
                                    itcState = "Pending NMA Approval";
                                    itcStatusChangeDate = team.Value<DateTime>("iTCSubmissionDate");
                                }

                                var iishfItcApprover = team.Value<IPublishedContent>("iISHFITCApprover");
                                if (nmaApprover != null && team.Value<DateTime>("nMAApprovedDate") != DateTime.MinValue)
                                {
                                    itcState = "NMA Approved - Pending IISHF Approval";
                                    itcStatusChangeDate = team.Value<DateTime>("nMAApprovedDate");

                                }
                            }

                            if (!string.IsNullOrWhiteSpace(team.Value<string>("iTCRejectionReason")))
                            {
                                itcState = "Changes required";
                            }

                            var invitation = new EventInvitation()
                            {
                                EventId = evt.Key,
                                EventTeamId = team.Key,
                                EventName = evt.Name,
                                ITCStatus = itcState,
                                ItcStatusChangeDate = itcStatusChangeDate == DateTime.MinValue ? null : itcStatusChangeDate,
                                TeamInformationRequired = false,
                                TeamInformationRequiredBy = requiredByDate,
                                TeamInformationSubmitted = false,
                                TeamInformationSubmittedDate = null,
                                TeamSubmissionRequiredBy = null,
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
    }
}
