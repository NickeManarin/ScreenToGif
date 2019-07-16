namespace ScreenToGif.ModelEx.Layers
{
    internal class ObfuscationLayer : LayerModel
    {
        internal enum ObfuscationType
        {
            Pixelation,
            Blur
        }

        public ObfuscationLayer()
        {
            Type = LayerType.Obfuscation;
            RequiresPreviousPixels = true;
        }

        public ObfuscationType Mode { get; set; }
        public int PixelSize { get; set; }

        //TODO: Other properties.
    }
}