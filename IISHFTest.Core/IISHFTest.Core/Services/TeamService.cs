using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.PublishedCache;
using Umbraco.Extensions;
using IMediaService = Umbraco.Cms.Core.Services.IMediaService;

namespace IISHFTest.Core.Services
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

        public Task UpdateNmaTeamHistory(string html, IPublishedContent team)
        {
            var nmaTeam = _contentService.GetById(team.Id);
            if (nmaTeam == null)
            {
                return Task.CompletedTask;
            }

            nmaTeam.SetValue("teamHistory", html);
            _contentService.SaveAndPublish(nmaTeam);

            return Task.CompletedTask;
        }

        public Task<IMedia>? UploadTeamPhoto(IFormFile file)
        {
            if (file.Length == 0)
            {
                return null;
            }

            using var stream = file.OpenReadStream();
            int folderId = EnsureFolderExists("Unsanitised Logos");
            IMedia media = _umbracoMediaService.CreateMedia("Unicorn", folderId, Constants.Conventions.MediaTypes.Image);
            media.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper, _contentTypeBaseServiceProvider, Constants.Conventions.Media.File, file.FileName, stream);
            _umbracoMediaService.Save(media);
            return Task.FromResult(media);
        }

        public Task AddImageToTeam(IMedia image, IPublishedContent team)
        {
            var nmaTeam = _contentService.GetById(team.Id);
           
            if (nmaTeam != null)
            {
                    var media = _contentQuery.Media(image.Key);
                    var udi = Udi.Create(Constants.UdiEntityType.Media, media.Key);
                    nmaTeam.SetValue("teamPhoto", udi.ToString());
                    
                    _contentService.SaveAndPublish(nmaTeam);
            }

            return Task.CompletedTask;
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
