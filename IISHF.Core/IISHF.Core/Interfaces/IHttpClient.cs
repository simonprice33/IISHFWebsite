namespace IISHF.Core.Interfaces
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> Get(Uri uri);

        Task<byte[]> GetByteArrayAsync(Uri uri);

        Task<string> GetStringAsync(Uri uri);
    }
}
