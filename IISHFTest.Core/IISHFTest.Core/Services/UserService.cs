using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using IUserService = IISHFTest.Core.Interfaces.IUserService;
using Member = IISHFTest.Core.Models.Member;
using static Umbraco.Cms.Core.Constants.Conventions;

namespace IISHFTest.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IUmbracoDatabaseFactory _databaseFactory;
        private readonly ServiceContext _services;
        private readonly AppCaches _appCaches;
        private readonly IProfilingLogger _profilingLogger;
        private readonly IPublishedUrlProvider _publishedUrlProvider;
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberManager _memberManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;

        public UserService(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery,
            IHttpContextAccessor httpContextAccessor,
            IMemberManager memberManager,
            IEmailService emailService,
            ILogger<UserService> logger)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _databaseFactory = databaseFactory;
            _services = services;
            _appCaches = appCaches;
            _profilingLogger = profilingLogger;
            _publishedUrlProvider = publishedUrlProvider;
            _contentQuery = contentQuery;
            _httpContextAccessor = httpContextAccessor;
            _memberManager = memberManager;
            _emailService = emailService;
            _logger = logger;
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

            await _emailService.SendRegistrationConfirmation(new Member()
            {
                Name = $"{model.FirstName} {model.LastName}",
                EmailAddress = model.EmailAddress,
                Token = token,
                TokenUrl = new Uri($"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/verify?token={token}")
            }, "MemberRegistration.html", "IISHF Membership registration");

            newMember.SetValue("verificationEmailSentDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            _services.MemberService.Save(newMember);

            return newMember;
        }

        public Guid GetVerificationKey(IMember member, Guid token)
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
    }
}
