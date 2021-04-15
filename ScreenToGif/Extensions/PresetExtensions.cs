using System.Linq;
using ScreenToGif.Model.UploadPresets;
using ScreenToGif.Settings;

namespace ScreenToGif.Extensions
{
    internal static class PresetExtensions
    {
        internal static void Persist(this UploadPreset preset, string previousTitle = null)
        {
            var current = UserSettings.All.UploadPresets.OfType<UploadPreset>().FirstOrDefault(f => f.Title == (previousTitle ?? preset.Title));

            if (current != null)
                UserSettings.All.UploadPresets.Remove(current);

            UserSettings.All.UploadPresets.Add(preset);
            UserSettings.Save();
        } 
    }
}