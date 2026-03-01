using System.Text.Json;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using IMediaService = IISHF.Core.Interfaces.IMediaService;
using Member = IISHF.Core.Models.Member;

namespace IISHF.Core.Services
{
    public class UserService : Interfaces.IUserService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IUmbracoDatabaseFactory _databaseFactory;
        private readonly ServiceContext _services;
        private readonly AppCaches _appCaches;
        private readonly IProfilingLogger _profilingLogger;
        private readonly IPublishedUrlProvider _publishedUrlProvider;
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberManager _memberManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;
        private readonly IMediaService _iishfMediaService;

        public UserService(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IHttpContextAccessor httpContextAccessor,
            IMemberManager memberManager,
            IEmailService emailService,
            ILogger<UserService> logger,
            IMediaService iishfMediaService
            )
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _databaseFactory = databaseFactory;
            _services = services;
            _appCaches = appCaches;
            _profilingLogger = profilingLogger;
            _publishedUrlProvider = publishedUrlProvider;
            _contentQuery = contentQuery;
            _contentService = contentService;
            _httpContextAccessor = httpContextAccessor;
            _memberManager = memberManager;
            _emailService = emailService;
            _logger = logger;
            _iishfMediaService = iishfMediaService;
        }

        public async Task<IMember> RegisterUser(RegisterViewModel model)
        {
            var identityUser =
                MemberIdentityUser.CreateNew(model.EmailAddress, model.EmailAddress, "Member", true, $"{model.FirstName} {model.LastName}");
            var identityResult = await _memberManager.CreateAsync(
                identityUser,
                model.Password);

            var newMember = _services.MemberService.GetById(int.Parse(identityUser.Id));

            _services.MemberService.AssignRole(newMember.Id, "Standard User");

            var token = Guid.NewGuid();
            newMember.SetValue("emailVerificationToken", token);

            _services.MemberService.Save(newMember);

            var invitation = _contentQuery.Content(model.InvitationKey);

            if (invitation != null)
            {
                SetMemberInvitationValues(model, invitation, newMember);
            }

            var template = _iishfMediaService.GetMediaTemplate("MemberRegistration");
            var templateUri = _iishfMediaService.GetTemplateUrl(template);

            await _emailService.SendRegistrationConfirmation(new Member()
            {
                Name = $"{model.FirstName} {model.LastName}",
                EmailAddress = model.EmailAddress,
                Token = token,
                TokenUrl = new Uri($"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/verify?token={token}")
            }, templateUri, "IISHF Membership registration");

            newMember.SetValue("verificationEmailSentDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            _services.MemberService.Save(newMember);

            return newMember;
        }

        private void SetMemberInvitationValues(RegisterViewModel model, IPublishedContent? invitation, IMember newMember)
        {
            newMember.SetValue("isNMA", invitation.Value<bool>("isNma"));
            newMember.SetValue("isIISHF", invitation.Value<bool>("isIISHF"));
            newMember.SetValue("isClubContact", invitation.Value<bool>("isClubContact"));

            if (invitation.Value<bool>("isClubContact"))
            {
                var club = _contentQuery.Content(invitation.Value<Guid>("clubKey"));
                var clubContent = _contentService.GetById(club.Id);
                clubContent.SetValue("primaryClubContactEmail", invitation.Value<string>("emailAddress"));
                clubContent.SetValue("primaryClubContact", invitation.Value<string>("inviteeName"));
                _contentService.SaveAndPublish(clubContent);
            }

            if (invitation.Value<bool>("teamAdministrator"))
            {

                var ageGroups = new List<string>();
                foreach (var team in invitation.Children().Where(x => x.ContentType.Alias == "memberInvitationTeam"))
                {
                    var invitaionTeam = _contentQuery.Content(team.Value<Guid>("teamKey"));
                    var teamContent = _contentService.GetById(invitaionTeam.Id);
                    teamContent.SetValue("teamContactEmail", invitation.Value<string>("emailAddress"));
                    teamContent.SetValue("teamContactName", invitation.Value<string>("inviteeName"));
                    _contentService.SaveAndPublish(teamContent);

                    var ageGroup = invitaionTeam.Parent.Name;

                    if (ageGroup != null && ageGroup.Contains("Women", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ageGroup = "Women";
                    }

                    ageGroups.Add(ageGroup);
                }

                try
                {
                    var json = JsonSerializer.Serialize(ageGroups);
                    newMember.SetValue("ageGroup", json);
                    newMember.SetValue("teamAdministrator", true);
                    _services.MemberService.Save(newMember);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            if (invitation.Value<bool>("teamAdministrator") || invitation.Value<bool>("isClubContact"))
            {
                var club = _contentQuery.Content(invitation.Value<Guid>("clubKey"));
                newMember.SetValue("clubName", club.Name);
                _services.MemberService.Save(newMember);

            }

            if (invitation.Value<bool>("isIISHF"))
            {
                var nma = _contentQuery.Content(invitation.Value<Guid>("nmaKey"));
                newMember.SetValue("nationalMemberAssosiciation", nma.Name);
                _services.MemberService.Save(newMember);
            }

            if (invitation.Value<bool>("isNma"))
            {
                var nma = _contentQuery.Content(invitation.Value<Guid>("nmaKey"));
                var nmaContact = _contentService.Create($"{model.FirstName} {model.LastName}", nma.Id, "nMAContact");
                nmaContact.SetValue("contactName", $"{model.FirstName} {model.LastName}");
                nmaContact.SetValue("nMAContactEmail", model.EmailAddress);
                var contactRoles = invitation.Children().Where(x => x.ContentType.Alias == "memberInvitionNmaRole");
                nmaContact.SetValue("nmaContactRole", string.Join(", ", contactRoles.Select(x => x.Name).ToList()));
                _contentService.SaveAndPublish(nmaContact);
            }
        }

        public Guid GetVerificationKey(IMember member, Guid token, bool redirectToPasswordReset)
        {
            var verified = member.GetValue<bool>("emailVerified");
            if (verified)
            {
                var exception = new MemberAccessException("Verification code has already been validated.");
                _logger.LogError(exception, "Member {memberId} with verification code {verificationCode} has already been validated", member.Id, token);
                throw exception;
            }

            member.SetValue("emailVerified", true);
            member.SetValue("emailVerificationDate", DateTime.UtcNow);
            _services.MemberService.Save(member);

            var rootContent = _contentQuery.ContentAtRoot().ToList();

            var loginContent = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Login");

            return loginContent.Key;
        }

        public IMember GetMembersByPropertyValue(string token, string property)
        {
            return _services.MemberService.GetMembersByPropertyValue(property, token).SingleOrDefault();
        }

        public async Task<IdentityResult> UpdatePassword(string email, string password)
        {
            var memberIdentity = await _memberManager.FindByEmailAsync(email);
            var resetToken = await _memberManager.GeneratePasswordResetTokenAsync(memberIdentity);
            var result = await _memberManager.ResetPasswordAsync(memberIdentity, resetToken, password);

            return result;
        }
    }
}
