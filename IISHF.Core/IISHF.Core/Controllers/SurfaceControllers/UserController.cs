using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.ActionsResults;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Models;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;
using IUserService = IISHF.Core.Interfaces.IUserService;

namespace IISHF.Core.Controllers.SurfaceControllers
{
    public class UserController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IMemberSignInManager _signInManager;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;
        private readonly IUserService _userService;
        private readonly ITwoFactorLoginService _twoFactorLoginService;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserController(
            IPublishedContentQuery contentQuery,
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberSignInManager signInManager,
            IMemberManager memberManager,
            IMemberService memberService,
            IUserService userService,
            ITwoFactorLoginService twoFactorLoginService,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _contentQuery = contentQuery;
            _signInManager = signInManager;
            _memberManager = memberManager;
            _memberService = memberService;
            _userService = userService;
            _twoFactorLoginService = twoFactorLoginService;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public ActionResult RenderForgotPassword()
        {
            return PartialView("~/Views/Partials/Members/ForgotPassword.cshtml", new ForgotPasswordResetRequestModel());
        }
        
        [HttpPost]
        public async Task<IActionResult> HandlePasswordResetRequest(ForgotPasswordResetRequestModel model)
        {
            TempData["Status"] = "OK";

            if (string.IsNullOrWhiteSpace(model.EmailAddress))
            {
                TempData["Status"] = "OK";
                return CurrentUmbracoPage();
            }

            var token = Guid.NewGuid();

            var member = Services.MemberService.GetByUsername(model.EmailAddress);

            await _emailService.SendRegistrationConfirmation(new Member()
            {
                Name = member.Name,
                EmailAddress = model.EmailAddress,
                Token = token,
                TokenUrl = new Uri($"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/reset-password?token={token}")
            }, "PasswordReset.html", "IISHF Password Reset Request");

            member.SetValue("resetExpiryDate", DateTime.UtcNow.AddHours(1));
            member.SetValue("resetToken", token);

            Services.MemberService.Save(member);

            TempData["Status"] = "OK";
            return CurrentUmbracoPage();
        }

        [HttpPost]
        public async Task<IActionResult> HandlePasswordResetAction(PasswordResetRequestModel model)
        {
            var queryToken = _httpContextAccessor.HttpContext.Request.Query["token"].ToString();
            var member = Services.MemberService.GetMembersByPropertyValue("resetToken", queryToken).FirstOrDefault();
            if (member == null)
            {
                TempData["Status"] = "Failed";
                ModelState.Clear();
                ModelState.AddModelError("Reset Token Not Found", "The token you have used is not valid");
                return CurrentUmbracoPage();
            }

            var tokenExpirationDate = member.GetValue<DateTime>("resetExpiryDate");
            if (DateTime.UtcNow > tokenExpirationDate)
            {
                ModelState.Clear();
                ModelState.AddModelError("Reset Token Not Found", "The token you have used is no longer valid");
                return CurrentUmbracoPage();
            }

            var result = await _userService.UpdatePassword(member.Email, model.Password);

            if (result.Succeeded)
            {
                member.SetValue("resetExpiryDate", null);
                member.SetValue("resetToken", null);

                Services.MemberService.Save(member);

                var rootContent = _contentQuery.ContentAtRoot().ToList();

                var loginContent = rootContent
                    .FirstOrDefault(x => x.Name == "Home")!.Children()?
                    .FirstOrDefault(x => x.Name == "Login");

                TempData["status"] = "OK";
                return RedirectToUmbracoPage(loginContent.Key);
            }

            TempData["Status"] = "Failed";
            var error = result.Errors.FirstOrDefault();
            ModelState.AddModelError(error.Code, error.Description);
            return CurrentUmbracoPage();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<IActionResult> HandleLogin([Bind(Prefix = "loginModel")] LoginModel model)
        {
            if (ModelState.IsValid == false)
            {
                return CurrentUmbracoPage();
            }

            MergeRouteValuesToModel(model);

            var member = Services.MemberService.GetByUsername(model.Username);

            if (member != null)
            {
                var verified = member.GetValue<bool>("emailVerified");
                if (!verified)
                {
                    ModelState.AddModelError("Verification Error", "Your email address has not yet been validated. Please check your inbox for an email from noreply@iishf.com for your account activation. <br/><br/> If you cannot find it look inside your spam \\ junk folder. If you still do not have this please email <a href=\"mailto:webmaster@iishf.com\">webmaster@iishf.com</a> with your username \\ email address and we will do out best to help you.");
                    return CurrentUmbracoPage();
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Username, model.Password, model.RememberMe, true);

            if (result.Succeeded)
            {
                TempData["LoginSuccess"] = true;

                // If there is a specified path to redirect to then use it.
                if (model.RedirectUrl.IsNullOrWhiteSpace() == false)
                {
                    // Validate the redirect URL.
                    // If it's not a local URL we'll redirect to the root of the current site.
                    return Redirect(Url.IsLocalUrl(model.RedirectUrl)
                        ? model.RedirectUrl
                        : CurrentPage!.AncestorOrSelf(1)!.Url(PublishedUrlProvider));
                }

                // Redirect to current URL by default.
                // This is different from the current 'page' because when using Public Access the current page
                // will be the login page, but the URL will be on the requested page so that's where we need
                // to redirect too.
                return Redirect("/my-account");
            }

            if (result.RequiresTwoFactor)
            {
                MemberIdentityUser? attemptedUser = await _memberManager.FindByNameAsync(model.Username);
                if (attemptedUser == null!)
                {
                    return new ValidationErrorResult(
                        $"No local member found for username {model.Username}");
                }

                IEnumerable<string> providerNames =
                    await _twoFactorLoginService.GetEnabledTwoFactorProviderNamesAsync(attemptedUser.Key);
                ViewData.SetTwoFactorProviderNames(providerNames);
            }
            else if (result.IsLockedOut)
            {
                ModelState.AddModelError("loginModel", "Member is locked out");
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError("loginModel", "Member is not allowed");
            }
            else
            {
                ModelState.AddModelError("loginModel", "Invalid username or password");
            }

            return CurrentUmbracoPage();
        }

        /// <summary>
        ///     We pass in values via encrypted route values so they cannot be tampered with and merge them into the model for use
        /// </summary>
        /// <param name="model"></param>
        private void MergeRouteValuesToModel(LoginModel model)
        {
            if (RouteData.Values.TryGetValue(nameof(LoginModel.RedirectUrl), out var redirectUrl) && redirectUrl != null)
            {
                model.RedirectUrl = redirectUrl.ToString();
            }
        }
    }
}
