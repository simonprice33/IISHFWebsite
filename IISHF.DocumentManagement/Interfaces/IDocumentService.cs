using IISHF.DocumentManagement.Models;

namespace IISHF.DocumentManagement.Interfaces
{
    public interface IDocumentService
    {
        Task<MemoryStream> GeneratePdfToMemoryStreamAsync(SubmittedTeamInformationModel teamInformation, byte[] iishfLogo);
    }
}
