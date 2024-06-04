using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using IISHF.Core.Models.ServiceBusMessage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using IFileService = IISHF.Core.Interfaces.IFileService;
using IMediaService = Umbraco.Cms.Core.Services.IMediaService;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Strings;

namespace IISHF.Core.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMemberService _memberService;
        private readonly IMemberManager _memberManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMediaService _umbracoMediaService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMessageSender _messageSender;
        private readonly IFileService _fileService;
        private readonly INMAService _nmaService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
        private readonly MediaFileManager _mediaFileManager;
        private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMemberService memberService,
            IMemberManager memberManager,
            IHttpContextAccessor httpContextAccessor,
            IMediaService umbracoMediaService,
            IWebHostEnvironment webHostEnvironment,
            IMessageSender messageSender,
            IFileService fileService,
            INMAService nmaService,
            IShortStringHelper shortStringHelper,
            IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
            MediaFileManager mediaFileManager,
            MediaUrlGeneratorCollection mediaUrlGeneratorCollection,
            ILogger<TournamentService> logger)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _memberService = memberService;
            _memberManager = memberManager;
            _httpContextAccessor = httpContextAccessor;
            _umbracoMediaService = umbracoMediaService;
            _webHostEnvironment = webHostEnvironment;
            _messageSender = messageSender;
            _fileService = fileService;
            _nmaService = nmaService;
            _shortStringHelper = shortStringHelper;
            _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
            _mediaFileManager = mediaFileManager;
            _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
            _logger = logger;
        }

        public int? CreateEvent(TournamentModel model)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournament = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children()?
                .FirstOrDefault(x => x.Name.ToLower().Contains(model.IsChampionships ? "championships" : "cup"))!
                .Children()
                .FirstOrDefault(x => x.Value<string>("EventShotCode") == model.TitleEvent);

            if (tournament == null)
            {
                return null;
            }

            var linkObject = new
            {
                name = $"{model.HostClub} Website",
                url = model.HostWebsite,
                target = "_blank",
            };

            var linkArray = new[] { linkObject };
            var jsonLinkArray = JsonSerializer.Serialize(linkArray);

            var iishfEvent = _contentService.Create(model.EventYear.ToString(), tournament.Id, "event");
            iishfEvent?.SetValue("eventStartDate", model.EventStartDate);
            iishfEvent?.SetValue("eventEndDate", model.EventEndDate);
            iishfEvent?.SetValue("hostClub", model.HostClub);
            iishfEvent?.SetValue("hostContact", model.HostContact);
            iishfEvent?.SetValue("hostPhoneNumber", model.HostPhoneNumber);
            iishfEvent?.SetValue("hostEmail", model.HostEmail);
            iishfEvent?.SetValue("hostWebSite", jsonLinkArray);
            iishfEvent?.SetValue("venueName", model.VenueName);
            iishfEvent?.SetValue("venueAddress", model.VenueAddress);
            iishfEvent?.SetValue("rinkSizeLength", model.RinkLength);
            iishfEvent?.SetValue("rinkSizeWidth", model.RinkWidth);
            iishfEvent?.SetValue("rinkFloor", model.RinkFloor);

            _contentService.SaveAndPublish(iishfEvent);

            return iishfEvent.Id;
        }

        public IPublishedContent? GetTournament(int id)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournament = _contentQuery.ContentAtRoot()
                .DescendantsOrSelfOfType("event")
                .Where(x => x.Id == id).FirstOrDefault();

            return tournament;
        }

        public IPublishedContent? GetTournament(bool isChampionships, string titleEvent, string eventYear)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournaments = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children()?
                .FirstOrDefault(x => x.Name.ToLower().Contains(isChampionships ? "championships" : "cup"))!.Children();

            return tournaments == null || !tournaments.Any()
                ? null
                : tournaments.FirstOrDefault(x => x.Value<string>("EventShotCode") == titleEvent).Children()
                    .FirstOrDefault(x => x.Name == eventYear) ?? null;
        }

        public IPublishedContent? GetTournamentTeamByName(string teamName, IPublishedContent tournament)
        {
            var team = tournament.Children().FirstOrDefault(x => x.Name == teamName);
            return team;
        }

        public IPublishedContent? GetTournamentTeamById(int id, IPublishedContent tournament)
        {
            var team = tournament.Children().FirstOrDefault(x => x.Id == id);
            return team;
        }

        public IPublishedContent? GetTournamentTeamByKey(Guid key, IPublishedContent tournament)
        {
            var team = tournament.Children().FirstOrDefault(x => x.Key == key);
            return team;
        }

        public async Task UpdateGameWithResults(UpdateTeamScores model, IPublishedContent tournament)
        {
            await Task.Run(async () =>
            {
                foreach (var finalScore in model.Scores)
                {
                    var game = tournament.Children.FirstOrDefault(x => x.Name == finalScore.GameNumber.ToString() && x.ContentType.Alias == "game");
                    if (game == null)
                    {
                        var exception = new NullReferenceException($"Game number {finalScore.GameNumber} not found");
                        _logger.LogError(exception, "Unable to find game number {gameId}", finalScore.GameNumber);
                        throw exception;
                    }

                    var gameToUpdate = _contentService.GetById(game.Id);

                    gameToUpdate?.SetValue("homeScore", finalScore.HomeScore);
                    gameToUpdate?.SetValue("awayScore", finalScore.AwayScore);
                    _contentService.SaveAndPublish(gameToUpdate);
                }

            });
        }

        public async Task<IContent>? AddGameSheetToGame(IFormFile file, string name, IPublishedContent game)
        {

            string pattern = @"\[(.*?)\]";

            Match match = Regex.Match(name, pattern);
            if (match.Success)
            {
                // This will print the value between the square brackets
                name = match.Groups[1].Value;
            }
            
            var mediaItem = await CreateMediaAsync(file, $"{game.Parent.Parent.Name}-{game.Parent.Name}");
            var document = _contentService.GetById(game.Id);

            // Ensure the mediaItemId is converted to a UDI
            var media = _contentQuery.Media(mediaItem.Key);
            var udi = Udi.Create(Constants.UdiEntityType.Media, media.Key);
            document.SetValue("gameSheet", udi.ToString());
            _contentService.SaveAndPublish(document);

            return document;
        }

        private async Task<IMedia> CreateMediaAsync(IFormFile file, string directory)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                int folderId = EnsureFolderExists(directory);
                IMedia media = _umbracoMediaService.CreateMedia(file.FileName, folderId,
                Constants.Conventions.MediaTypes.Image);
                media.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper,
                _contentTypeBaseServiceProvider,
                    Constants.Conventions.Media.File, file.FileName, stream);
                _umbracoMediaService.Save(media);
                return media;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "unable to upload media");
                throw;
            }
        }


        
        public Task CreateEventGame(CreateScheduleGames model, IPublishedContent tournament)
        {
            foreach (var scheduledGame in model.Games)
            {
                var game = _contentService.Create(scheduledGame.GameNumber.ToString(), tournament.Id, "game");
                game?.SetValue("homeTeam", scheduledGame.HomeTeam);
                game?.SetValue("awayTeam", scheduledGame.AwayTeam);
                game?.SetValue("gameNumber", scheduledGame.GameNumber);
                game?.SetValue("scheduleDateTime", scheduledGame.GameDateTime);
                game?.SetValue("group", scheduledGame.Group);
                game?.SetValue("remarks", scheduledGame.Remarks);
                _contentService.SaveAndPublish(game);
            }

            return Task.CompletedTask;
        }

        public async Task UpdateEventGame(CreateScheduleGames model, IPublishedContent tournament)
        {
            await Task.Run(async () =>
            {
                foreach (var scheduledGame in model.Games)
                {
                    var canUpdate = false;
                    var publishedGame = tournament.Children.FirstOrDefault(x => x.Name == scheduledGame.GameNumber.ToString() && x.ContentType.Alias == "game");

                    var game = _contentService.GetById(publishedGame.Id);

                    var currentHomeTeam = game.GetValue<string>("homeTeam");
                    var currentAwayTeamTeam = game.GetValue<string>("awayTeam");

                    if (currentHomeTeam != scheduledGame.HomeTeam)
                    {
                        game?.SetValue("homeTeam", scheduledGame.HomeTeam);
                        canUpdate = true;
                    }


                    if (currentAwayTeamTeam != scheduledGame.AwayTeam)
                    {
                        game?.SetValue("awayTeam", scheduledGame.AwayTeam);
                        canUpdate = true;
                    }

                    if (canUpdate)
                    {
                        _contentService.SaveAndPublish(game);

                    }
                }
            });
        }

        public IContent CreateEventTeam(Team model, IPublishedContent tournament)
        {
            var team = _contentService.Create(model.TeamName, tournament.Id, "team");
            team?.SetValue("eventTeam", model.TeamName);
            team?.SetValue("countryIso3", model.CountryIso3);
            team?.SetValue("group", model.Group);

            // Assuming you have a URL and a name for the link
            if (model.TeamUrl != null)
            {
                var linkObject = new
                {
                    name = $"{model.TeamName} Website",
                    url = model.TeamUrl,
                    target = "_blank",
                };

                var linkArray = new[] { linkObject };
                var jsonLinkArray = JsonSerializer.Serialize(linkArray);

                team.SetValue("teamWebsite", jsonLinkArray);
            }

            _contentService.SaveAndPublish(team);

            return team;
        }

        public async Task SetSelectTeamCreator(IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var tournamentTeam = _contentService.GetById(team.Id);
            if (tournamentTeam == null)
            {
                return;
            }

            var udi = Udi.Create(Constants.UdiEntityType.Member, user.Key);

            tournamentTeam.SetValue("selectTeamCreatedBy", udi);
            _contentService.SaveAndPublish(tournamentTeam);
        }

        public async Task UpdateTeamProperties(string propertyValue, string fieldName, IPublishedContent team)
        {
            await Task.Run(() =>
            {
                if (team.Value<string>(fieldName) == propertyValue)
                {
                    return Task.FromResult(Task.CompletedTask);
                }
                var teamToUpdate = _contentService.GetById(team.Id);
                teamToUpdate?.SetValue(fieldName, propertyValue);
                _contentService.SaveAndPublish(teamToUpdate);
                return Task.CompletedTask;
            });
        }

        public async Task SetTeamItcSubmissionDateFromTeam(IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var tournamentTeam = _contentService.GetById(team.Id);
            if (tournamentTeam == null)
            {
                return;
            }

            var udi = Udi.Create(Constants.UdiEntityType.Member, user.Key);

            tournamentTeam.SetValue("ITcsubmissionDate", DateTime.UtcNow);
            tournamentTeam.SetValue("ITcsubmitted", true);
            tournamentTeam.SetValue("ITcsubmittedBy", udi);
            _contentService.SaveAndPublish(tournamentTeam);
        }

        public async Task UnsubmitItc(IPublishedContent team)
        {
            await Task.Run(async () =>
            {
                var tournamentTeam = _contentService.GetById(team.Id);
                if (tournamentTeam == null)
                {
                    return;
                }

                tournamentTeam.SetValue("iTCSubmissionDate", null);
                tournamentTeam.SetValue("iTCSubmitted", false);
                tournamentTeam.SetValue("iTCSubmittedBy", null);
                _contentService.SaveAndPublish(tournamentTeam);

            });
        }

        public async Task CopyItc(IPublishedContent team, bool rejected = false)
        {
            await Task.Run(async () =>
            {
                var lable = rejected ? "rejected" : "revision";
                var tournamentTeam = _contentService.GetById(team.Id);
                var newContent = _contentService.Copy(tournamentTeam, tournamentTeam.ParentId, true, true);
                newContent.Name = $"{team.Name} - {lable}  - {DateTime.Now.ToString("yyyyMMdd-HHmmss")}";
                _contentService.Save(newContent);
            });
        }

        public async Task SetNmaCheckValue(IEnumerable<RosterApproval> rosterMembers, bool isNma)
        {
            await Task.Run(async () =>
            {

                var propertyName = isNma ? "nmaCheck" : "iishfCheck";
                foreach (var rosterMember in rosterMembers)
                {


                    var rosterMemberContent = _contentService.GetById(rosterMember.Id);
                    rosterMemberContent.SetValue(propertyName, rosterMember.Approved);
                    rosterMemberContent.SetValue("comments", rosterMember.Comments);
                    _contentService.SaveAndPublish(rosterMemberContent);
                }
            });
        }

        public async Task SetTeamItcNmaApprovalDate(IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var tournamentTeam = _contentService.GetById(team.Id);
            if (tournamentTeam == null)
            {
                return;
            }

            var udi = Udi.Create(Constants.UdiEntityType.Member, user.Key);

            tournamentTeam.SetValue("nMAApprovedDate", DateTime.UtcNow);
            tournamentTeam.SetValue("iTCNMAApprover", udi);
            _contentService.SaveAndPublish(tournamentTeam);
        }

        public async Task SetTeamItcIISHFApprovalDate(IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var tournamentTeam = _contentService.GetById(team.Id);
            if (tournamentTeam == null)
            {
                return;
            }

            var udi = Udi.Create(Constants.UdiEntityType.Member, user.Key);

            tournamentTeam.SetValue("iISHFApprovedDate", DateTime.UtcNow);
            tournamentTeam.SetValue("iISHFITCApprover", udi);
            tournamentTeam.SetValue("iTCApproved", true);
            _contentService.SaveAndPublish(tournamentTeam);
        }

        public async Task SetTeamInformationSubmissionDate(IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }

            var tournamentTeam = _contentService.GetById(team.Id);
            if (tournamentTeam == null)
            {
                return;
            }

            var udi = Udi.Create(Constants.UdiEntityType.Member, user.Key);

            tournamentTeam.SetValue("teamInformationSubmissionDate", DateTime.UtcNow);
            tournamentTeam.SetValue("teamInformationSubmitted", true);
            tournamentTeam.SetValue("teamInformationSubmittedBy", udi);
            _contentService.SaveAndPublish(tournamentTeam);
        }

        public async Task SubmitTeamInformationToHost(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team)
        {
            // Logos from nma team - done
            // Photo from nma team - done
            // Sponsor from tournament team - done
            // Roster from tournament team - dine
            // Team write up from nma team - done
            // Age group from tournament - done
            // Host information from tournament - done

            var protocol = _httpContextAccessor.HttpContext.Request.Scheme;
            var baseUrl = _httpContextAccessor.HttpContext.Request.Host;

            var sponsorList = nmaTeam.Children.Where(x => x.ContentType.Alias == "sponsor")
                .Select(x => x.Value<IPublishedContent>("sponsorImage").Url().ToString()).ToList();

            var teamPhotoUrl = nmaTeam.Value<IPublishedContent>("teamPhoto");
            var teamLogoUrl = nmaTeam.Value<IPublishedContent>("teamLogo");
            var logos = _umbracoMediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Logos");
            var iishfLogo = _umbracoMediaService.GetPagedChildren(logos.Id, 0, int.MaxValue, out var totalChildren).FirstOrDefault(x => x.Name == "IISHF");

            var value = iishfLogo.Properties[0].Values
                .FirstOrDefault(v => !string.IsNullOrEmpty(v.EditedValue.ToString()) &&
                                     v.EditedValue.ToString().Contains("\"src\"") &&
                                     v.EditedValue.ToString().Contains("iishf"));
            var src = string.Empty;
            if (value != null)
            {
                var jsonDocument = JsonDocument.Parse(value.EditedValue.ToString());
                src = jsonDocument.RootElement.GetProperty("src").GetString();
                // Use the src value as needed
            }

            var sponsors = nmaTeam.Children().Where(x => x.ContentType.Alias == "sponsor");
            var sponsorImages = sponsors
                .Select(sponsor => sponsor.Value<IPublishedContent>("sponsorImage"))
                .Select(logo => new Sponsor()
                {
                    SponsorLogo = new Uri($"{protocol}://{baseUrl}{logo.Url()}"),
                    SponsorName = logo.Name
                })
                .ToList();

            var teamWriteUp = nmaTeam.Value<string>("teamHistory");

            var roster = team.Children().Where(x => x.ContentType.Alias == "roster").ToList();

            var roleOrder = new Dictionary<string, int>
            {
                {"Captain", 11},
                {"Assistant Captain", 12},
                {"Netminder", 13},

                {"Head Coach", 24},
                {"Assistant Coach", 25},
                {"Training Staff", 26},
                {"Equipment Manager", 27},
                {"Physio", 28},
                {"Photographer", 29},
            };

            var sortedRoster = roster
                .OrderBy(player => player.Value<bool>("isBenchOfficial") ? 1 : 0) // First, separate bench officials from players
                .ThenBy(player => roleOrder.ContainsKey(player.Value<string>("role")) ? roleOrder[player.Value<string>("role")] : int.MaxValue)
                .ThenBy(player => player.Value<int>("jerseyNumber"))
                .ThenBy(player => player.Value<bool>("isBenchOfficial") && roleOrder.ContainsKey(player.Value<string>("role")) ? roleOrder[player.Value<string>("role")] : int.MaxValue) // Additional sorting for bench officials by role
                .ToList();

            var rosteredMembers = sortedRoster.Select(x => new Roster()
            {
                Id = x.Id,
                Role = x.Value<string>("role"),
                GivenName = x.Value<string>("firstName"),
                LastName = x.Value<string>("lastName"),
                JerseyNumber = x.Value<int?>("jerseyNumber"),
            }).ToList();

            var ageGroup = tournament.Parent.Value<string>("AgeGroup");

            var sanction = tournament.Value<string>("sanctionNumber");

            var teamInformationSubmissionDate = team.Value<DateTime>("teamInformationSubmissionDate");
            var teamInformationSubmittedBy = team.Value<IPublishedContent>("teamInformationSubmittedBy");
            var submittedByMember = _memberService.GetById(teamInformationSubmittedBy.Id);

            var teamInformation = new TeamInformation()
            {
                Id = team.Id,
                EventReference = sanction,
                AgeGroup = ageGroup,
                Roster = rosteredMembers, // need to map the roster
                TeamName = team.Name,
                TeamWriteUp = teamWriteUp,
                TeamPhoto = new Uri($"{protocol}://{baseUrl}{teamPhotoUrl.Url()}"),
                TeamLogo = new Uri($"{protocol}://{baseUrl}{teamLogoUrl.Url()}"),
                IISHFLogo = new Uri($"{protocol}://{baseUrl}{src}"),
                Sponsors = sponsorImages,
                SubmittedBy = teamInformationSubmittedBy.Name,
                SubmittedDateTime = teamInformationSubmissionDate,
                SubmittedByEmail = submittedByMember.Email
            };

            var eventInformation = new EventInformation
            {
                EventNumber = sanction,
                EventName = tournament.Parent.Name,
                StartDate = tournament.Value<DateTime>("eventStartDate"),
                EndDate = tournament.Value<DateTime>("eventEndDate")
            };

            var hostInformation = new HostInformation
            {
                HostName = tournament.Value<string>("hostContact"),
                EmailAddress = tournament.Value<string>("hostEmail")
            };

            var serviceBusMessage = new SubmittedInformation
            {
                EventInformation = eventInformation,
                TeamInformation = teamInformation,
                HostInformation = hostInformation
            };

            await _messageSender.SendMessage(serviceBusMessage, "team-submission");
        }

        public async Task NotifyNmaApprover(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team)
        {
            var user = await _memberManager.GetCurrentMemberAsync();
            if (user == null)
            {
                return;
            }


            IPublishedContent nma = null;

            if (nmaTeam != null)
            {
                nma = nmaTeam.Parent.Parent.Parent;
            }
            else
            {
                nma = _contentQuery.ContentAtRoot()
                   .DescendantsOrSelfOfType("nationalMemberAssociations")
                   .FirstOrDefault()
                   .Children()
                   .FirstOrDefault(x => x.Value<string>("iSO3") == team.Value<string>("countryIso3"));
            }

            var itcApprovers = await _nmaService.GetNMAITCApprovers(nma.Key);

            var protocol = _httpContextAccessor.HttpContext.Request.Scheme;
            var baseUrl = _httpContextAccessor.HttpContext.Request.Host;
            var route = $"itc";
            var queryString = $"team={team.Key.ToString()}";

            var serviceBusMessage = new SubmittedITCInformation
            {
                ItcApprovers = itcApprovers.ToList(),
                ITCApprovalUri = new Uri($"{protocol}://{baseUrl}/{route}?{queryString}"),
                TemplateName = "NmaItcApproval",
                TeamName = team.Name,
                SubmittedByName = user.Name
            };

            await _messageSender.SendMessage(serviceBusMessage, "itc-submission");
        }

        public Task NotifyIISHFApprover(IPublishedContent tournament, IPublishedContent nmaTeam, IPublishedContent team)
        {
            throw new NotImplementedException();
        }

        public async Task<RejectedRosterMembersModel> GetRejectedRosterMembers(int[] playerIds)
        {
            return await Task.Run(async () =>
            {
                var rosterMembers = new List<RejectedRosterMember>();

                foreach (var playerId in playerIds)
                {
                    var content = GetById(playerId);

                    var rosterMember = new RejectedRosterMember
                    {
                        Id = playerId,
                        LicenseNumber = content.Value<string>("licenseNumber"),
                        Name = content.Value<string>("playerName"),
                        Reason = content.Value<string>("comments")
                    };

                    rosterMembers.Add(rosterMember);

                }

                return new RejectedRosterMembersModel
                {
                    RejectedRosterMembers = rosterMembers
                };

            });
        }

        public async Task SetItcRejectionReason(IPublishedContent team)
        {
            await Task.Run(async () =>
            {
                var updatedTeam = GetById(team.Id);

                var rosterMembers = team.Children().Where(x => x.ContentType.Alias == "roster").Where(x => !string.IsNullOrWhiteSpace(x.Value<string>("comments"))).ToList();

                var comments = new List<string>();

                var commentsStart = "<ul>";
                var commentsEnd = "<ul>";

                foreach (var rosterMember in rosterMembers)
                {

                    comments.Add($"<li>{rosterMember.Value<string>("playerName")} - {rosterMember.Value<string>("comments")}</li>");
                }

                var teamContent = _contentService.GetById(team.Id);
                teamContent.SetValue("iTCRejectionReason", $"{commentsStart}{string.Join(Environment.NewLine, comments)}{commentsEnd}");
                _contentService.SaveAndPublish(teamContent);

            });
        }

        public byte[] GenerateItcAsPdfFile(byte[] itBytes)
        {
            return _fileService.GenerateItcAsPdfFile(itBytes);
        }

        public async Task<byte[]> GenerateItcAsExcelFile(IPublishedContent team, IPublishedContent tournament)
        {
            return await _fileService.GenerateItcAsExcelFile(team, tournament);
        }

        public async Task ResetNmaApproval(IPublishedContent team)
        {
            await Task.Run(async () =>
            {
                await CopyItc(team, true);

                var tournamentTeam = _contentService.GetById(team.Id);
                if (tournamentTeam == null)
                {
                    return;
                }

                tournamentTeam.SetValue("iTCSubmissionDate", null);
                tournamentTeam.SetValue("iTCSubmitted", false);
                tournamentTeam.SetValue("iTCSubmittedBy", null);

                tournamentTeam.SetValue("nMAApprovedDate", null);
                tournamentTeam.SetValue("iTCNMAApprover", null);

                tournamentTeam.SetValue("iTCApproved", false);
                _contentService.SaveAndPublish(tournamentTeam);
            });
        }

        public IPublishedContent GetById(int id)
        {
            return _contentQuery.Content(id);
        }

        private List<IPublishedContent> GetEventTeams(int year, int tournamentId, int teamId)
        {
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var tournaments = rootContent
                .FirstOrDefault(x => x.Name == "Home")!.Children()?
                .FirstOrDefault(x => x.Name == "Tournaments")!.Children().ToList();

            var cups = tournaments
                .FirstOrDefault(x => x.Name == "European Cups")
                .Children()
                .Select(x => x.Children.FirstOrDefault(x => x.Name == year.ToString()))
                .ToList();

            var championships = tournaments
                .FirstOrDefault(x => x.Name == "European Championships")
                .Children()
                .Select(x => x.Children.FirstOrDefault(x => x.Name == year.ToString()))
                .ToList();

            var selectedEvent = cups.Where(x => x != null).ToList().Concat(championships.Where(x => x != null).ToList())
                .FirstOrDefault(x => x.Id == tournamentId);

            if (selectedEvent == null)
            {
                return new List<IPublishedContent>();
            }

            var team = selectedEvent.Children.Where(x => x.ContentType.Alias == "team").ToList();

            return team;
        }

        public byte[] GetImageByteArray(int mediaId, string filePath)
        {
            // Retrieve the media item
            var mediaItem = _umbracoMediaService.GetById(mediaId);
            if (mediaItem == null) return null;

            // Combine with the root path if it's stored locally
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));

            // Read the file into a byte array
            return System.IO.File.Exists(fullPath) ? System.IO.File.ReadAllBytes(fullPath) : null;
        }

        private int EnsureFolderExists(string folderName)
        {
            // Check if the folder exists
            var existingFolder = _umbracoMediaService.GetRootMedia().FirstOrDefault(x => x.Name == folderName && x.ContentType.Alias == Constants.Conventions.MediaTypes.Folder);

            if (existingFolder != null)
            {
                // Return the existing folder's ID if found
                return existingFolder.Id;
            }

            // If the folder does not exist, create it
            var mediaFolder = _umbracoMediaService.CreateMedia(folderName, Constants.System.Root, Constants.Conventions.MediaTypes.Folder);
            _umbracoMediaService.Save(mediaFolder);

            return mediaFolder.Id; // Return the new folder's ID
        }

    }
}
