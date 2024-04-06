using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using IISHF.Core.Models.ServiceBusMessage;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class ApprovalService : IApprovals
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            ILogger<ApprovalService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
            _logger = logger;
        }

        public async Task<bool> IsApprover()
        {
            // First Pass is user NMA Approver?
            var loggedInMember = await _memberManager.GetCurrentMemberAsync();

            var nmaApprover = _memberService.GetMembersByPropertyValue("nMAITCApprover", true)
                .FirstOrDefault(x => x.Email == loggedInMember.Email);

            var iishfApprover = _memberService.GetMembersByPropertyValue("nMAITCApprover", true)
                .FirstOrDefault(x => x.Email == loggedInMember.Email);

            return nmaApprover != null || iishfApprover != null;
        }


        public async Task<IEnumerable<ITCApproval>> GetApprovalsAsync()
        {
            var publishedEvents = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault().Children().ToList();

            var loggedInMember = await _memberManager.GetCurrentMemberAsync();

            var nmaApprover = _memberService.GetMembersByPropertyValue("nMAITCApprover", true)
                .FirstOrDefault(x => x.Email == loggedInMember.Email);

            var approverNma = nmaApprover?.GetValue<string>("nationalMemberAssosiciation");

            var nma = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("nationalMemberAssociations")
                .FirstOrDefault()
                .Children()
                .FirstOrDefault(x => x.Name == approverNma);


            var iishfItcApprover = _memberService.GetMembersByPropertyValue("iISHFITCApprover", true)
                           .FirstOrDefault(x => x.Email == loggedInMember.Email);

            var approvalsList = new List<ITCApproval>();

            var iishfPublishedEvents = publishedEvents.Where(x => x.ContentType.Alias != "noneTitleEvents").ToList();
            foreach (var publishedEvent in iishfPublishedEvents)
            {
                foreach (var ageGroup in publishedEvent.Children().ToList())
                {
                    var titleEvents = ageGroup.Children().Where(x => int.Parse(x.Name) >= DateTime.Now.Year);

                    if (nmaApprover != null && nma != null)
                    {
                        AddNmaApprovals(approvalsList, titleEvents, nma, ageGroup, nmaApprover, iishfItcApprover);
                    }

                    if (iishfItcApprover != null)
                    {
                        AddIishfApprovals(approvalsList, titleEvents, ageGroup, nmaApprover, iishfItcApprover);

                    }
                }
            }


            return approvalsList;
        }

        private void AddNmaApprovals(List<ITCApproval> approvalsList,
            IEnumerable<IPublishedContent> titleEvents,
            IPublishedContent? nma,
            IPublishedContent ageGroup,
            IMember? nmaApprover,
            IMember? iishfItcApprover)
        {
            approvalsList.AddRange(from iishfEvent in titleEvents
                                   let eventTeams = iishfEvent.Children().Where(x => x.ContentType.Alias == "team").ToList()
                                   from team in eventTeams
                                   where team.Value<Guid>("nmaKey") == nma.Key
                                   && nma.Name == nmaApprover.GetValue<string>("nationalMemberAssosiciation")
                                   let itcSubmittionDate = team.Value<DateTime>("iTCSubmissionDate")
                                   let itcSubmittedBy = team.Value<IPublishedContent>("iTCSubmittedBy")
                                   let submittedBy = itcSubmittedBy?.Name ?? string.Empty
                                   let nmaApproverDate = team.Value<DateTime>("nMAApprovedDate")
                                   let nmaApprovedBy = team.Value<IPublishedContent>("iTCNMAApprover")
                                   let nmaItcApprover = nmaApprovedBy?.Name ?? string.Empty
                                   let iishfApproveDate = team.Value<DateTime>("iISHFApprovedDate")
                                   let iishApprover = team.Value<IPublishedContent>("iISHFITCApprover")
                                   let iishfApprover = iishApprover?.Name ?? string.Empty
                                   select new ITCApproval
                                   {
                                       EventId = iishfEvent.Key,
                                       EventName = ageGroup.Name,
                                       EventStartDate = team.Parent.Value<DateTime>("eventStartDate"),
                                       EventEndDate = team.Parent.Value<DateTime>("eventEndDate"),
                                       EventTeamId = team.Key,
                                       TeamName = team.Name,
                                       ITCSubmittedDate = itcSubmittionDate,
                                       ITCSubmittedBy = submittedBy,
                                       NMAApproveDate = nmaApproverDate,
                                       NMAApprover = nmaItcApprover,
                                       IISHFApproveDate = iishfApproveDate,
                                       IISHFApprover = iishfApprover,
                                       CanApprove = CanApprove(nmaApprover, iishfItcApprover, team)
                                   });
        }

        //private void AddIishfApprovals(
        //    List<ITCApproval> approvalsList,
        //    IEnumerable<IPublishedContent> titleEvents,
        //    IPublishedContent ageGroup,
        //    IMember? nmaApprover,
        //    IMember? iishfItcApprover)
        //{
        //    approvalsList.AddRange(from iishfEvent in titleEvents
        //                           let eventTeams = iishfEvent.Children().Where(x => x.ContentType.Alias == "team").ToList()
        //                           from team in eventTeams
        //                           where team.Value<IPublishedContent>("iTCSubmittedBy") != null

        //                           let itcSubmittionDate = team.Value<DateTime>("iTCSubmissionDate")
        //                           let itcSubmittedBy = team.Value<IPublishedContent>("iTCSubmittedBy")
        //                           let submittedBy = itcSubmittedBy?.Name ?? string.Empty
        //                           let nmaApproverDate = team.Value<DateTime>("nMAApprovedDate")
        //                           let nmaApprovedBy = team.Value<IPublishedContent>("iTCNMAApprover")
        //                           let nmaItcApprover = nmaApprovedBy?.Name ?? string.Empty
        //                           let iishfApproveDate = team.Value<DateTime>("iISHFApprovedDate")
        //                           let iishApprover = team.Value<IPublishedContent>("iISHFITCApprover")
        //                           let iishfApprover = iishApprover?.Name ?? string.Empty
        //                           select new ITCApproval
        //                           {
        //                               EventId = iishfEvent.Key,
        //                               EventName = ageGroup.Name,
        //                               EventStartDate = team.Parent.Value<DateTime>("eventStartDate"),
        //                               EventEndDate = team.Parent.Value<DateTime>("eventEndDate"),
        //                               EventTeamId = team.Key,
        //                               TeamName = team.Name,
        //                               ITCSubmittedDate = itcSubmittionDate,
        //                               ITCSubmittedBy = submittedBy,
        //                               NMAApproveDate = nmaApproverDate,
        //                               NMAApprover = nmaItcApprover,
        //                               IISHFApproveDate = iishfApproveDate,
        //                               IISHFApprover = iishfApprover,
        //                               CanApprove = CanApprove(nmaApprover, iishfItcApprover, team)
        //                           });
        //}

        private void AddIishfApprovals(
            List<ITCApproval> approvalsList,
            IEnumerable<IPublishedContent> titleEvents,
            IPublishedContent ageGroup,
            IMember? nmaApprover,
            IMember? iishfItcApprover)
        {
            foreach (var iishfEvent in titleEvents)
            {
                var eventTeams = GetEventTeams(iishfEvent);
                foreach (var team in eventTeams)
                {
                    if (IsTeamSubmitted(team))
                    {
                        var approval = CreateApproval(iishfEvent, team, ageGroup, nmaApprover, iishfItcApprover);
                        approvalsList.Add(approval);
                    }
                }
            }
        }

        private List<IPublishedContent> GetEventTeams(IPublishedContent iishfEvent)
        {
            return iishfEvent.Children().Where(x => x.ContentType.Alias == "team").ToList();
        }

        private bool IsTeamSubmitted(IPublishedContent team)
        {
            return team.Value<IPublishedContent>("ITcsubmittedBy") != null;
        }

        private ITCApproval CreateApproval(IPublishedContent iishfEvent, IPublishedContent team, IPublishedContent ageGroup, IMember? nmaApprover, IMember? iishfItcApprover)
        {
            return new ITCApproval
            {
                EventId = iishfEvent.Key,
                EventName = ageGroup.Name,
                EventStartDate = team.Parent.Value<DateTime>("eventStartDate"),
                EventEndDate = team.Parent.Value<DateTime>("eventEndDate"),
                EventTeamId = team.Key,
                TeamName = team.Name,
                ITCSubmittedDate = team.Value<DateTime>("iTCSubmissionDate"),
                ITCSubmittedBy = team.Value<IPublishedContent>("iTCSubmittedBy")?.Name ?? string.Empty,
                NMAApproveDate = team.Value<DateTime>("nMAApprovedDate"),
                NMAApprover = team.Value<IPublishedContent>("iTCNMAApprover")?.Name ?? string.Empty,
                IISHFApproveDate = team.Value<DateTime>("iISHFApprovedDate"),
                IISHFApprover = team.Value<IPublishedContent>("iISHFITCApprover")?.Name ?? string.Empty,
                CanApprove = CanApprove(nmaApprover, iishfItcApprover, team)
            };
        }


        private bool CanApprove(IMember nmaApprover, IMember iishfItcApprover, IPublishedContent team)
        {
            // If approved can not be approved again
            if (nmaApprover != null && (team.Value<DateTime>("nMAApprovedDate") != null &&
                                        team.Value<DateTime>("nMAApprovedDate") > DateTime.MinValue &&
                                        team.Value<DateTime>("nMAApprovedDate") > DateTime.Parse("1/1/1753")))
            {
                return false;
            }

            if (iishfItcApprover != null && (team.Value<DateTime>("iISHFApprovedDate") != null &&
                                             team.Value<DateTime>("iISHFApprovedDate") > DateTime.MinValue &&
                                             team.Value<DateTime>("iISHFApprovedDate") > DateTime.Parse("1/1/1753")))
            {
                return false;
            }

            return true;
        }
    }
}
