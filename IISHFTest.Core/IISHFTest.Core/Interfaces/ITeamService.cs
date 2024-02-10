using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHFTest.Core.Interfaces
{
    public interface ITeamService
    {
        Task UpdateNmaTeamHistory(string html, IPublishedContent team);

        Task<IMedia>? UploadTeamPhoto(IFormFile file);

        Task AddImageToTeam(IMedia imageId, IPublishedContent team);
    }
}
