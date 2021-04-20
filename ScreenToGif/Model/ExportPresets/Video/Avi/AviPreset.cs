namespace ScreenToGif.Model.ExportPresets.Video.Avi
{
    public class AviPreset : VideoPreset
    {
        public AviPreset()
        {
            Type = Util.Export.Avi;
            DefaultExtension = ".avi";
            Extension = ".avi";
            IsAncientContainer = true;
        }
    }
}