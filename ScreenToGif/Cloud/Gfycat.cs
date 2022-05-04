using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models.Upload.Gfycat;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.UploadPresets.Gfycat;
using ScreenToGif.ViewModel.UploadPresets.History;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Cloud;

public class Gfycat : IUploader
{
    public async Task<IHistory> UploadFileAsync(IUploadPreset preset, string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
    {
        if (preset is not GfycatPreset gfycatPreset)
            throw new Exception("Gfycat preset is null.");

        if (!await IsAuthorized(gfycatPreset))
            throw new UploadException("It was not possible to get the authorization to upload to Gfycat.");

        var headers = new NameValueCollection
        {
            { "Authorization", "Bearer " + gfycatPreset.AccessToken }
        };

        if (cancellationToken.IsCancellationRequested)
            return null;

        return await Upload(gfycatPreset, path, headers);
    }

    private async Task<History> Upload(GfycatPreset preset, string path, NameValueCollection headers)
    {
        var create = preset.AskForDetails ? Application.Current.Dispatcher.Invoke<GfycatCreateRequest>(() => UploadDetailsDialog.OkCancel(preset)) : preset.ToCreateRequest();

        var result = await WebHelper.Post("https://api.gfycat.com/v1/gfycats", Serializer.Serialize(create), headers);
        var createResponse = Serializer.Deserialize<GfycatCreateResponse>(result);
            
        if (createResponse.Error != null)
            return new GfycatHistory
            {
                PresetName = preset.Title,
                DateInUtc = DateTime.UtcNow,
                Result = 400,
                GfyName = createResponse.Name,
                Message = createResponse.Error.Code + " - " + createResponse.Error.Description
            };

        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var args = new Dictionary<string, string>
            {
                { "key", createResponse.Name }
            };

            //I can't send the authorization header along the file upload.
            result = await WebHelper.SendFile("https://filedrop.gfycat.com", stream, createResponse.Name, args);

            //If response is empty/null, it means that the file was already processed.
            var uploadResponse = string.IsNullOrWhiteSpace(result) ? null : Serializer.Deserialize<GfycatUploadResponse>(result);

            while (uploadResponse?.Task != "complete")
            {
                result = await WebHelper.Get("https://api.gfycat.com/v1/gfycats/fetch/status/" + createResponse.Name, headers);
                uploadResponse = Serializer.Deserialize<GfycatUploadResponse>(result);
                    
                if (uploadResponse.Task != "complete")
                    Thread.Sleep(1000);
                else if (uploadResponse.Task == "error")
                    return new GfycatHistory
                    {
                        PresetName = preset.Title,
                        DateInUtc = DateTime.UtcNow,
                        Result = 400,
                        GfyName = createResponse.Name,
                        Message = uploadResponse.Error?.Code + " - " + uploadResponse.Error?.Description
                    };
            }

            var history = new GfycatHistory
            {
                PresetName = preset.Title,
                DateInUtc = DateTime.UtcNow,
                Result = 200,
                Link = "https://gfycat.com/" + uploadResponse.GfyName,
                Size = uploadResponse.Mp4Size,
                DeletionLink = "https://gfycat.com/delete/" + createResponse.Secret,
                Mp4Url = uploadResponse.Mp4Url,
                WebmUrl = uploadResponse.WebmUrl,
                GifUrl = uploadResponse.GifUrl,
                MobileUrl = uploadResponse.MobileUrl,
                WebmSize = uploadResponse.WebmSize,
                GifSize = uploadResponse.GifSize,
                GfyId = uploadResponse.GfyId,
                GfyName = uploadResponse.GfyName
            };
                
            return history;
        }
    }

    [Obsolete("Maybe switch to using HttpClient, as it's a better solution")]
    public Task<History> UploadFileAsync(string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
    {
        //var handler = new HttpClientHandler
        //{
        //    Proxy = WebHelper.GetProxy(),
        //    PreAuthenticate = true,
        //    UseDefaultCredentials = false,
        //};

        //using (var client = new HttpClient(handler))
        //{
        //    //var request = new HttpRequestMessage
        //    //{
        //    //    RequestUri = new Uri("https://api.gfycat.com/v1/gfycats"),
        //    //    Method = HttpMethod.Get,
        //    //    Headers = { { "", ""} }
        //    //};

        //    using (var res = await client.PostAsync(@"https://api.gfycat.com/v1/gfycats", null, cancellationToken))
        //    {
        //        var result = await res.Content.ReadAsStringAsync();
        //        //{"isOk":true,"gfyname":"ThreeWordCode","secret":"15alphanumerics","uploadType":"filedrop.gfycat.com"}

        //        var ser = new JavaScriptSerializer();

        //        if (!(ser.DeserializeObject(result) is Dictionary<string, object> thing))
        //            throw new Exception("It was not possible to get the gfycat name: " + res);

        //        var name = thing["gfyname"] as string;

        //        using (var content = new MultipartFormDataContent())
        //        {
        //            content.Add(new StringContent(name), "key");
        //            content.Add(new ByteArrayContent(File.ReadAllBytes(path)), "file", name);

        //            using (var res2 = await client.PostAsync("https://filedrop.gfycat.com", content, cancellationToken))
        //            {
        //                if (!res2.IsSuccessStatusCode)
        //                    throw new Exception("It was not possible to get the gfycat upload result: " + res2);

        //                //{"task": "complete", "gfyname": "ThreeWordCode"}
        //                //{"progress": "0.03", "task": "encoding", "time": 10}

        //                //If the task is not yet completed, try waiting.
        //                var input2 = "";

        //                while (!input2.Contains("complete"))
        //                {
        //                    using (var res3 = await client.GetAsync("https://api.gfycat.com/v1/gfycats/fetch/status/" + name, cancellationToken))
        //                    {
        //                        input2 = await res3.Content.ReadAsStringAsync();

        //                        if (!res3.IsSuccessStatusCode)
        //                            throw new UploadingException("It was not possible to get the gfycat upload status: " + res3);
        //                    }

        //                    if (!input2.Contains("complete"))
        //                        Thread.Sleep(1000);
        //                }

        //                if (res2.IsSuccessStatusCode)
        //                    return new History { Link = "https://gfycat.com/" + name };
        //            }
        //        }
        //    }
        //}

        throw new UploadException("Unknown error");
    }


    public static async Task<bool> GetTokens(GfycatPreset preset)
    {
        var auth = new GfycatAuthRequest
        {
            GrantType = "client_credentials",
            ClientId = Secret.GfycatId,
            ClientSecret = Secret.GfycatSecret
        };

        return await GetTokens(preset, auth);
    }

    public static async Task<bool> GetTokens(GfycatPreset preset, string username, string password)
    {
        var auth = new GfycatAuthRequest
        {
            GrantType = "password",
            ClientId = Secret.GfycatId,
            ClientSecret = Secret.GfycatSecret,
            Username = username,
            Password = password
        };

        return await GetTokens(preset, auth);
    }

    public static async Task<bool> RefreshToken(GfycatPreset preset)
    {
        var auth = new GfycatAuthRequest
        {
            GrantType = "refresh",
            ClientId = Secret.GfycatId,
            ClientSecret = Secret.GfycatSecret,
            RefreshToken = preset.RefreshToken
        };

        return await GetTokens(preset, auth);
    }


    public static bool IsAuthorizationExpired(GfycatPreset preset)
    {
        return DateTime.UtcNow > preset.AccessTokenExpiryDate;
    }

    public static async Task<bool> IsAuthorized(IUploadPreset preset)
    {
        if (preset is not GfycatPreset gfycatPreset)
            return false;

        //When in anonymous mode, only the access token is used.
        if (preset.IsAnonymous)
        {
            //If the access token is still valid, no need to refresh it.
            if (!string.IsNullOrWhiteSpace(gfycatPreset.AccessToken) && IsAuthorizationExpired(gfycatPreset))
                return true;

            if (!await GetTokens(gfycatPreset))
                return false;
        }

        //When in authenticated mode, if there's no refresh token, it means that the app if not authorized.
        if (string.IsNullOrWhiteSpace(gfycatPreset.RefreshToken))
            return false;

        if (!IsAuthorizationExpired(gfycatPreset))
            return true;

        return await RefreshToken(gfycatPreset);
    }

        
    private static async Task<bool> GetTokens(GfycatPreset preset, GfycatAuthRequest auth)
    {
        var response = await WebHelper.Post("https://api.gfycat.com/v1/oauth/token", Serializer.Serialize(auth));

        if (string.IsNullOrEmpty(response))
            return false;

        var token = Serializer.Deserialize<OAuth2Token>(response);

        if (string.IsNullOrEmpty(token?.AccessToken))
            return false;

        preset.AccessToken = token.AccessToken;
        preset.RefreshToken = token.RefreshToken;
        preset.AccessTokenExpiryDate = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpiresIn - 10);
        preset.RefreshTokenExpiryDate = DateTime.UtcNow + TimeSpan.FromSeconds(token.RefreshTokenExpiresIn - 10);
        return true;
    }
}