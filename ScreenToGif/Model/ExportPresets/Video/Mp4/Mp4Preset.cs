namespace ScreenToGif.Model.ExportPresets.Video.Mp4
{
    public class Mp4Preset : VideoPreset
    {
        public Mp4Preset()
        {
            Type = Util.Export.Mp4;
            DefaultExtension = ".mp4";
            Extension = ".mp4";
        }
    }
}