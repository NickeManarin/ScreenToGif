using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ScreenToGif.Util;

namespace ScreenToGif.Cloud.YandexDisk
{
    public class YandexDisk : ICloud
    {
        private readonly string _oauthToken;

        public YandexDisk(string oauthToken)
        {
            if (string.IsNullOrEmpty(oauthToken)) throw new ArgumentException(nameof(oauthToken));

            _oauthToken = oauthToken;
        }

        public async Task<UploadedFile> UploadFileAsync(string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException(nameof(path));

            var fileName = Path.GetFileName(path);

            var link = await GetAsync<Link>("https://cloud-api.yandex.net/v1/disk/resources/upload?path=app:/" + fileName + "&overwrite=true", cancellationToken);
            if (string.IsNullOrEmpty(link?.href)) throw new UploadingException("Unknown error");

            using (var fileSteram = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                await PutAsync(link.href, new StreamContent(fileSteram), cancellationToken);
            }

            var downloadLink = await GetAsync<Link>("https://cloud-api.yandex.net/v1/disk/resources/download?path=app:/" + fileName, cancellationToken);

            return new UploadedFile { Link = downloadLink.href };
        }

        private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            var handler = new HttpClientHandler
            {
                Proxy = WebHelper.GetProxy(),
                PreAuthenticate = true,
                UseDefaultCredentials = false,
            };

            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Headers =
                    {
                        {HttpRequestHeader.Authorization.ToString(), "OAuth " + _oauthToken}
                    }
                };

                string responseBody;
                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }
                
                var errorDescriptor = WebHelper.Deserialize<ErrorDescriptor>(responseBody);

                if (errorDescriptor.error != null)
                    throw new UploadingException($"{errorDescriptor.error}, {errorDescriptor.message}, {errorDescriptor.description}");

                return WebHelper.Deserialize<T>(responseBody);
            }
        }

        private async Task PutAsync(string url, HttpContent content, CancellationToken cancellationToken)
        {
            var handler = new HttpClientHandler
            {
                Proxy = WebHelper.GetProxy(),
                PreAuthenticate = true,
                UseDefaultCredentials = false,
            };

            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Headers =
                    {
                        {HttpRequestHeader.Authorization.ToString(), "OAuth " + _oauthToken}
                    },
                    Content = content
                };

                using (await client.SendAsync(request, cancellationToken))
                {

                }
            }
        }

        public static bool IsAuthorized()
        {
            if (string.IsNullOrWhiteSpace(UserSettings.All.YandexDiskOAuthToken))
                return false;

            return true;
        }
    }
}