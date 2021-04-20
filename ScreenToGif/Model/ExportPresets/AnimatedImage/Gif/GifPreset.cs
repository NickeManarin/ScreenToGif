namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Gif
{
    public class GifPreset : AnimatedImagePreset
    {
        private bool _useGlobalColorTable;


        public bool UseGlobalColorTable
        {
            get => _useGlobalColorTable;
            set => SetProperty(ref _useGlobalColorTable, value);
        }


        public GifPreset()
        {
            Type = Util.Export.Gif;
            DefaultExtension = ".gif";
            Extension = ".gif";
        }
    }
}