using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface ITeamService
    {
        Task UpdateNmaTeamHistory(string html, IPublishedContent team);

        Task<IMedia>? UploadTeamPhoto(IFormFile file, string directory);

        Task<List<IMedia>> UploadSponsors(List<IFormFile> files, IPublishedContent team, string directory);

        Task AddImageToTeam(IMedia imageId, IPublishedContent team, string propertyAlias);
    }
}
