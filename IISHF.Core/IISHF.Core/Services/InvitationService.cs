using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            ILogger<InvitationService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _logger = logger;
        }

        public List<EventInvitation> GetInvitation(string email)
        {

            var invitations = new List<EventInvitation>();

            var teams = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("clubTeam")
                .FirstOrDefault(x => x.Value<string>("TeamContactEmail") == email);

            var publishedEvents = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault().Children().ToList();

            if (teams != null)
            {
                foreach (var iishfEvent in publishedEvents.Where(x => x.ContentType.Alias != "noneTitleEvents"))
                {
                    foreach (var ageGroup in iishfEvent.Children().ToList())
                    {
                        try
                        {
                            var titleEvents = ageGroup.Children().Where(x =>
                                x.Name == DateTime.Now.Year.ToString() &&
                                x.Parent.Value<string>("ageGroup") == teams.Value<string>("ageGroup")).ToList();
                            foreach (var evt in titleEvents)
                            {
                                var team = evt.Children().Where(x => x.ContentType.Alias == "team")
                                    .FirstOrDefault(x => x.Name == teams.Name);

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
                            .FirstOrDefault(x => x.Name == teams.Name);

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

            return invitations;

        }
    }
}
