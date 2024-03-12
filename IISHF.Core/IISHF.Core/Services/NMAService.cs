using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Microsoft.Azure.Amqp.Encoding;
using Umbraco.Cms.Core.Models;
using Lucene.Net.Index;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IISHF.Core.Services
{
    public class NMAService : INMAService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly ILogger<NMAService> _logger;

        public NMAService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            ILogger<NMAService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
            _logger = logger;
        }

        public async Task<IPublishedContent?> GetPublishedContentByKey(Guid key)
        {
            return await Task.Run(async () => _contentQuery.Content(key));
        }

        public async Task<IContent> CreatingNmaReportingYear(int reportingYear, Guid nmaKey)
        {
            return await Task.Run(async () =>
            {
                var yearContent = _contentService.Create(reportingYear.ToString(), nmaKey, "nMAReporting");
                _contentService.SaveAndPublish(yearContent);

                return yearContent;
            });

        }

        public async Task<IContent> AddClub(NmaClub club, int reportingYear, Guid nmaKey)
        {
            var nma = await GetPublishedContentByKey(nmaKey);

            if (nma == null)
            {
                return null;
            }

            var nmaReportingYearPublishedContent = nma.Children().FirstOrDefault(x => x.ContentType.Alias == "nMAReporting" && x.Name == reportingYear.ToString());
            var reportingYearKey = Guid.Empty;
            if (nmaReportingYearPublishedContent == null)
            {
                var nmaReportingYearContent = await CreatingNmaReportingYear(reportingYear, nma.Key);
                nmaReportingYearPublishedContent = await GetPublishedContentByKey(nmaReportingYearContent.Key);
            }

            var exists = nmaReportingYearPublishedContent.Children().FirstOrDefault(x => x.Name.Trim() == club.ClubName.Trim());

            if (exists != null)
            {
                var exception = new Exception("Club with this name already exists");
                _logger.LogError(exception, "Club {clubName} already exists", club.ClubName.Trim());
                throw exception;
            }

            // ToDo
            // Search previous 2 years for same club and copy information forward for club only.
            // Will do similar when adding club teams

            var nmaContent = _contentService.Create(club.ClubName.Trim(), nmaReportingYearPublishedContent.Key, "club");
            nmaContent.SetValue("clubName", club.ClubName.Trim());
            _contentService.SaveAndPublish(nmaContent);

            return nmaContent;
        }

        public async Task AddClubTeams(NmaClub club, IContent nmaClubContent)
        {
            var teamProperties = club.GetType().GetProperties()
                .Where(p => typeof(IEnumerable<ClubTeam>).IsAssignableFrom(p.PropertyType));

            foreach (var property in teamProperties)
            {
                var teams = property.GetValue(club) as IEnumerable<ClubTeam>;
                var ageGroup = property.Name;

                if (ageGroup == "Men")
                {
                    ageGroup = "Senior";
                }

                if (ageGroup == "Women")
                {
                    ageGroup = "Senior Women";
                }

                var ageGroupContent = _contentService.Create(ageGroup, nmaClubContent.Key, "ageGroup");
                _contentService.SaveAndPublish(ageGroupContent);

                if (teams != null)
                {
                    foreach (var team in teams)
                    {


                        ////if (exists != null)
                        ////{
                        ////    var exception = new Exception("Team with this name already exists");
                        ////    _logger.LogError(exception, "Team {teamName} already exists", team.TeamName.Trim());
                        ////    continue;
                        ////}

                        var nmaContent = _contentService.Create(team.TeamName.Trim(), ageGroupContent.Key, "clubTeam");
                        nmaContent.SetValue("teamName", team.TeamName.Trim());
                        nmaContent.SetValue("ageGroup", ageGroup);
                        _contentService.SaveAndPublish(nmaContent);

                        // ToDO
                        // Check it team exists in previous two years reporting and copy information over. 
                        // Copy logos 
                        // Copy team information
                        // copy contact information. 

                    }
                }
            }
        }
    }
}
