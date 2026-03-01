using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.Security;
using IMediaService = IISHF.Core.Interfaces.IMediaService;

namespace IISHF.Web.Controllers
{
    [Authorize]
    [Route("umbraco/api/accountusers")]
    public sealed class AccountUsersController : UmbracoApiController
    {
        private readonly IMemberManager _memberManager; // current member only
        private readonly IMemberService _memberService; // for reading current member properties (permission gate)
        private readonly IMemberGroupService _memberGroupService; // list groups
        private readonly UserManager<MemberIdentityUser> _userManager; // IMPORTANT: query/update members
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMediaService _iishfMediaService;
        private readonly IEmailService _emailService;

        public AccountUsersController(
            IMemberManager memberManager,
            IMemberService memberService,
            IMemberGroupService memberGroupService,
            UserManager<MemberIdentityUser> userManager,
            IEmailSender emailSender,
            IHttpContextAccessor httpContextAccessor,
            IMediaService iishfMediaService,
            IEmailService emailService)
        {
            _memberManager = memberManager;
            _memberService = memberService;
            _memberGroupService = memberGroupService;
            _userManager = userManager;
            _emailSender = emailSender;

            _httpContextAccessor = httpContextAccessor;
            _iishfMediaService = iishfMediaService;
            _emailService = emailService;
        }

        // -----------------------------
        // Permission gate
        // -----------------------------
        private async Task EnsureCanManageUsersAsync()
        {
            var current = await _memberManager.GetCurrentMemberAsync();
            if (current == null) throw new UnauthorizedAccessException("Not logged in.");

            if (!int.TryParse(current.Id, out var currentId))
                throw new UnauthorizedAccessException("Invalid member id.");

            var member = _memberService.GetById(currentId);
            if (member == null) throw new UnauthorizedAccessException("Member not found.");

            // Supports both common alias casings
            var canManage =
                member.GetValue<bool>("canManageUsers") ||
                member.GetValue<bool>("CanManageUsers") ||
                member.GetValue<bool>("canManageRoles") ||
                member.GetValue<bool>("CanManageRoles");

            if (!canManage)
                throw new UnauthorizedAccessException("Forbidden.");
        }

        // -----------------------------
        // GET /umbraco/api/accountusers/member-groups
        // -----------------------------
        [HttpGet("member-groups")]
        public async Task<IActionResult> MemberGroups()
        {
            await EnsureCanManageUsersAsync();

            var roles = _memberGroupService.GetAll()
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x)
                .ToArray();

            return Ok(new { roles });
        }

        // -----------------------------
        // GET /umbraco/api/accountusers/members?page=1&pageSize=25&query=...
        // -----------------------------
        [HttpGet("members")]
        public async Task<IActionResult> Members([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? query = null)
        {
            await EnsureCanManageUsersAsync();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            // Umbraco paging is 0-based
            var pageIndex = page - 1;

            // Pull a page of members from Umbraco (this is the supported way)
            // NOTE: 'out total' type is long in Umbraco
            long total;
            var members = _memberService.GetAll(pageIndex, pageSize, out total);

            // If you want filtering by login/email, you can do it in-memory on the page.
            // (If you need full DB-side search, we can add a dedicated search path next.)
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();

                var filtered = members.Where(m =>
                        (!string.IsNullOrWhiteSpace(m.Username) && m.Username.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(m.Email) && m.Email.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // Since we're filtering after paging, update totals to match what's returned.
                // If you want accurate totals for filtered search across *all* members, tell me and I'll implement a proper search endpoint.
                total = filtered.Count;
                members = filtered;
            }

            var items = new List<MemberListItemDto>();

            foreach (var m in members)
            {
                // Member groups (roles)
                var roles = _memberService.GetAllRoles(m.Id).OrderBy(x => x).ToArray();

                items.Add(new MemberListItemDto
                {
                    UserId = m.Id.ToString(),
                    UserName = m.Username ?? "",
                    Email = m.Email ?? "",
                    Roles = roles,
                    IsApproved = m.IsApproved,
                    IsLockedOut = m.IsLockedOut
                });
            }

            return Ok(new
            {
                total,
                items
            });
        }

        // -----------------------------
        // PUT /umbraco/api/accountusers/members
        // body: { userId, isApproved, isLockedOut, roles[] }
        // -----------------------------
        [HttpPut("members")]
        public async Task<IActionResult> UpdateMember([FromBody] UpdateMemberRequest request)
        {
            await EnsureCanManageUsersAsync();

            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest("Missing userId.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return NotFound("User not found.");

            // Approved flag (Umbraco member identity user)
            user.IsApproved = request.IsApproved;

            // Lockout handling
            if (request.IsLockedOut)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            else
                await _userManager.SetLockoutEndDateAsync(user, null);

            // Roles (Member Groups)
            var existingRoles = await _userManager.GetRolesAsync(user);

            var desired = (request.Roles ?? Array.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var toRemove = existingRoles.Where(r => !desired.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();
            var toAdd = desired.Where(r => !existingRoles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToArray();

            if (toRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!removeResult.Succeeded)
                    return BadRequest(string.Join("; ", removeResult.Errors.Select(e => e.Description)));
            }

            if (toAdd.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, toAdd);
                if (!addResult.Succeeded)
                    return BadRequest(string.Join("; ", addResult.Errors.Select(e => e.Description)));
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

            return Ok();
        }

        // -----------------------------
        // POST /umbraco/api/accountusers/members/{userId}/password-reset
        // -----------------------------
        [HttpPost("members/{userId}/password-reset")]
        public async Task<IActionResult> StartPasswordReset([FromRoute] string userId)
        {
            await EnsureCanManageUsersAsync();

            // Find the identity user (gives us email/username)
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // Your existing reset flow uses MemberService.GetByUsername(email)
            // so prefer Email, fallback to UserName.
            var key = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.UserName;
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("User has no email/username to initiate password reset.");

            var member = _memberService.GetByUsername(key);
            if (member == null)
                return NotFound("Member not found for password reset.");

            // Match your existing implementation
            var token = Guid.NewGuid();

            var template = _iishfMediaService.GetMediaTemplate("PasswordReset");
            var templateUri = _iishfMediaService.GetTemplateUrl(template);

            var http = _httpContextAccessor.HttpContext;
            if (http == null)
                return StatusCode(500, "HttpContext not available.");

            var tokenUrl = new Uri($"{http.Request.Scheme}://{http.Request.Host}/reset-password?token={token}");

            await _emailService.SendRegistrationConfirmation(new Member()
            {
                Name = member.Name,
                EmailAddress = key,
                Token = token,
                TokenUrl = tokenUrl
            }, templateUri, "IISHF Password Reset Request");

            member.SetValue("resetExpiryDate", DateTime.UtcNow.AddHours(1));
            member.SetValue("resetToken", token);

            _memberService.Save(member);

            return Ok(new { sent = true });
        }

        private sealed class MemberListItemDto
        {
            public string UserId { get; set; } = "";
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public string[] Roles { get; set; } = Array.Empty<string>();
            public bool IsApproved { get; set; }
            public bool IsLockedOut { get; set; }
        }

        public sealed class UpdateMemberRequest
        {
            [Required]
            public string UserId { get; set; } = "";

            public bool IsApproved { get; set; }
            public bool IsLockedOut { get; set; }
            public string[]? Roles { get; set; }
        }
    }
}