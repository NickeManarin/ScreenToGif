using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ScreenToGif.Util;

namespace ScreenToGif.Cloud
{
    public class Imgur : ICloud
    {
        public async Task<UploadedFile> UploadFileAsync(string path, CancellationToken cancellationToken, IProgress<double> progressCallback = null)
        {
            using (var w = new WebClient())
            {
                w.Headers.Add("Authorization", "Client-ID " + Secret.ImgurId);
                var values = new NameValueCollection { { "image", Convert.ToBase64String(File.ReadAllBytes(path)) } };
                var response = await w.UploadValuesTaskAsync("https://api.imgur.com/3/upload.xml", values);
                var x = XDocument.Load(new MemoryStream(response));

                var node = x.Descendants().FirstOrDefault(n => n.Name == "link");
                var nodeHash = x.Descendants().FirstOrDefault(n => n.Name == "deletehash");

                if (node == null)
                    throw new UploadingException("No link was provided by Imgur", new Exception(x.Document?.ToString() ?? "The document was null. :/"));

                return new UploadedFile() {Link = node.Value, DeleteLink = "https://imgur.com/delete/" + nodeHash?.Value};
            }
        }
    }
}