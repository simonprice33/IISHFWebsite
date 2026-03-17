using System.Runtime.InteropServices.JavaScript;
using IISHF.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Security;
using IUserService = IISHF.Core.Interfaces.IUserService;

namespace IISHF.Core.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LookUpController : ControllerBase
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IMemberSignInManager _signInManager;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;
        private readonly IUserService _userService;
        private readonly ITwoFactorLoginService _twoFactorLoginService;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LookUpController(
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
        public async Task<IActionResult> GetNmaInformation()
        {
            var nmaPublishedContent = GetContent("nationalMemberAssociation");

            var responseObject = nmaPublishedContent.Select(x => new
            {
                x.Id,
                x.Name,
                x.Key
            }).ToList();

            return Ok(responseObject.OrderBy(x => x.Name));
        }

        [HttpGet]
        [Route("search-clubs")]
        public async Task<IActionResult> GetNmaClubs(Guid nmaKey, string searchText = "")
        {
            // ToDo
            // Adjust this to get the latest NMA reporting year
            // Ok for immediate use, will be an issue for next season
            // When carried forward

            var foundList = GetContent("club")
                .Where(x => x.Parent.Parent.Key == nmaKey &&
                            x.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToList();

            var year = DateTime.Now.AddYears(-1).Year.ToString();

            var clubs = foundList
                .Where(x => x.Parent.Name == year)
                .Select(x => new
                {
                    Id = x.Id,
                    Key = x.Key,
                    Name = x.Name,
                })
                .ToList();
            return Ok(clubs);
        }

        [HttpGet]
        [Route("search-club-teams-by-key")]
        public async Task<IActionResult> GetClubTeams(Guid clubKey)
        {
            var teams = GetContent("clubTeam")
                .Where(x => x.Parent.Parent.Key == clubKey)
                .Select(x => new
                {
                    Id = x.Id,
                    Key = x.Key,
                    Name = x.Name,
                    AgeGroup = x.Parent.Name
                })
                .ToList();
            return Ok(teams.OrderBy(x => x.AgeGroup));
        }

        private List<IPublishedContent> GetContent(string type)
        {
            var content = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType(type)
                .ToList();
            return content;
        }
    }
}
