using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

internal class Migration2_28_0To2_29_0
{
    internal static bool Up(List<Property> properties)
    {
        //Remove deprecated properties.
        var removeKeys = new List<string>
        {
            "AsyncRecording"
        };
        properties.RemoveAll(r => removeKeys.Contains(r.Key));

        return true;
    }
}