using System;
using ScreenToGif.Util;

namespace ScreenToGif.Cloud
{
    public class CloudFactory
    {
        public static ICloud CreateCloud(UploadService service)
        {
            switch (service)
            {
                case UploadService.ImgurAnonymous:
                    return new Imgur.Imgur();
                case UploadService.Imgur:
                    return new Imgur.Imgur(false);
                case UploadService.GyfcatAnonymous:
                    return new Gfycat();
                //case UploadService.Gyfcat:
                //    return new Gfycat();
                case UploadService.Yandex:
                    return new YandexDisk.YandexDisk(UserSettings.All.YandexDiskOAuthToken);
            }

            throw new NotImplementedException();
        }
    }
}