using System;
using ScreenToGif.Cloud;
using ScreenToGif.Cloud.YandexDisk;

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
                    return new YandexDisk("need token");
            }

            throw new NotImplementedException();
        }
    }
}