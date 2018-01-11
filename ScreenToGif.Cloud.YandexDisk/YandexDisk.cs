using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenToGif.Cloud.YandexDisk
{
    public class YandexDisk : ICloud
    {
        private readonly string _oauthToken;

        public YandexDisk(string oauthToken)
        {
            _oauthToken = oauthToken;
        }

        public async Task<UploadedFile> UploadFileAsync(string path, CancellationToken cancellationToken,
            IProgress<double> progressCallback = null)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://cloud-api.yandex.net/v1/disk/")
            {
                Headers =
                {
                    {HttpRequestHeader.Authorization.ToString(), "OAuth " + _oauthToken}
                }
            };

            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync();

            return new UploadedFile();
        }
    }
}