using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using IISHF.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using IUserService = IISHF.Core.Interfaces.IUserService;

namespace IISHF.Core.Controllers.SurfaceControllers
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
            string userType = _httpContextAccessor?.HttpContext?.Request?.Query["type"].ToString();
            string converted = string.Empty;

            if (!string.IsNullOrWhiteSpace(userType))
            {
                converted = userType.DecodeBase64MultipleTimes();
            }

            var model = new RegisterViewModel()
            {
                InvitedAccountType = "test value"
            };
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

                    // ToDo - If Member is NMA User or Team Administrator 
                    // Trigger password reset 
                    // Do not send the email 
                    // redirect to password set page

                    //// Might not need this as will be setting these values in registation page 
                    //var redirect = member.GetValue<bool>("isNMA") || member.GetValue<bool>("teamAdministrator") ||
                    //               member.GetValue<bool>("isIISHF");

                    var key = _userService.GetVerificationKey(member, token, false);
                    TempData["status"] = "OK";
                    return RedirectToUmbracoPage(key);
                }
                catch (MemberAccessException)
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
