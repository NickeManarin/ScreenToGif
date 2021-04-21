using System.Collections.Generic;
using ScreenToGif.Settings.Migrations;

namespace ScreenToGif.Settings
{
    internal static class Migration
    {
        internal static bool Migrate(List<Property> properties, string version)
        {
            switch (version)
            {
                case "0.0": //2.27.3 or older to 2.28.
                    Migration0to2_28_0.Up(properties);
                    goto case "2.28";

                case "2.28": //To 2.29
                case "2.28.1":
                case "2.28.2":
                    //Migration0to2_28_0.Up(properties);
                    goto default;

                default:
                {
                    properties.RemoveAll(p => p.Key == "Version");
                    return true;
                }
            }
        }
    }
}