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
    /// <summary>
    /// Web related methods.
    /// Boundary: http://www.w3.org/Protocols/rfc1341/7_2_Multipart.html
    /// </summary>
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

        internal static string AppendQuery(string url, Dictionary<string, string> args)
        {
            if (args == null)
                return url;

            var suffix = args.Select(s => s.Key + "=" + HttpUtility.UrlEncode(s.Value)).Aggregate((p, n) => p + "&" + n);

            return url + (string.IsNullOrWhiteSpace(suffix) ? "" : "?" + suffix);
        }


        internal static async Task<string> SimpleRequest(HttpMethod method, string url, NameValueCollection headers = null)
        {
            using (var webResponse = await GetResponse(method, url, null, null, headers))
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

        internal static async Task<string> MultiRequest(string url, Dictionary<string, string> args)
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteStringUtf8(GetMultipartString("+fringe+", args));

                using (var webResponse = await GetResponse(HttpMethod.Post, url, stream, "multipart/form-data; boundary=+fringe+"))
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

        internal static async Task<string> SendFile(string url, Stream data, string filename, Dictionary<string, string> args = null, NameValueCollection headers = null)
        {
            using (var head = GetMultipartStream("+fringe+", args, filename, data))
            {
                var request = GetWebRequest(HttpMethod.Post, url, headers, "multipart/form-data; boundary=+fringe+", head.Length);

                using (var requestStream = await request.GetRequestStreamAsync())
                    requestStream.WriteStream(head);

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
        }

        private static Stream GetMultipartStream(string border, Dictionary<string, string> args, string filename, Stream data)
        {
            var stream = new MemoryStream();

            foreach (var content in args.Where(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value)))
                stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name=\"{content.Key}\"\r\n\r\n{content.Value}\r\n");

            if (!string.IsNullOrWhiteSpace(filename))
                stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"{filename}\"\r\nContent-Type: image/gif\r\n\r\n");

            stream.WriteStream(data);

            stream.WriteStringUtf8($"\r\n--{border}--\r\n");

            return stream;
        }

        private static string GetMultipartString(string border, Dictionary<string, string> args)
        {
            return args.Where(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value))
                .Aggregate("", (p, n) => p + $"--{border}\r\nContent-Disposition: form-data; name=\"{n.Key}\"\r\n\r\n{n.Value}\r\n") + $"--{border}--\r\n";
        }

        private static Task<WebResponse> GetResponse(HttpMethod method, string url, Stream data = null, string contentType = null, NameValueCollection headers = null)
        {
            try
            {
                var length = data?.Length ?? 0;

                var request = GetWebRequest(method, url, headers, contentType, length);

                if (length <= 0)
                    return request.GetResponseAsync();

                using (var requestStream = request.GetRequestStream())
                    requestStream.WriteStream(data);

                return request.GetResponseAsync();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Get response: " + url);
            }

            return null;
        }

        private static HttpWebRequest GetWebRequest(HttpMethod method, string url, NameValueCollection headers = null, string contentType = null, long contentLength = 0)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (headers != null)
                request.Headers.Add(headers);

            request.Method = method.ToString();
            request.Proxy = GetProxy();
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
            request.ContentType = contentType;

            if (contentLength == 0)
            {
                request.KeepAlive = false;
                return request;
            }

            if (method == HttpMethod.Get)
                request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

            request.AllowWriteStreamBuffering = IsProxyBeingUsed();
            request.ContentLength = contentLength;
            request.Pipelined = false;
            request.Timeout = -1;
            return request;
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