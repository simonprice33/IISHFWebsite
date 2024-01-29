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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
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
using IUserService = IISHFTest.Core.Interfaces.IUserService;

namespace IISHFTest.Core.Controllers.SurfaceControllers
{
    public class RegisterController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberManager _memberManager;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services, AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery,
            IHttpContextAccessor httpContextAccessor,
            IMemberManager memberManager,
            IEmailService emailService,
            IUserService userService,
            ILogger<RegisterController> logger)
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
            _userService = userService;
            _logger = logger;
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
            var member = _userService.GetMembersByPropertyValue(token.ToString(), "emailVerificationToken");

            if (member != null)
            {
                try
                {
                    var key = _userService.GetVerificationKey(member, token);
                    TempData["status"] = "OK";
                    return RedirectToUmbracoPage(key);
                }
                catch (MemberAccessException mEx)
                {
                    ModelState.AddModelError("Verification Error", "Verification code has already been validated.");
                    return CurrentUmbracoPage();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to verify user");
                    throw;
                }
            }

            // need to return user to home page and open a modal saying that didnt work. 
            _logger.LogWarning("Unable to find user with token {token}", token);
            ModelState.AddModelError("Verification Error", "We have been unable to validate your account");
            TempData["status"] = "Failed";
            return CurrentUmbracoPage();
        }

        [HttpPost]
        public async Task<IActionResult> HandleRegister(RegisterViewModel model)
        {
            var existingMember = Services.MemberService.GetByEmail(model.EmailAddress);

            if (existingMember != null)
            {
                ModelState.AddModelError("Registration Error", "User with this email address already exists");
                return CurrentUmbracoPage();
            }

            await _userService.RegisterUser(model);

            TempData["status"] = "Member Registered Ok";

            return CurrentUmbracoPage();
        }
    }
}
