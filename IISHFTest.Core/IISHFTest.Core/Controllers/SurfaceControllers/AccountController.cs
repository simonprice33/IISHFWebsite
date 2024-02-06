using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace IISHFTest.Core.Controllers.SurfaceControllers
{
    public class AccountController : SurfaceController
    {
        private readonly IInvitationService _invitationService;
        private readonly IMemberManager _memberManager;

        public AccountController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IInvitationService invitationService,
            IMemberManager memberManager)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _invitationService = invitationService;
            _memberManager = memberManager;
        }

        [HttpGet]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> Index()
        {
            var member = await _memberManager.GetCurrentMemberAsync();

            if (member == null)
            {
                return Unauthorized();
            }

            var invitations = _invitationService.GetInvitation(member.Email);

            var model = new AccountViewModel
            {
                Email = member.Email,
                Name = member.Name,
                Username = member.Email,
                Invitations = invitations
            };

            return PartialView("~/Views/Partials/Members/MyAccount.cshtml", model);
        }

        [HttpPost]
        public IActionResult HandleUpdateDetails(AccountViewModel model)
        {
            TempData["Status"] = "Ok";
            return CurrentUmbracoPage();
        }
    }
}
