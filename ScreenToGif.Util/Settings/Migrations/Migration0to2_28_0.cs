using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

// ReSharper disable once InconsistentNaming
public static class Migration0To2_28_0
{
    public static bool Up(List<Property> properties)
    {
        #region Upload presets

        var uploadPresets = new Property
        {
            Type = "ArrayList",
            NameSpace = "clr-namespace:System.Collections;assembly=mscorlib",
            Key = "UploadPresets"
        };
            
        //Imgur (authenticated).
        var imgurRefreshToken = properties.FirstOrDefault(f => f.Key == "ImgurRefreshToken")?.Value;
            
        if (!string.IsNullOrWhiteSpace(imgurRefreshToken))
        {
            var albums = new Property
            {
                Type = "ArrayList",
                NameSpace = "clr-namespace:System.Collections;assembly=mscorlib",
                Key = "Albums"
            };

            foreach (var album in properties.FirstOrDefault(f => f.Key == "ImgurAlbumList")?.Children ?? new List<Property>())
            {
                albums.Children.Add(new Property
                {
                    Type = "ImgurAlbum",
                    NameSpace = "clr-namespace:ScreenToGif.Model.UploadPresets.Imgur",
                    Attributes = new List<Property>
                    {
                        new Property { Key = "Id", Value = album.Attributes.FirstOrDefault(f => f.Key == "Id")?.Value },
                        new Property { Key = "Title", Value = album.Attributes.FirstOrDefault(f => f.Key == "Title")?.Value },
                        new Property { Key = "Description", Value = album.Attributes.FirstOrDefault(f => f.Key == "Description")?.Value },
                        new Property { Key = "Link", Value = album.Attributes.FirstOrDefault(f => f.Key == "Link")?.Value },
                        new Property { Key = "Privacy", Value = album.Attributes.FirstOrDefault(f => f.Key == "Privacy")?.Value },
                        new Property { Key = "Nsfw", Value = album.Attributes.FirstOrDefault(f => f.Key == "Nsfw")?.Value },
                        new Property { Key = "ImagesCount", Value = album.Attributes.FirstOrDefault(f => f.Key == "ImagesCount")?.Value },
                    }
                });
            }

            uploadPresets.Children.Add(new Property
            {
                Type = "ImgurPreset",
                NameSpace = "clr-namespace:ScreenToGif.Model.UploadPresets.Imgur",
                Attributes = new List<Property>
                {
                    new Property { Key = "Title", Value = "Imgur" },
                    new Property { Key = "AccessToken", Value = properties.FirstOrDefault(f => f.Key == "ImgurAccessToken")?.Value },
                    new Property { Key = "RefreshToken", Value = properties.FirstOrDefault(f => f.Key == "ImgurRefreshToken")?.Value },
                    new Property { Key = "ExpiryDate", Value = properties.FirstOrDefault(f => f.Key == "ImgurExpireDate")?.Value },
                    new Property { Key = "UseDirectLinks", Value = properties.FirstOrDefault(f => f.Key == "ImgurUseDirectLinks")?.Value ?? "False" },
                    new Property { Key = "UseGifvLinks", Value = properties.FirstOrDefault(f => f.Key == "ImgurUseGifvLink")?.Value ?? "False" },
                    new Property { Key = "UploadToAlbum", Value = properties.FirstOrDefault(f => f.Key == "ImgurUploadToAlbum")?.Value ?? "False" },
                    new Property { Key = "SelectedAlbum", Value = properties.FirstOrDefault(f => f.Key == "ImgurSelectedAlbum")?.Value },
                    albums
                }, 
            });
        }

        //Yandex.
        var yandexToken = properties.FirstOrDefault(f => f.Key == "YandexDiskOAuthToken")?.Value;

        if (!string.IsNullOrWhiteSpace(yandexToken))
        {
            uploadPresets.Children.Add(new Property
            {
                Type = "YandexPreset",
                NameSpace = "clr-namespace:ScreenToGif.Model.UploadPresets.Yandex",
                Attributes = new List<Property>
                {
                    new Property { Key = "Title", Value = "Yandex" },
                    new Property { Key = "OAuthToken", Value = yandexToken }
                }
            });
        }

        if (uploadPresets.Children.Any())
            properties.Add(uploadPresets);

        #endregion

        //Remove deprecated properties.
        var removeKeys = new List<string>
        {
            "GifEncoder",
            "ApngEncoder",
            "VideoEncoder",
            "IsGifOptionsExpanded",
            "IsApngOptionsExpanded",
            "IsVideoOptionsExpanded",
            "IsPsdOptionsExpanded",
            "IsProjectOptionsExpanded",
            "IsSaveOptionsExpanded",
            "IsGifOptionsExpanded",
            "IsApngOptionsExpanded",
            "IsVideoOptionsExpanded",
            "IsPsdOptionsExpanded",
            "IsProjectOptionsExpanded",
            "IsSaveOptionsExpanded",
            "ColorQuantization",
            "SamplingFactor",
            "GifskiQuality",
            "MaximumColors",
            "UseGlobalColorTable",
            "Looped",
            "RepeatForever",
            "RepeatCount",
            "EnableTransparency",
            "SelectTransparencyColor",
            "TransparencyColor",
            "DetectUnchanged",
            "PaintTransparent",
            "ChromaKey",
            "LatestOutputFolder",
            "LatestFilename",
            "LatestExtension",
            "PickLocation",
            "OverwriteOnSave",
            "SaveAsProjectToo",
            "UploadFile",
            "LatestUploadService",
            "SaveToClipboard",
            "LatestCopyType",
            "ExecuteCustomCommands",
            "CustomCommands",
            "DetectUnchangedApng",
            "PaintTransparentApng",
            "LoopedApng",
            "RepeatCountApng",
            "RepeatForeverApng",
            "LatestApngOutputFolder",
            "LatestApngFilename",
            "LatestApngExtension",
            "PickLocationApng",
            "OverwriteOnSaveApng",
            "SaveAsProjectTooApng",
            "UploadFileApng",
            "LatestUploadServiceApng",
            "SaveToClipboardApng",
            "LatestCopyTypeApng",
            "ExecuteCustomCommandsApng",
            "CustomCommandsApng",
            "AviQuality",
            "FlipVideo",
            "OutputFramerate",
            "FfmpegPresets",
            "LatestVideoOutputFolder",
            "LatestVideoFilename",
            "LatestVideoExtension",
            "PickLocationVideo",
            "OverwriteOnSaveVideo",
            "SaveAsProjectTooVideo",
            "SaveToClipboardVideo",
            "LatestCopyTypeVideo",
            "ExecuteCustomCommandsVideo",
            "CustomCommandsVideo",
            "CompressionLevelProject",
            "LatestProjectOutputFolder",
            "LatestProjectFilename",
            "LatestProjectExtension",
            "OverwriteOnSaveProject",
            "SaveToClipboardProject",
            "LatestCopyTypeProject",
            "ZipImages",
            "LatestImageOutputFolder",
            "LatestImageFilename",
            "LatestImageExtension",
            "OverwriteOnSaveImages",
            "CompressImage",
            "SaveTimeline",
            "MaximizeCompatibility",
            "LatestPhotoshopOutputFolder",
            "LatestPhotoshopFilename",
            "LatestPhotoshopExtension",
            "PickLocationPhotoshop",
            "OverwriteOnSavePhotoshop",
            "SaveAsProjectTooPhotoshop",
            "SaveToClipboardPhotoshop",
            "LatestCopyTypePhotoshop",
            "ExecuteCustomCommandsPhotoshop",
            "CustomCommandsPhotoshop",

            "Quality",
            "SnapshotMode",
            "SnapshotDefaultDelay",
            "DetectMouseClicks",
            "ClickColor",
            "FullScreenMode",
            "ExtraParametersGifski",
            "LatestUploadIndex",
            "ExtraParameters",
            "ExtraParametersGif",
            "ExtraParametersApngFFmpeg",

            "ImgurAnonymousUseDirectLinks",
            "ImgurAnonymousUseGifvLink",
            "ImgurOAuthToken",
            "ImgurAccessToken",
            "ImgurRefreshToken",
            "ImgurExpireDate",
            "ImgurUseDirectLinks",
            "ImgurUseGifvLink",
            "ImgurUploadToAlbum",
            "ImgurSelectedAlbum",
            "ImgurAlbumList",
            "YandexDiskOAuthToken",
        };
        properties.RemoveAll(r => removeKeys.Contains(r.Key));

        return true;
    }
}