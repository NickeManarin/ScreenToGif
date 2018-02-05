using System;
using ScreenToGif.Util;

namespace ScreenToGif.Cloud
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
                    return new YandexDisk.YandexDisk(UserSettings.All.YandexDiskOAuthToken);
            }

            throw new NotImplementedException();
        }
    }
}