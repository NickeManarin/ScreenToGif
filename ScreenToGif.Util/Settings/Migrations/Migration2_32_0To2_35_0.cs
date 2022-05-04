using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

internal class Migration2_32_0To2_35_0
{
    internal static bool Up(List<Property> properties)
    {
        UpdateNamespaces(properties);

        //Remove deprecated properties.
        var removeKeys = new List<string>
        {
            "EditorExtendChrome",
            "SharpDxLocationFolder"
        };
        properties.RemoveAll(r => removeKeys.Contains(r.Key));

        return true;
    }

    private static void UpdateNamespaces(List<Property> properties)
    {
        foreach (var child in properties)
        {
            switch (child.Type)
            {
                case "ArrayList":
                {
                    UpdateNamespaces(child.Children);
                    break;
                }
                case "FfmpegAviPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Video.Avi;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "FfmpegMkvPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Video.Mkv;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "FfmpegMovPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Video.Mov;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "FfmpegMp4Preset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Video.Mp4;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "FfmpegWebmPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Video.Webm;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "BmpPreset":
                case "JpegPreset":
                case "PngPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Image;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "PsdPreset":
                case "StgPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.Other;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "EmbeddedApngPreset":
                case "FfmpegApngPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Apng;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "EmbeddedGifPreset":
                case "FfmpegGifPreset":
                case "GifskiGifPreset":
                case "SystemGifPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "FfmpegWebpPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Webp;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "ImgurPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.UploadPresets.Imgur;assembly=ScreenToGif.ViewModel";

                    UpdateNamespaces(child.Attributes);
                    break;
                }
                case "GfycatPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.UploadPresets.Gfycat;assembly=ScreenToGif.ViewModel";

                    UpdateNamespaces(child.Attributes);
                    break;
                }
                case "YandexPreset":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.UploadPresets.Yandex;assembly=ScreenToGif.ViewModel";

                    UpdateNamespaces(child.Attributes);
                    break;
                }
                case "ImgurAlbum":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.UploadPresets.Imgur;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "History":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.UploadPresets.History;assembly=ScreenToGif.ViewModel";
                    break;
                }
                case "MouseClicksViewModel":
                case "KeyStrokesViewModel":
                case "DelayViewModel":
                case "ProgressViewModel":
                case "BorderViewModel":
                case "ShadowViewModel":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.Tasks;assembly=ScreenToGif.ViewModel";
                    break;
                }

                case "CaptureFrequency":
                {
                    child.Type = "CaptureFrequencies";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "CopyType":
                {
                    child.Type = "CopyModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "AppTheme":
                {
                    child.Type = "AppThemes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "ProxyType":
                {
                    child.Type = "ProxyTypes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "Export":
                case "ExportType":
                {
                    child.Type = "ExportFormats";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "PasteBehavior":
                {
                    child.Type = "PasteBehaviors";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "DelayUpdateType":
                {
                    child.Type = "DelayUpdateModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "ReduceDelayType":
                {
                    child.Type = "ReduceDelayModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "DuplicatesRemovalType":
                {
                    child.Type = "DuplicatesRemovalModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "DuplicatesDelayType":
                {
                    child.Type = "DuplicatesDelayModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "SizeUnits":
                {
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "ObfuscationMode":
                {
                    child.Type = "ObfuscationModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "FadeToType":
                {
                    child.Type = "FadeModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "ProgressType":
                {
                    child.Type = "ProgressTypes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }

                case "UploadType":
                {
                    child.Type = "UploadDestinations";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "EncoderType":
                {
                    child.Type = "EncoderTypes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "PartialExportType":
                {
                    child.Type = "PartialExportModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }
                case "VideoSettingsMode":
                {
                    child.Type = "VideoSettingsModes";
                    child.NameSpace = "clr-namespace:ScreenToGif.Domain.Enums;assembly=ScreenToGif.Domain";
                    break;
                }

                default:
                {
                    break;
                }
            }
        }
    }
}