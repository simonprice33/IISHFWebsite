using IISHFTest.Core.Interfaces;

namespace IISHFTest.Core.Services
{
    public class HttpClient : IHttpClient
    {
        private readonly System.Net.Http.HttpClient _client;

        public HttpClient()
        {
            _client = new System.Net.Http.HttpClient();
        }

        public async Task<HttpResponseMessage> Get(Uri uri)
        {
            return await _client.GetAsync(uri);
        }

        public async Task<byte[]> GetByteArrayAsync(Uri uri)
        {
            return await _client.GetByteArrayAsync(uri);
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            return await _client.GetStringAsync(uri);
        }
    }
}
