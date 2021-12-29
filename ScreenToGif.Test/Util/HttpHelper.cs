using System.Net.Http;
using System.Threading.Tasks;

namespace ScreenToGif.Test.Util
{
    internal static class HttpHelper
    {
        static public async Task<string> HttpDownloadFileAsync(string url)
        {
            var httpClient = new HttpClient();

            using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            return await response.Content.ReadAsStringAsync();
        }
    }
}