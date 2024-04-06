using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Interfaces
{
    public interface IFileService
    {
        Task<byte[]> GenerateItcAsExcelFile(IPublishedContent team, IPublishedContent tournament);

        byte[] GenerateItcAsPdfFile(byte[] itcAsByteArray);
    }
}
