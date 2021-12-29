using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models.Upload.YandexDisk;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.UploadPresets.History;
using ScreenToGif.ViewModel.UploadPresets.Yandex;

namespace ScreenToGif.Cloud;

public class YandexDisk : IUploader
{
    public async Task<IHistory> UploadFileAsync(IUploadPreset preset, string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
    {
        if (string.IsNullOrEmpty(path)) 
            throw new ArgumentException(nameof(path));

        var fileName = Path.GetFileName(path);

        var link = await GetAsync<Link>(preset as YandexPreset, "https://cloud-api.yandex.net/v1/disk/resources/upload?path=app:/" + fileName + "&overwrite=true", cancellationToken);
            
        if (string.IsNullOrEmpty(link?.Href)) 
            throw new UploadException("Unknown error");

        await using (var fileSteram = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            await PutAsync(preset as YandexPreset, link.Href, new StreamContent(fileSteram), cancellationToken);
        }

        var downloadLink = await GetAsync<Link>(preset as YandexPreset, "https://cloud-api.yandex.net/v1/disk/resources/download?path=app:/" + fileName, cancellationToken);

        var history = new History
        {
            Type = preset.Type,
            PresetName = preset.Title,
            DateInUtc = DateTime.UtcNow,
            Result = 200,
            Link = downloadLink.Href
        };
            
        return history;
    }

    private async Task<T> GetAsync<T>(YandexPreset preset, string url, CancellationToken cancellationToken)
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
                    {HttpRequestHeader.Authorization.ToString(), "OAuth " + preset.OAuthToken}
                }
            };

            string responseBody;
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
                
            var errorDescriptor = Serializer.Deserialize<ErrorDescriptor>(responseBody);

            if (errorDescriptor.Error != null)
                throw new UploadException($"{errorDescriptor.Error}, {errorDescriptor.Message}, {errorDescriptor.Description}");

            return Serializer.Deserialize<T>(responseBody);
        }
    }

    private async Task PutAsync(YandexPreset preset, string url, HttpContent content, CancellationToken cancellationToken)
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
                    {HttpRequestHeader.Authorization.ToString(), "OAuth " + preset.OAuthToken}
                },
                Content = content
            };

            using (await client.SendAsync(request, cancellationToken))
            { }
        }
    }

    public static string GetAuthorizationAdress()
    {
        var args = new Dictionary<string, string>
        {
            {"client_id", Secret.YandexId},
            {"response_type", "token"}
        };

        return WebHelper.AppendQuery($"https://oauth.yandex.{(UserSettings.All.LanguageCode.StartsWith("ru") ? "ru" : "com")}/authorize", args);
    }

    public static bool IsAuthorized(YandexPreset preset)
    {
        return !string.IsNullOrWhiteSpace(preset.OAuthToken);
    }
}