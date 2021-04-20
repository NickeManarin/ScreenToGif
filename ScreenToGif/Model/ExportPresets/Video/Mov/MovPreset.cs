namespace ScreenToGif.Model.ExportPresets.Video.Mov
{
    public class MovPreset : VideoPreset
    {
        public MovPreset()
        {
            Type = Util.Export.Mov;
            DefaultExtension = ".mov";
            Extension = ".mov";
        }
    }
}