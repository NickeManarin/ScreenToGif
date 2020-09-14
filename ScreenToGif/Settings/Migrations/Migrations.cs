using System;

namespace ScreenToGif.Settings.Migrations
{
    internal static class Migrations
    {
        internal static bool Migrate(ref string type, ref string property, ref string value, Version appVersion, Version settingsVersion)
        {
            switch (settingsVersion.ToString())
            {
                //Treat versions older than v2.28 as v2.27.
                case "0.0":
                    new Migration0to2_28().Up(ref type, ref property, ref value);
                    goto case "2.28";

                case "2.28":
                    if (appVersion > settingsVersion)
                        new Migration0to2_28().Up(ref type, ref property, ref value); //2.28 to 2.29
                    else
                        new Migration0to2_28().Down(ref type, ref property, ref value);  //2.28 to 2.29

                    goto default;

                default:
                    
                    return true;
            }
        }
    }
}