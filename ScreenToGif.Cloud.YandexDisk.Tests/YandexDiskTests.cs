using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScreenToGif.Cloud.YandexDisk.Tests
{
    [TestClass]
    public class YandexDiskTests
    {
        [TestMethod]
        public async Task CanUploadFile()
        {
            var cloud = new YandexDisk("Paste your OAuth token");

            // upload
            var result = await cloud.UploadFileAsync("1.txt", CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(false, string.IsNullOrEmpty(result.Link));

            Trace.WriteLine("link: " + result.Link);

            // download
            var data = new WebClient().DownloadData(result.Link);
            Assert.IsNotNull(data);
        }
    }
}
