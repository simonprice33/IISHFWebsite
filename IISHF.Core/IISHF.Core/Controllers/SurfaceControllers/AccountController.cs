using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace IISHF.Core.Controllers.SurfaceControllers
{
    public class AccountController : SurfaceController
    {
        private readonly IInvitationService _invitationService;
        private readonly IApprovals _approvals;
        private readonly IMemberManager _memberManager;

        public AccountController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IInvitationService invitationService,
            IApprovals approvals,
            IMemberManager memberManager)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _invitationService = invitationService;
            _approvals = approvals;
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

            var approvals = await _approvals.GetApprovalsAsync();

            var model = new AccountViewModel
            {
                Email = member.Email,
                Name = member.Name,
                Username = member.Email,
                Invitations = invitations,
                Approvals = approvals
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
