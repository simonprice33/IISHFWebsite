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
using Umbraco.Cms.Web.Common.ActionsResults;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Models;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;

namespace IISHFTest.Core.Controllers.SurfaceControllers
{
    public class UserController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IMemberSignInManager _signInManager;
        private readonly IMemberManager _memberManager;
        private readonly ITwoFactorLoginService _twoFactorLoginService;

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
            ITwoFactorLoginService twoFactorLoginService)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _contentQuery = contentQuery;
            _signInManager = signInManager;
            _memberManager = memberManager;
            _twoFactorLoginService = twoFactorLoginService;
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
                return RedirectToCurrentUmbracoUrl();
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
