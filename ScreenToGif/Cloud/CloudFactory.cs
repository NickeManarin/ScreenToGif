using System;
using ScreenToGif.Util;

namespace ScreenToGif.Cloud
{
    public class CloudFactory
    {
        public static IUploader CreateCloud(UploadType service)
        {
            switch (service)
            {
                case UploadType.Imgur:
                    return new Imgur.Imgur();
                case UploadType.Gfycat:
                    return new Gfycat.Gfycat();
                case UploadType.Yandex:
                    return new YandexDisk.YandexDisk();
            }

            throw new NotImplementedException();
        }
    }
}