using System;
using ScreenToGif.Cloud;
using ScreenToGif.Cloud.YandexDisk;
using ScreenToGif.Util;

namespace ScreenToGif.Services
{
    public class CloudFactory
    {
        public static ICloud CreateCloud(int id) // ToDo: Use name of cloud service
        {
            switch (id)
            {
                case 0:
                    return new Imgur();
                case 1:
                    return new Gfycat();
                case 2:
                    return new YandexDisk(UserSettings.All.YandexDiskOAuthToken);
            }

            throw new NotImplementedException();
        }
    }
}