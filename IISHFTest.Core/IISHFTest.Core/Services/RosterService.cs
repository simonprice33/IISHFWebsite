using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace IISHFTest.Core.Services
{
    public class RosterService : IRosterService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly ILogger<RosterService> _logger;

        public RosterService(IPublishedContentQuery contentQuery,
            IContentService contentService,
            ILogger<RosterService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _logger = logger;
        }

        public Task SetRosterForTeamInformation(RosterMembers roster, IPublishedContent team)
        {
            throw new NotImplementedException();
        }

        public Task UpdateRosterWithItcValues(RosterMembers model, IPublishedContent team)
        {
            foreach (var rosterMember in model.ItcRosterMembers)
            {
                var rosteredMember = _contentService.Create(rosterMember.PlayerName, team.Id, "roster");
                rosteredMember?.SetValue("playerName", rosterMember.PlayerName);
                rosteredMember?.SetValue("licenseNumber", rosterMember.License);
                rosteredMember?.SetValue("isBenchOfficial", rosterMember.IsBenchOfficial);
                rosteredMember?.SetValue("role", rosterMember.Role);
                rosteredMember?.SetValue("jerseyNumber", rosterMember.JerseyNumber);
                rosteredMember?.SetValue("dateOfBirth", rosterMember.DateOfBirth.ToString("yyyy-MM-dd"));
                rosteredMember?.SetValue("nmaCheck", rosterMember.NmaCheck);
                rosteredMember?.SetValue("iishfCheck", rosterMember.IISHFCheck);
                rosteredMember?.SetValue("comments", rosterMember.Comments);

                _contentService.SaveAndPublish(rosteredMember);
            }

            return Task.CompletedTask;
        }
    }
}
