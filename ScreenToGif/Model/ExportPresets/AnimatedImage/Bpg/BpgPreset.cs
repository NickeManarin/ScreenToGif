namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Bpg
{
    public class BpgPreset : AnimatedImagePreset
    {
        public BpgPreset()
        {
            Type = Util.Export.Bpg;
            DefaultExtension = ".bpg";
            Extension = ".bpg";
        }
    }
}