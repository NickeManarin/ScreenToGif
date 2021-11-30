using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models.Upload.Imgur;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.UploadPresets.History;
using ScreenToGif.ViewModel.UploadPresets.Imgur;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Cloud;

public class Imgur : IUploader
{
    public async Task<IHistory> UploadFileAsync(IUploadPreset preset, string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
    {
        if (preset is not ImgurPreset imgurPreset)
            throw new Exception("Imgur preset is null.");

        var args = new Dictionary<string, string>();
        var headers = new NameValueCollection();

        if (!preset.IsAnonymous)
        {
            if (!await IsAuthorized(imgurPreset))
                throw new UploadException("It was not possible to get the authorization to upload to Imgur.");

            headers.Add("Authorization", "Bearer " + imgurPreset.AccessToken);

            if (imgurPreset.UploadToAlbum)
            {
                var album = string.IsNullOrWhiteSpace(imgurPreset.SelectedAlbum) || imgurPreset.SelectedAlbum == "♥♦♣♠" ?
                    await AskForAlbum(imgurPreset) : imgurPreset.SelectedAlbum;

                if (!string.IsNullOrEmpty(album))
                    args.Add("album", album);
            }
        }
        else
        {
            headers.Add("Authorization", "Client-ID " + Secret.ImgurId);
        }

        if (cancellationToken.IsCancellationRequested)
            return null;

        return await Upload(imgurPreset, path, args, headers);
    }


    public static string GetAuthorizationAdress()
    {
        var args = new Dictionary<string, string>
        {
            {"client_id", Secret.ImgurId},
            {"response_type", "pin"}
        };

        return WebHelper.AppendQuery("https://api.imgur.com/oauth2/authorize", args);
    }

    public static async Task<bool> GetTokens(ImgurPreset preset)
    {
        var args = new Dictionary<string, string>
        {
            {"client_id", Secret.ImgurId},
            {"client_secret", Secret.ImgurSecret},
            {"grant_type", "pin"},
            {"pin", preset.OAuthToken}
        };

        return await GetTokens(preset, args);
    }

    public static async Task<bool> RefreshToken(ImgurPreset preset)
    {
        var args = new Dictionary<string, string>
        {
            {"refresh_token", preset.RefreshToken},
            {"client_id", Secret.ImgurId},
            {"client_secret", Secret.ImgurSecret},
            {"grant_type", "refresh_token"}
        };

        return await GetTokens(preset, args);
    }

    public static bool IsAuthorizationExpired(ImgurPreset preset)
    {
        return DateTime.UtcNow > preset.ExpiryDate;
    }

    public static async Task<bool> IsAuthorized(ImgurPreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.RefreshToken))
            return false;

        if (!IsAuthorizationExpired(preset))
            return true;

        return await RefreshToken(preset);
    }

    public static async Task<List<ImgurAlbum>> GetAlbums(ImgurPreset preset)
    {
        if (!await IsAuthorized(preset))
            return null;

        var headers = new NameValueCollection
        {
            { "Authorization", "Bearer " + preset.AccessToken }
        };

        var response = await WebHelper.Get("https://api.imgur.com/3/account/me/albums", headers);

        var responseAux = Serializer.Deserialize<ImgurAlbumsResponse>(response);

        if (responseAux == null || (!responseAux.Success && responseAux.Status != 200))
            return null;

        var list = responseAux.Data.Select(s => new ImgurAlbum(s)).ToList();

        preset.Albums = new ArrayList(list);

        return list;
    }

    public static async Task<string> AskForAlbum(ImgurPreset preset)
    {
        var albums = await GetAlbums(preset);

        return Application.Current.Dispatcher.Invoke<string>(() => PickAlbumDialog.OkCancel(albums));
    }


    private static async Task<bool> GetTokens(ImgurPreset preset, Dictionary<string, string> args)
    {
        var response = await WebHelper.PostMultipart("https://api.imgur.com/oauth2/token", args);

        if (string.IsNullOrEmpty(response))
            return false;

        var token = Serializer.Deserialize<OAuth2Token>(response);

        if (string.IsNullOrEmpty(token?.AccessToken))
            return false;

        preset.AccessToken = token.AccessToken;
        preset.RefreshToken = token.RefreshToken;
        preset.ExpiryDate = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpiresIn - 10);
        return true;
    }

    private async Task<History> Upload(ImgurPreset preset, string path, Dictionary<string, string> args, NameValueCollection headers)
    {
        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var result = await WebHelper.SendFile("https://api.imgur.com/3/image", stream, path, args, headers, "image");
            var response = Serializer.Deserialize<ImgurUploadResponse>(result);

            //Error when sending video.
            //{"data":{"errorCode":null,"ticket":"7234557b"},"success":true,"status":200}
            //{"data":{"error":"No image data was sent to the upload api","request":"\/3\/image","method":"POST"},"success":false,"status":400}

            if (response == null || (!response.Success && response.Status != 200))
            {
                LogWriter.Log("It was not possible to upload to Imgur", result);

                return new ImgurHistory
                {
                    PresetName = preset.Title,
                    DateInUtc = DateTime.UtcNow,
                    Result = 400,
                    Message = response?.Status + " - " + (response?.Data?.Error ?? result)
                };
            }

            if (string.IsNullOrEmpty(response.Data?.Link))
            {
                LogWriter.Log("It was not possible to upload to Imgur", result);

                return new ImgurHistory
                {
                    PresetName = preset.Title,
                    DateInUtc = DateTime.UtcNow,
                    Result = 400,
                    Message = "Upload failed. The link was not provided."
                };
            }

            var history = new ImgurHistory
            {
                PresetName = preset.Title,
                DateInUtc = DateTime.UtcNow,
                Result = 200,
                Id = response.Data.Id,
                Link = $"https://imgur.com/{response.Data.Id}",
                DeletionLink = $"https://imgur.com/delete/{response.Data.DeleteHash}",
                Mp4 = response.Data.Mp4,
                Webm = response.Data.Webm,
                Gifv = response.Data.Gifv,
                Gif = response.Data.Link
            };

            return history;
        }
    }
}