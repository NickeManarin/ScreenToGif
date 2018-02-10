namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    internal class Chunk
    {
        #region Properties

        public uint Length { get; set; }

        public string ChunkType { get; set; }

        public byte[] ChunkData { get; set; }

        public uint Crc { get; set; }

        #endregion


    }
}