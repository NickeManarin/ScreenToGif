using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Util;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Cloud.Imgur
{
    public class Imgur : ICloud
    {
        public bool IsAnonymous { get; set; }

        public Imgur(bool anonymous = true)
        {
            IsAnonymous = anonymous;
        }

        public async Task<UploadedFile> UploadFileAsync(string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
        {
            var args = new Dictionary<string, string>();
            var headers = new NameValueCollection();

            if (IsAnonymous)
            {
                headers.Add("Authorization", "Client-ID " + Secret.ImgurId);
            }
            else
            {
                headers.Add("Authorization", "Bearer " + UserSettings.All.ImgurAccessToken);

                if (!await IsAuthorized())
                    throw new UploadingException("It was not possible to get the authorization to upload to Imgur");

                if (UserSettings.All.ImgurUploadToAlbum)
                {
                    var album = string.IsNullOrWhiteSpace(UserSettings.All.ImgurSelectedAlbum) || UserSettings.All.ImgurSelectedAlbum == "♥♦♣♠" ? await AskForAlbum() : UserSettings.All.ImgurSelectedAlbum;

                    if (!string.IsNullOrEmpty(album))
                        args.Add("album", album);
                }
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var result = await WebHelper.SendFile("https://api.imgur.com/3/image", stream, path, args, headers);

                var responseAux = WebHelper.Deserialize<ImgurUploadImageResponse>(result);

                if (responseAux == null || (!responseAux.Success && responseAux.Status != 200))
                    throw new UploadingException("Upload failed: " + (responseAux?.Status.ToString() ?? "Response was null"));

                if (string.IsNullOrEmpty(responseAux.Data?.Link))
                    throw new UploadingException("Upload failed. The link was not provided.");

                var url = "";
                if ((IsAnonymous && UserSettings.All.ImgurAnonymousUseDirectLinks) || (!IsAnonymous && UserSettings.All.ImgurUseDirectLinks))
                {
                    if ((IsAnonymous && UserSettings.All.ImgurAnonymousUseGifvLink) || (!IsAnonymous && UserSettings.All.ImgurUseGifvLink) && !string.IsNullOrEmpty(responseAux.Data.Gifv))
                        url = responseAux.Data.Gifv ?? responseAux.Data.Link;
                    else
                        url = responseAux.Data.Link;
                }
                else
                {
                    url = $"https://imgur.com/{responseAux.Data.Id}";
                }
                
                return new UploadedFile { Link = url, DeleteLink = $"https://imgur.com/delete/{responseAux.Data.DeleteHash}" };
            }
        }


        public static string GetGetAuthorizationAdress()
        {
            var args = new Dictionary<string, string>
            {
                {"client_id", Secret.ImgurId}, {"response_type", "pin"}
            };

            return WebHelper.AppendQuery("https://api.imgur.com/oauth2/authorize", args);
        }

        public static async Task<bool> GetAccessToken()
        {
            var args = new Dictionary<string, string>
            {
                {"client_id", Secret.ImgurId},
                {"client_secret", Secret.ImgurSecret},
                {"grant_type", "pin"},
                {"pin", UserSettings.All.ImgurOAuthToken}
            };

            var response = await WebHelper.MultiRequest("https://api.imgur.com/oauth2/token", args);

            if (string.IsNullOrEmpty(response))
                return false;

            var token = WebHelper.Deserialize<OAuth2Token>(response);

            if (string.IsNullOrEmpty(token?.AccessToken))
                return false;

            UserSettings.All.ImgurAccessToken = token.AccessToken;
            UserSettings.All.ImgurRefreshToken = token.RefreshToken;
            UserSettings.All.ImgurExpireDate = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpiresIn - 10);
            return true;
        }

        public static async Task<bool> RefreshToken()
        {
            var args = new Dictionary<string, string>
            {
                {"refresh_token", UserSettings.All.ImgurRefreshToken},
                {"client_id", Secret.ImgurId},
                {"client_secret", Secret.ImgurSecret},
                {"grant_type", "refresh_token"}
            };

            var response = await WebHelper.MultiRequest("https://api.imgur.com/oauth2/token", args);

            if (string.IsNullOrEmpty(response)) return false;

            var token = WebHelper.Deserialize<OAuth2Token>(response);

            if (string.IsNullOrEmpty(token?.AccessToken))
                return false;

            UserSettings.All.ImgurAccessToken = token.AccessToken;
            UserSettings.All.ImgurRefreshToken = token.RefreshToken;
            UserSettings.All.ImgurExpireDate = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpiresIn - 10);
            return true;
        }

        public static bool IsAuthorizationExpired()
        {
            return DateTime.UtcNow > UserSettings.All.ImgurExpireDate;
        }

        public static async Task<bool> IsAuthorized()
        {
            if (string.IsNullOrWhiteSpace(UserSettings.All.ImgurAccessToken))
                return false;

            if (!IsAuthorizationExpired())
                return true;

            return await RefreshToken();
        }

        public static async Task<List<ImgurAlbumData>> GetAlbums()
        {
            if (!await IsAuthorized())
                return null;

            var headers = new NameValueCollection { { "Authorization", "Bearer " + UserSettings.All.ImgurAccessToken } };

            var response = await WebHelper.SimpleRequest(HttpMethod.Get, "https://api.imgur.com/3/account/me/albums", headers: headers);

            var responseAux = WebHelper.Deserialize<ImgurGetAlbumsResponse>(response);

            if (responseAux == null || (!responseAux.Success && responseAux.Status != 200))
                return null;

            UserSettings.All.ImgurAlbumList = new ArrayList(responseAux.Data);
            return responseAux.Data;
        }

        public static async Task<string> AskForAlbum()
        {
            var albums = await GetAlbums();

            //This looks ugly.
            var selected = Application.Current.Dispatcher.Invoke(() => Application.Current.Windows[0].Dispatcher.Invoke(() => PickAlbumDialog.OkCancel(albums)));

            return selected;
        }
    }
}