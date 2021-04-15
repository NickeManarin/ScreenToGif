namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Webp
{
    public class WebpPreset : AnimatedImagePreset
    {
        public WebpPreset()
        {
            Type = Util.Export.Webp;
            DefaultExtension = ".webp";
            Extension = ".webp";
        }
    }
}