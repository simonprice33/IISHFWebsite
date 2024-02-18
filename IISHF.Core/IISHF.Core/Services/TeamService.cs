using IISHF.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using IMediaService = Umbraco.Cms.Core.Services.IMediaService;

namespace IISHF.Core.Services
{
    public class TeamService : ITeamService
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IContentService _contentService;
        private readonly IMediaService _umbracoMediaService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
        private readonly MediaFileManager _mediaFileManager;
        private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;

        public TeamService(
            IPublishedContentQuery contentQuery,
            IContentService contentService,
            IMediaService umbracoMediaService,
            IShortStringHelper shortStringHelper,
            IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
            MediaFileManager mediaFileManager,
            MediaUrlGeneratorCollection mediaUrlGeneratorCollection)
        {
            _contentQuery = contentQuery;
            _contentService = contentService;
            _umbracoMediaService = umbracoMediaService;
            _shortStringHelper = shortStringHelper;
            _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
            _mediaFileManager = mediaFileManager;
            _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
        }

        public async Task UpdateNmaTeamHistory(string html, IPublishedContent team)
        {
            var nmaTeam = _contentService.GetById(team.Id);
            if (nmaTeam == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                nmaTeam.SetValue("teamHistory", html);
                _contentService.SaveAndPublish(nmaTeam);
                return Task.CompletedTask;
            });
        }

        public async Task<IMedia>? UploadTeamPhoto(IFormFile file, string teamPhoto)
        {
            if (file.Length == 0)
            {
                return null;
            }

            return await CreateMedia(file, teamPhoto);
        }

        private async Task<IMedia> CreateMedia(IFormFile file, string directory)
        {
            return await Task.Run(async () =>
            {
                await using var stream = file.OpenReadStream();
                int folderId = EnsureFolderExists(directory);
                IMedia media = _umbracoMediaService.CreateMedia(file.FileName, folderId, Constants.Conventions.MediaTypes.Image);
                media.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper, _contentTypeBaseServiceProvider,
                    Constants.Conventions.Media.File, file.FileName, stream);
                _umbracoMediaService.Save(media);
                return media;
            });
        }

        public async Task<List<IMedia>> UploadSponsors(List<IFormFile> files, IPublishedContent team, string teamPhoto)
        {
            var mediaList = new List<IMedia>();
            foreach (var file in files)
            {
                var mediaItem = await CreateMedia(file, "Sponsors");
                var sponsor = _contentService.Create(file.FileName, team.Id, "sponsor");
                _contentService.SaveAndPublish(sponsor);
                await AddImageToTeam(mediaItem, sponsor, "sponsorImage");
                mediaList.Add(mediaItem);
            }

            return mediaList;
        }

        public async Task AddImageToTeam(IMedia image, IPublishedContent team, string propertyAlias)
        {
            var nmaTeam = _contentService.GetById(team.Id);
            await AddImageToTeam(image, nmaTeam, propertyAlias);
        }

        public async Task DeleteSponsor(int sponsorId, int mediaId, IPublishedContent team)
        {
            if (team != null)
            {
                await Task.Run(() =>
                {
                    var nmaTeam = _contentQuery.Content(team.Value<Guid>("nMATeamKey"));

                    var sponsor = nmaTeam.Children().FirstOrDefault(x => x.Id == sponsorId);
                    var img = _contentService.GetById(sponsorId);

                    var media = _umbracoMediaService.GetById(mediaId);

                    _contentService.Delete(img);
                    _umbracoMediaService.Delete(media);

                    return Task.CompletedTask;
                });
            }
        }

        public async Task AddImageToTeam(IMedia image, IContent team, string propertyAlias)
        {
            if (team != null)
            {
                await Task.Run(() =>
                {
                    var media = _contentQuery.Media(image.Key);
                    var udi = Udi.Create(Constants.UdiEntityType.Media, media.Key);
                    team.SetValue(propertyAlias, udi.ToString());

                    _contentService.SaveAndPublish(team);
                    return Task.CompletedTask;
                });
            }
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
