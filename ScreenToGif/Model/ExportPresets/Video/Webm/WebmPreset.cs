namespace ScreenToGif.Model.ExportPresets.Video.Webm
{
    public class WebmPreset : VideoPreset
    {
        public WebmPreset()
        {
            Type = Util.Export.Webm;
            DefaultExtension = ".webm";
            Extension = ".webm";
        }
    }
}