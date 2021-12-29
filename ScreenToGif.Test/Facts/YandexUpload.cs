using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ScreenToGif.Cloud;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Test.Util;
using ScreenToGif.ViewModel.UploadPresets.Yandex;
using Xunit;

namespace ScreenToGif.Test.Facts
{
    public class YandexUpload
    {
        [Fact]
        public async Task CanUploadFile()
        {
            var cloud = CloudFactory.CreateCloud(UploadDestinations.Yandex);
            var preset = new YandexPreset
            {
                OAuthToken = "2344534523e45LW2jwerdp-efUwe4rmg" //Put your test token in here.
            };

            //Upload.
            var history = await cloud.UploadFileAsync(preset, "./Data/Test.txt", CancellationToken.None);

            Assert.NotNull(history);
            Assert.False(string.IsNullOrEmpty(history.Link));

            Trace.WriteLine("Link: " + history.Link);

            //Download.
            var data = await HttpHelper.HttpDownloadFileAsync(history.Link);

            Assert.NotNull(data);
        }

        [Fact]
        public async Task ThrowExceptionWhenUploadFileWithInvalidToken()
        {
            var cloud = CloudFactory.CreateCloud(UploadDestinations.Yandex);
            var preset = new YandexPreset
            {
                OAuthToken = "Invalid token"
            };

            //Upload.
            await Assert.ThrowsAsync<UploadException>(async () => await cloud.UploadFileAsync(preset, "./Data/Test.txt", CancellationToken.None));
        }
    }
}