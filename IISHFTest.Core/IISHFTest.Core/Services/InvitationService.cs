using IISHFTest.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;

namespace IISHFTest.Core.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            ILogger<InvitationService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _logger = logger;
        }

        public List<EventInvitation> GetInvitation(string email)
        {

            var invitations = new List<EventInvitation>();

            var teams = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("clubTeam")
                .FirstOrDefault(x => x.Value<string>("TeamContactEmail") == email);

            var publishedEvents = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("Tournaments")
                .FirstOrDefault().Children().ToList();

            foreach (var iishfEvent in publishedEvents)
            {
                foreach (var ageGroup in iishfEvent.Children().ToList())
                {
                    var titleEvents = ageGroup.Children().Where(x => x.Name == DateTime.Now.Year.ToString() && x.Parent.Value<string>("ageGroup") == teams.Value<string>("ageGroup")).ToList();
                    foreach (var evt in titleEvents)
                    {
                        var team = evt.Children().Where(x => x.ContentType.Alias == "team")
                            .FirstOrDefault(x => x.Name == teams.Name);

                        var invitation = new EventInvitation()
                        {
                            EventId = evt.Key,
                            EventTeamId = team.Key,
                            EventName = evt.Parent.Name,
                            ITCStatus = "Not Delivered",
                            TeamInformationSubmitted = team.Value<bool>("teamInformationSubmitted")
                        };

                        invitations.Add(invitation);

                    }
                }
            }


            return invitations;
        }
    }
}
