using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ScreenToGif.Util
{
    internal static class WebHelper
    {
        internal static T Deserialize<T>(string json)
        {
            var ser = new DataContractJsonSerializer(typeof(T));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (T)ser.ReadObject(stream);
        }

        internal static string Protect(string str)
        {
            var entropy = Encoding.ASCII.GetBytes(Environment.MachineName);
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.ASCII.GetBytes(str), entropy, DataProtectionScope.CurrentUser));
        }

        internal static string Unprotect(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            var entropy = Encoding.ASCII.GetBytes(Environment.MachineName);
            return Encoding.ASCII.GetString(ProtectedData.Unprotect(Convert.FromBase64String(str), entropy, DataProtectionScope.CurrentUser));
        }


        internal static string CreateQuery(Dictionary<string, string> args)
        {
            if (args != null && args.Count > 0)
                return string.Join("&", args.Select(x => x.Key + "=" + HttpUtility.UrlEncode(x.Value)).ToArray());

            return "";
        }

        internal static string CreateQuery(string url, Dictionary<string, string> args)
        {
            var query = CreateQuery(args);

            if (!string.IsNullOrEmpty(query))
                return url + "?" + query;

            return url;
        }

        internal static async Task<string> SimpleRequest(HttpMethod method, string url, Stream data = null, string contentType = null, Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
        {
            using (var webResponse = await GetResponse(method, url, data, contentType, args, headers, cookies))
            {
                using (var responseStream = webResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        return null;

                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                        return reader.ReadToEnd();
                }
            }
        }

        internal static async Task<string> MultiRequest(string url, Dictionary<string, string> args, NameValueCollection headers = null, CookieCollection cookies = null)
        {
            var boundary = CreateBoundary();
            var contentType = "multipart/form-data; boundary=" + boundary;
            var data = MakeInputContent(boundary, args);

            using (var stream = new MemoryStream())
            {
                stream.Write(data, 0, data.Length);

                using (var webResponse = await GetResponse(HttpMethod.Post, url, stream, contentType, null, headers, cookies))
                {
                    using (var responseStream = webResponse.GetResponseStream())
                    {
                        if (responseStream == null)
                            return null;

                        using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                            return reader.ReadToEnd();
                    }
                }
            }
        }

        internal static async Task<string> SendFile(string url, Stream data, string output, Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
        {
            var boundary = CreateBoundary();
            var contentType = "multipart/form-data; boundary=" + boundary;

            var bytesArguments = MakeInputContent(boundary, args, false);
            var bytesDataOpen = MakeFileInputContentOpen(boundary, "image", output);
            var bytesDataClose = MakeFileInputContentClose(boundary);

            var request = PrepareWebRequest(HttpMethod.Post, url, headers, cookies, contentType, bytesArguments.Length + bytesDataOpen.Length + data.Length + bytesDataClose.Length);

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                requestStream.Write(bytesArguments, 0, bytesArguments.Length);
                requestStream.Write(bytesDataOpen, 0, bytesDataOpen.Length);

                TransferData(data, requestStream);

                requestStream.Write(bytesDataClose, 0, bytesDataClose.Length);
            }

            using (var response = await request.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                        return null;

                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                        return reader.ReadToEnd();
                }
            }
        }

        private static string CreateBoundary()
        {
            return new string('-', 20) + DateTime.Now.Ticks.ToString("x");
        }

        private static byte[] MakeInputContent(string boundary, Dictionary<string, string> contents, bool isFinal = true)
        {
            using (var stream = new MemoryStream())
            {
                if (string.IsNullOrEmpty(boundary))
                    boundary = CreateBoundary();

                if (contents == null)
                    return stream.ToArray();

                byte[] bytes;
                foreach (var content in contents)
                {
                    if (string.IsNullOrEmpty(content.Key) || string.IsNullOrEmpty(content.Value))
                        continue;

                    bytes = MakeInputContent(boundary, content.Key, content.Value);
                    stream.Write(bytes, 0, bytes.Length);
                }

                if (!isFinal)
                    return stream.ToArray();

                bytes = MakeFinalBoundary(boundary);
                stream.Write(bytes, 0, bytes.Length);

                return stream.ToArray();
            }
        }

        private static byte[] MakeInputContent(string boundary, string name, string value)
        {
            return Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n");
        }

        private static byte[] MakeFinalBoundary(string boundary)
        {
            return Encoding.UTF8.GetBytes($"--{boundary}--\r\n");
        }

        private static byte[] MakeFileInputContentOpen(string boundary, string fileFormName, string fileName)
        {
            return Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"{fileFormName}\"; filename=\"{fileName}\"\r\nContent-Type: image/gif\r\n\r\n");
        }

        private static byte[] MakeFileInputContentClose(string boundary)
        {
            return Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");
        }

        private static Task<WebResponse> GetResponse(HttpMethod method, string url, Stream data = null, string contentType = null, Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
        {
            try
            {
                url = CreateQuery(url, args);

                long length = 0;

                if (data != null)
                    length = data.Length;

                var request = PrepareWebRequest(method, url, headers, cookies, contentType, length);

                if (length <= 0)
                    return request.GetResponseAsync();

                using (var requestStream = request.GetRequestStream())
                    TransferData(data, requestStream);

                return request.GetResponseAsync();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Get response: " + url);
            }

            return null;
        }

        private static HttpWebRequest PrepareWebRequest(HttpMethod method, string url, NameValueCollection headers = null, CookieCollection cookies = null, string contentType = null, long contentLength = 0)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = method.ToString();

            if (headers != null)
            {
                if (headers["Accept"] != null)
                {
                    request.Accept = headers["Accept"];
                    headers.Remove("Accept");
                }

                if (headers["Content-Length"] != null)
                {
                    request.ContentLength = Convert.ToInt32(headers["Content-Length"]);
                    headers.Remove("Content-Length");
                }

                request.Headers.Add(headers);
            }

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            request.Proxy = GetProxy();
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
            request.ContentType = contentType;

            if (contentLength > 0)
            {
                request.AllowWriteStreamBuffering = IsProxyBeingUsed();

                if (method == HttpMethod.Get)
                    request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

                request.ContentLength = contentLength;
                request.Pipelined = false;
                request.Timeout = -1;
            }
            else
            {
                request.KeepAlive = false;
            }

            return request;
        }

        private static void TransferData(Stream dataStream, Stream requestStream)
        {
            if (dataStream.CanSeek)
                dataStream.Position = 0;

            var length = (int)Math.Min(8192, dataStream.Length);
            var buffer = new byte[length];
            int bytesRead;

            while ((bytesRead = dataStream.Read(buffer, 0, length)) > 0)
                requestStream.Write(buffer, 0, bytesRead);
        }

        internal static IWebProxy GetProxy()
        {
            if (UserSettings.All.ProxyMode == ProxyType.System)
                return WebRequest.GetSystemWebProxy();
            
            if (UserSettings.All.ProxyMode == ProxyType.Manual)
                return string.IsNullOrEmpty(UserSettings.All.ProxyHost) || UserSettings.All.ProxyPort <= 0 ? null :
                    new WebProxy($"{UserSettings.All.ProxyHost}:{UserSettings.All.ProxyPort}", true, null, new NetworkCredential(UserSettings.All.ProxyUsername, Unprotect(UserSettings.All.ProxyPassword)));

            return null;
        }

        internal static bool IsProxyBeingUsed()
        {
            if (UserSettings.All.ProxyMode == ProxyType.System)
                return true;

            if (UserSettings.All.ProxyMode == ProxyType.Manual)
                return !string.IsNullOrEmpty(UserSettings.All.ProxyHost) && UserSettings.All.ProxyPort > 0;

            return false;
        }
    }
}