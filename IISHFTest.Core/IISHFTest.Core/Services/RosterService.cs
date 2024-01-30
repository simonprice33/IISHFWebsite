using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

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
        
        public RosterMembers UpsertRosterMembers(RosterMembers model, IPublishedContent team)
        {
            foreach (var rosterMember in model.ItcRosterMembers)
            {
                if (rosterMember.Id == 0)
                {
                    var newRosterMember = _contentService.Create(rosterMember.PlayerName, team.Id, "roster");
                    rosterMember.Id = SetRosterMemberValues(team, rosterMember, newRosterMember);
                }
                else
                {
                    var umbracoRosteredMember = _contentService.GetById(rosterMember.Id);
                    SetRosterMemberValues(team, rosterMember, umbracoRosteredMember);
                }
            }

            return model;
        }

        public IPublishedContent FindRosterMemberById(int playerId, IPublishedContent team)
        {
            var roster = team.Children().Where(x => x.ContentType.Alias == "roster").ToList();
            var rosterMember = roster.FirstOrDefault(x => x.Id == playerId);
            return rosterMember;
        }

        public void DeleteRosteredPlayer(int playerId)
        {
            var umbracoRosteredMember = _contentService.GetById(playerId);
            _contentService.Delete(umbracoRosteredMember);
        }

        private int SetRosterMemberValues(IPublishedContent team, RosterMember rosterMember, IContent umbracoRosteredMember)
        {
            umbracoRosteredMember?.SetValue("playerName", rosterMember.PlayerName);
            umbracoRosteredMember?.SetValue("licenseNumber", rosterMember.License);
            umbracoRosteredMember?.SetValue("isBenchOfficial", rosterMember.IsBenchOfficial);
            umbracoRosteredMember?.SetValue("role", rosterMember.Role);
            umbracoRosteredMember?.SetValue("jerseyNumber", rosterMember.JerseyNumber);
            umbracoRosteredMember?.SetValue("dateOfBirth", rosterMember.DateOfBirth.ToString("yyyy-MM-dd"));
            umbracoRosteredMember?.SetValue("nmaCheck", rosterMember.NmaCheck);
            umbracoRosteredMember?.SetValue("iishfCheck", rosterMember.IISHFCheck);
            umbracoRosteredMember?.SetValue("comments", rosterMember.Comments);

            _contentService.SaveAndPublish(umbracoRosteredMember);
            return umbracoRosteredMember.Id;
        }
    }
}
