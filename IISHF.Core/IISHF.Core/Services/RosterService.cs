using System.Globalization;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace IISHF.Core.Services
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

        public async Task<RosterMembers> UpsertRosterMembers(RosterMembers model, IPublishedContent team)
        {
            return await Task.Run(async () =>
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
            });
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
            var dob = rosterMember.DateOfBirth != null
                ? rosterMember.DateOfBirth.Value.ToString("yyyy-MM-dd")
                : DateTime.Parse("1/1/1753").ToString("yyyy-MM-dd"); // Umbraco default date

            umbracoRosteredMember?.SetValue("playerName", rosterMember.PlayerName);
            umbracoRosteredMember?.SetValue("firstName", rosterMember.FirstName);
            umbracoRosteredMember?.SetValue("lastName", rosterMember.LastName);
            umbracoRosteredMember?.SetValue("gender", rosterMember.Gender);
            umbracoRosteredMember?.SetValue("licenseNumber", rosterMember.License);
            umbracoRosteredMember?.SetValue("isBenchOfficial", rosterMember.IsBenchOfficial);
            umbracoRosteredMember?.SetValue("nationality", rosterMember.Nationality);
            umbracoRosteredMember?.SetValue("iso3", GetCountryIso3Code(rosterMember.Nationality));
            umbracoRosteredMember?.SetValue("role", rosterMember.Role);
            umbracoRosteredMember?.SetValue("jerseyNumber", rosterMember.JerseyNumber);
            umbracoRosteredMember?.SetValue("dateOfBirth", dob);
            umbracoRosteredMember?.SetValue("nmaCheck", rosterMember.NmaCheck);
            umbracoRosteredMember?.SetValue("iishfCheck", rosterMember.IISHFCheck);
            umbracoRosteredMember?.SetValue("comments", rosterMember.Comments);
            umbracoRosteredMember?.SetValue("isGuest", rosterMember.IsGuest);

            _contentService.SaveAndPublish(umbracoRosteredMember);
            return umbracoRosteredMember.Id;
        }

        private string GetCountryIso3Code(string countryName)
        {
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                RegionInfo region = new RegionInfo(ci.Name);
                if (region.EnglishName.Equals(countryName, StringComparison.OrdinalIgnoreCase))
                {
                    return region.ThreeLetterISORegionName;
                }
            }

            return null;
        }
    }
}
