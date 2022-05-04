using ScreenToGif.Domain.Enums;
using ScreenToGif.Util.Settings;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace ScreenToGif.Util;

/// <summary>
/// Web related methods.
/// Boundary: http://www.w3.org/Protocols/rfc1341/7_2_Multipart.html
/// </summary>
public static class WebHelper
{
    private const string Boundary = "+++fringe+++";

    public static string Protect(string str)
    {
        var entropy = Encoding.ASCII.GetBytes(Environment.MachineName);
        return Convert.ToBase64String(ProtectedData.Protect(Encoding.ASCII.GetBytes(str), entropy, DataProtectionScope.CurrentUser));
    }

    public static string Unprotect(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        var entropy = Encoding.ASCII.GetBytes(Environment.MachineName);
        return Encoding.ASCII.GetString(ProtectedData.Unprotect(Convert.FromBase64String(str), entropy, DataProtectionScope.CurrentUser));
    }

    public static string AppendQuery(string url, Dictionary<string, string> args)
    {
        if (args == null)
            return url;

        var suffix = args.Select(s => s.Key + "=" + WebUtility.UrlEncode(s.Value)).Aggregate((p, n) => p + "&" + n);

        return url + (string.IsNullOrWhiteSpace(suffix) ? "" : "?" + suffix);
    }


    public static async Task<string> Get(string url, NameValueCollection headers = null)
    {
        using (var webResponse = await GetResponse(HttpMethod.Get, url, headers))
        {
            await using (var responseStream = webResponse.GetResponseStream())
            {
                if (responseStream == null)
                    return null;

                using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    return await reader.ReadToEndAsync();
            }
        }
    }

    public static async Task<string> Post(string url, string content, NameValueCollection headers = null)
    {
        using (var webResponse = await GetResponse(HttpMethod.Post, url, content, "application/json", headers))
        {
            await using (var responseStream = webResponse.GetResponseStream())
            {
                if (responseStream == null)
                    return null;

                using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    return await reader.ReadToEndAsync();
            }
        }
    }

    public static async Task<string> PostMultipart(string url, Dictionary<string, string> args)
    {
        await using (var stream = new MemoryStream())
        {
            stream.WriteStringUtf8(GetMultipartString(Boundary, args));

            using (var webResponse = await GetResponse(HttpMethod.Post, url, stream, "multipart/form-data; boundary=" + Boundary))
            {
                await using (var responseStream = webResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        return null;

                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                        return await reader.ReadToEndAsync();
                }
            }
        }
    }

    public static async Task<string> SendFile(string url, Stream data, string filename, Dictionary<string, string> args = null, NameValueCollection headers = null, string streamName = "file")
    {
        await using (var head = GetMultipartStream(Boundary, args, filename, data, streamName))
        {
            using (var webResponse = await GetResponse(HttpMethod.Post, url, head, "multipart/form-data; boundary=" + Boundary, headers))
            {
                await using (var responseStream = webResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        return null;

                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                        return await reader.ReadToEndAsync();
                }
            }
        }
    }

    public static async Task<string> SendFile2(string url, Stream data, string filename, Dictionary<string, string> args = null, NameValueCollection headers = null)
    {
        await using (var head = GetMultipartStream(Boundary, args, filename, data))
        {
            var request = GetWebRequest(HttpMethod.Post, url, headers, "multipart/form-data; boundary=" + Boundary, head.Length);

            await using (var requestStream = await request.GetRequestStreamAsync())
                requestStream.WriteStream(head);

            using (var response = await request.GetResponseAsync())
            {
                await using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                        return null;

                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                        return await reader.ReadToEndAsync();
                }
            }
        }
    }

    public static Stream GetMultipartStream(string border, Dictionary<string, string> args, string filename, Stream data, string streamName = "file")
    {
        var stream = new MemoryStream();

        if (args.Any(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value)))
            stream.WriteStringUtf8("Content-Type: text/plain; charset=utf-8");

        foreach (var content in args.Where(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value)))
            stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name=\"{content.Key}\"\r\n\r\n{content.Value}\r\n");

        if (!string.IsNullOrWhiteSpace(filename))
            stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name={streamName}; filename={filename};\r\n\r\n");
        //stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"{filename}\"\r\nContent-Type: image/gif\r\n\r\n"); //TODO: Fixed content type.

        stream.WriteStream(data);
            
        stream.WriteStringUtf8($"\r\n--{border}--\r\n");

        stream.Position = 0;
        return stream;
    }

    public static string GetMultipartString(string border, Dictionary<string, string> args)
    {
        return args.Where(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value))
            .Aggregate("", (p, n) => p + $"--{border}\r\nContent-Disposition: form-data; name=\"{n.Key}\"\r\n\r\n{n.Value}\r\n") + $"--{border}--\r\n";
    }


    public static Task<WebResponse> GetResponse(HttpMethod method, string url, NameValueCollection headers = null)
    {
        try
        {
            return GetWebRequest(method, url, headers).GetResponseAsync();
        }
        catch (WebException we)
        {
            if (we.Response is not WebResponse resp)
                throw;

            return Task.FromResult(resp);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Get response: " + url);
        }

        return null;
    }

    private static Task<WebResponse> GetResponse(HttpMethod method, string url, string content, string contentType = null, NameValueCollection headers = null)
    {
        try
        {
            var request = GetWebRequest(method, url, headers, contentType, content.Length);

            if (content.Length > 0)
                using (var requestStream = request.GetRequestStream())
                    requestStream.WriteStringUtf8(content);

            return request.GetResponseAsync();
        }
        catch (WebException we)
        {
            if (we.Response is not WebResponse resp)
                throw;

            return Task.FromResult(resp);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Get response: " + url);
        }

        return Task.FromResult<WebResponse>(null);
    }

    private static async Task<WebResponse> GetResponse(HttpMethod method, string url, Stream data, string contentType = null, NameValueCollection headers = null)
    {
        try
        {
            var request = GetWebRequest(method, url, headers, contentType, data?.Length ?? 0);
                
            if (request.ContentLength > 0)
                await using (var requestStream = await request.GetRequestStreamAsync())
                    requestStream.WriteStream(data);

            return await request.GetResponseAsync();
        }
        catch (WebException we)
        {
            if (we.Response is not WebResponse resp)
                throw;

            return resp;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Get response: " + url);
        }

        return null;
    }


    private static HttpWebRequest GetWebRequest(HttpMethod method, string url, NameValueCollection headers = null, string contentType = null, long contentLength = 0)
    {
        var request = (HttpWebRequest) WebRequest.Create(url);

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


    public static IWebProxy GetProxy()
    {
        if (UserSettings.All.ProxyMode == ProxyTypes.System)
            return WebRequest.GetSystemWebProxy();

        if (UserSettings.All.ProxyMode == ProxyTypes.Manual)
            return string.IsNullOrEmpty(UserSettings.All.ProxyHost) || UserSettings.All.ProxyPort <= 0 ? null :
                new WebProxy($"{UserSettings.All.ProxyHost}:{UserSettings.All.ProxyPort}", true, null, new NetworkCredential(UserSettings.All.ProxyUsername, Unprotect(UserSettings.All.ProxyPassword)));

        return null;
    }

    internal static bool IsProxyBeingUsed()
    {
        if (UserSettings.All.ProxyMode == ProxyTypes.System)
            return true;

        if (UserSettings.All.ProxyMode == ProxyTypes.Manual)
            return !string.IsNullOrEmpty(UserSettings.All.ProxyHost) && UserSettings.All.ProxyPort > 0;

        return false;
    }
}