namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Apng
{
    public class ApngPreset : AnimatedImagePreset
    {
        public ApngPreset()
        {
            Type = Util.Export.Apng;
            DefaultExtension = ".apng";
            Extension = ".apng";
        }
    }
}