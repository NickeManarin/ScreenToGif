using ScreenToGif.Domain.Models;
using ScreenToGif.Settings.Migrations;
using ScreenToGif.Util.Settings.Migrations;

namespace ScreenToGif.Util.Settings;

public static class Migration
{
    public static bool Migrate(List<Property> properties, string version)
    {
        switch (version)
        {
            case "0.0": //2.27.3 or older to 2.28.
                Migration0To2_28_0.Up(properties);
                goto case "2.28";

            case "2.28": //To 2.29
            case "2.28.1":
            case "2.28.2":
                Migration2_28_0To2_29_0.Up(properties);
                goto case "2.29";

            case "2.29": //To 2.31
            case "2.29.1":
            case "2.30":
            case "2.30.1":
                Migration2_29_0To2_31_0.Up(properties);
                goto case "2.31";

            case "2.31": //To 2.32
                Migration2_31_0To2_32_0.Up(properties);
                goto case "2.32";

            case "2.32": //To 2.35
            case "2.32.1":
            case "2.33":
            case "2.33.1":
            case "2.34":
            case "2.34.1":
                Migration2_32_0To2_35_0.Up(properties);
                goto case "2.35";

            case "2.35": //To 2.36
            case "2.35.1":
            case "2.35.2":
            case "2.35.3":
            case "2.35.4":
                Migration2_35_0To2_36_0.Up(properties);
                goto case "2.36";

            case "2.36": //To 2.37
                Migration2_36_0To2_37_0.Up(properties);
                goto default;

            default:
            {
                properties.RemoveAll(p => p.Key == "Version");
                return true;
            }
        }
    }
}