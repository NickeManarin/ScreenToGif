using ScreenToGif.Domain.Models;

namespace ScreenToGif.Util.Settings.Migrations;

internal class Migration2_35_0To2_36_0
{
    internal static bool Up(List<Property> properties)
    {
        //Rename a property.
        var presets = properties.FirstOrDefault(f => f.Key == "ExportPresets");

        if (presets != null)
        {
            foreach (var child in presets.Children)
            {
                foreach (var attribute in child.Attributes)
                {
                    switch (attribute.Key)
                    {
                        case "OverwriteOnSave":
                        {
                            attribute.Key = "OverwriteMode";
                            attribute.Value = attribute.Value == "True" ? "Allow" : "Prompt";
                            break;
                        }
                    }
                }
            }
        }

        return true;
    }
}
