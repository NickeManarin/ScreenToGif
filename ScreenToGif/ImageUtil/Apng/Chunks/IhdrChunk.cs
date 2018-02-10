namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    internal class IhdrChunk
    {
        internal int Width { get; private set; }

        internal int Height { get; private set; }

        internal byte BitDepth { get; private set; }

        internal byte ColorType { get; private set; }

        internal byte CompressionMethod { get; private set; }

        internal byte FilterMethod { get; private set; }

        internal byte InterlaceMethod { get; private set; }
    }
}