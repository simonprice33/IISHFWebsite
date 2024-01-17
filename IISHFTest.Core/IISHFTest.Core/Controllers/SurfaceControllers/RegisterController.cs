using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace IISHFTest.Core.Controllers.SurfaceControllers
{
    public class RegisterController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberManager _memberManager;
        private readonly IEmailService _emailService;

        public RegisterController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services, AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery,
            IHttpContextAccessor httpContextAccessor,
            IMemberManager memberManager,
            IEmailService emailService)
            : base(umbracoContextAccessor,
                databaseFactory,
                services,
                appCaches,
                profilingLogger,
                publishedUrlProvider)
        {
            _contentQuery = contentQuery;
            _httpContextAccessor = httpContextAccessor;
            _memberManager = memberManager;
            _emailService = emailService;
        }

        [HttpGet]
        public ActionResult RenderRegister()
        {
            var model = new RegisterViewModel();
            return PartialView("~/Views/Partials/Members/Register.cshtml", model);
        }

        [HttpGet("verify")]
        public async Task<IActionResult> RenderEmailVerification(VerificationViewModel model, [FromQuery] Guid token)
        {
            var member = Services.MemberService.GetMembersByPropertyValue("emailVerificationToken", token.ToString()).SingleOrDefault();

            if (member != null)
            {
                var verified = member.GetValue<bool>("emailVerified");
                if (verified)
                {
                    ModelState.AddModelError("Verification Error", "Verification code has already been validated.");
                    return CurrentUmbracoPage();
                }

                member.SetValue("emailVerified", true);
                member.SetValue("emailVerificationDate", DateTime.UtcNow);
                Services.MemberService.Save(member);

                var rootContent = _contentQuery.ContentAtRoot().ToList();

                var loginContent = rootContent
                    .FirstOrDefault(x => x.Name == "Home")!.Children()?
                    .FirstOrDefault(x => x.Name == "Login");

                TempData["status"] = "OK";
                return RedirectToUmbracoPage(loginContent.Key);
            }

            // need to return user to home page and open a modal saying that didnt work. 
            ModelState.AddModelError("Verification Error", "We have been unable to validate your email address");
            TempData["status"] = "Failed";
            return CurrentUmbracoPage();
        }

        [HttpPost]
        public async Task<IActionResult> HandleRegister(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Not enough information has been provided to submit your message");
                return CurrentUmbracoPage();

            }

            var existingMember = Services.MemberService.GetByEmail(model.EmailAddress);

            if (existingMember != null)
            {
                ModelState.AddModelError("Registration Error", "User with this email address already exists");
                return CurrentUmbracoPage();
            }

            var identityUser =
                MemberIdentityUser.CreateNew(model.EmailAddress, model.EmailAddress, "Member", true, $"{model.FirstName} {model.LastName}");
            var identityResult = await _memberManager.CreateAsync(
                identityUser,
                model.Password);

            var newMember = Services.MemberService.GetById(int.Parse(identityUser.Id));

            Services.MemberService.AssignRole(newMember.Id, "Standard User");

            var token = Guid.NewGuid();
            newMember.SetValue("emailVerificationToken", token);
            Services.MemberService.Save(newMember);

            await _emailService.SendRegistrationConfirmation(new Member()
            {
                Name = $"{model.FirstName} {model.LastName}",
                EmailAddress = model.EmailAddress,
                Token = token,
                TokenUrl = new Uri($"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/verify?token={token}")
            }, "MemberRegistration.html", "IISHF Membership registration");

            newMember.SetValue("verificationEmailSentDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            Services.MemberService.Save(newMember);

            TempData["status"] = "Member Registered Ok";

            return CurrentUmbracoPage();
        }
    }
}
