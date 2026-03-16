using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

internal class Migration2_37_0To2_43_0
{
    internal static bool Up(List<Property> properties)
    {
        //Change the poreset value for a specific preset.
        var presets = properties.FirstOrDefault(f => f.Key == "ExportPresets");

        if (presets != null)
        {
            foreach (var child in presets.Children)
            {
                if (!child.Type.Equals("GifskiGifPreset"))
                    continue;

                if (!child.Attributes.Any(a => a.Key.Equals("DescriptionKey") && a.Value.Equals("S.Preset.Gif.Gifski.High.Description")))
                    continue;

                foreach (var attribute in child.Attributes)
                {
                    switch (attribute.Key)
                    {
                        case "Quality":
                        {
                            attribute.Value = "80";
                            return true; //Only one item, so return earlier.
                        }
                    }
                }
            }
        }

        return true;
    }
}