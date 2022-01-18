using System.Linq;
using ScreenToGif.Domain.Events;
using System.Threading.Tasks;
using ScreenToGif.Cloud;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.ViewModel.UploadPresets.Gfycat;
using ScreenToGif.ViewModel.UploadPresets.Imgur;
using ScreenToGif.ViewModel.UploadPresets.Yandex;
using ScreenToGif.Windows;

namespace ScreenToGif.Util.Extensions;

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

    public static async Task<ValidatedEventArgs> IsValid(UploadPreset preset)
    {
        switch (preset)
        {
            case GfycatPreset gfycat:
                return await IsValid(gfycat);

            case ImgurPreset imgur:
                return await IsValid(imgur);

            case YandexPreset yandex:
                return await IsValid(yandex);
        }

        return await preset.IsValid();
    }

    public static async Task<ValidatedEventArgs> IsValid(GfycatPreset preset)
    {
        if (!preset.IsAnonymous && !await Gfycat.IsAuthorized(preset))
            return new ValidatedEventArgs("S.SaveAs.Warning.Upload.NotAuthorized", StatusReasons.UploadServiceUnauthorized, () => App.MainViewModel.OpenOptions.Execute(Options.UploadIndex));

        return await preset.IsValid();
    }

    public static async Task<ValidatedEventArgs> IsValid(ImgurPreset preset)
    {
        if (!preset.IsAnonymous && !await Imgur.IsAuthorized(preset))
            return new ValidatedEventArgs("S.SaveAs.Warning.Upload.NotAuthorized", StatusReasons.UploadServiceUnauthorized, () => App.MainViewModel.OpenOptions.Execute(Options.UploadIndex));

        return await preset.IsValid();
    }

    public static async Task<ValidatedEventArgs> IsValid(YandexPreset preset)
    {
        if (!preset.IsAnonymous && !YandexDisk.IsAuthorized(preset))
            return new ValidatedEventArgs("S.SaveAs.Warning.Upload.NotAuthorized", StatusReasons.UploadServiceUnauthorized, () => App.MainViewModel.OpenOptions.Execute(Options.UploadIndex));

        return await preset.IsValid();
    }
}