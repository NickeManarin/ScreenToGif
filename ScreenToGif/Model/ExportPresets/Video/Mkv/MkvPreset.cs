namespace ScreenToGif.Model.ExportPresets.Video.Mkv
{
    public class MkvPreset : VideoPreset
    {
        public MkvPreset()
        {
            Type = Util.Export.Mkv;
            DefaultExtension = ".mkv";
            Extension = ".mkv";
        }
    }
}