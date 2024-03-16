using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
{
    public class UserInvitationService : IUserInvitationService
    {
        private readonly INMAService _nmaService;
        private readonly ITeamService _teamService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly IContentService _contentService;
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IEmailService _emailService;

        public UserInvitationService(
            INMAService nmaService,
            ITeamService teamService,
            IHttpContextAccessor httpContextAccessor,
            IMemberService memberService,
            IMemberManager memberManager,
            IContentService contentService,
            IPublishedContentQuery contentQuery,
            IEmailService emailService)
        {
            _nmaService = nmaService;
            _teamService = teamService;
            _httpContextAccessor = httpContextAccessor;
            _memberService = memberService;
            _memberManager = memberManager;
            _contentService = contentService;
            _contentQuery = contentQuery;
            _emailService = emailService;
        }

        public async Task InviteUser(UserInvitationModel model)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var member = _memberService.GetByKey(user.Key);

            var memberInvitation = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("memberInvitations").FirstOrDefault();

            var invitation = _contentService.Create(model.Name, memberInvitation.Id, "memberInvitation", member.Id);
            invitation.SetValue("inviteeName", model.Name);
            invitation.SetValue("emailAddress", model.Email);
            invitation.SetValue("isNma", model.IsNma);
            invitation.SetValue("isClubContact", model.IsClubContact);
            invitation.SetValue("teamAdministrator", model.IsTeamAdmin);
            invitation.SetValue("isIISHF", model.IsIISHF);
            invitation.SetValue("nMAKey", model.NmaKey);
            invitation.SetValue("clubKey", model.ClubKey);

            _contentService.SaveAndPublish(invitation);

            if (model.IsNma)
            {
                foreach (var nmaRole in model.NmaRoles)
                {
                    var role = _contentService.Create(nmaRole, invitation.Id, "memberInvitionNmaRole", member.Id);
                    role.SetValue("role", role);
                    _contentService.SaveAndPublish(role);
                }

                if (!string.IsNullOrWhiteSpace(model.OtherNmaRole))
                {
                    var roles = model.OtherNmaRole.Split(',');
                    foreach (var nmaRole in roles)
                    {
                        var role = _contentService.Create(nmaRole.Trim(), invitation.Id, "memberInvitionNmaRole", member.Id);
                        role.SetValue("role", nmaRole.Trim());
                        _contentService.SaveAndPublish(role);
                    }
                }
            }

            if (model.IsTeamAdmin)
            {
                foreach (var teamKey in model.clubTeams)
                {
                    var team = _contentQuery.Content(teamKey);
                    var toManage = _contentService.Create(team.Name, invitation.Id, "memberInvitationTeam", member.Id);
                    toManage.SetValue("teamKey", teamKey);
                    _contentService.SaveAndPublish(toManage);
                }
            }
            
            var queryString = $"id={invitation.Key}";

            var template = "MemberRegistrationInvitation.html";

            var protocol = _httpContextAccessor.HttpContext.Request.Scheme;
            var baseUrl = _httpContextAccessor.HttpContext.Request.Host;
            var route = "register";

            var registerUrl = new Uri($"{protocol}://{baseUrl}/{route}?{queryString}");

            await _emailService.SendUserInvitation(member, model.Email, model.Name, registerUrl, template, "IISHF Website User Invitation");
        }
    }
}
