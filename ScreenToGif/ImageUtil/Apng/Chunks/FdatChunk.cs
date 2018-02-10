namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    internal class FdatChunk
    {
        internal uint SequenceNumber { get; private set; }

        internal byte[] FrameData { get; private set; }
    }
}