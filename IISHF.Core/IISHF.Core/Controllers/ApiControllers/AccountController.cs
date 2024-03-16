using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Filters;
using IUserService = IISHF.Core.Interfaces.IUserService;

namespace IISHF.Core.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly ITournamentService _tournamentService;
        private readonly IRosterService _rosterService;
        private readonly IEventResultsService _eventResultsService;
        private readonly IUserService _userService;
        private readonly IUserInvitationService _userInvitationService;
        private readonly ILogger<EventsController> _logger;

        public AccountController(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            ITournamentService tournamentService,
            IRosterService rosterService,
            IEventResultsService eventResultsService,
            IUserService userService,
            IUserInvitationService userInvitationService,
            ILogger<EventsController> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
            _tournamentService = tournamentService;
            _rosterService = rosterService;
            _eventResultsService = eventResultsService;
            _userService = userService;
            _userInvitationService = userInvitationService;
            _logger = logger;
        }

        [HttpPut]
        [Route("update-details")]
        [UmbracoMemberAuthorize]
        public async Task<IActionResult> UpdateDetails(AccountViewModel model)
        {
            var member = await _memberManager.GetCurrentMemberAsync();

            if (member == null)
            {
                return Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(model.Password)
                && !string.IsNullOrWhiteSpace(model.ConfirmPassword)
                && model.Password.Equals(model.ConfirmPassword))
            {
                var result = _userService.UpdatePassword(member.Email, model.Password);
            }

            if (member.Name != model.Name)
            {
            }

            if (member.Email != model.Email)
            {
            }

            return NoContent();
        }

        [HttpPost]
        [Route("invite-user")]
        public async Task<AcceptedResult> InviteUser(UserInvitationModel model)
        {
            await _userInvitationService.InviteUser(model);

            return Accepted();
        }
    }
}
