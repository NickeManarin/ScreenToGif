using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public async Task<UploadedFile> UploadFileAsync(string path, CancellationToken cancellationToken,
            IProgress<double> progressCallback = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException(nameof(path));

            var fileName = Path.GetFileName(path);

            var link =  await GetAsync<Link>("https://cloud-api.yandex.net/v1/disk/resources/upload?path=" + fileName + "&overwrite=true", cancellationToken);

            if (string.IsNullOrEmpty(link?.Href)) throw new UploadingException();

            using (var fileSteram = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                await PutAsync(link.Href, new StreamContent(fileSteram), cancellationToken);
            }

            var downloadLink = await GetAsync<Link>("https://cloud-api.yandex.net/v1/disk/resources/download?path=" + fileName, cancellationToken);
            
            return new UploadedFile() {Link = downloadLink.Href};
        }

        private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
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

                return JsonConvert.DeserializeObject<T>(responseBody);
            }
        }

        private async Task PutAsync(string url, HttpContent content, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Headers =
                    {
                        {HttpRequestHeader.Authorization.ToString(), "OAuth " + _oauthToken}
                    },
                    Content = content
                };
                
                using ( await client.SendAsync(request, cancellationToken))
                {
                    
                }
            }
        }
    }
}