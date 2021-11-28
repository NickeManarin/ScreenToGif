using System.IO;

namespace ScreenToGif.Util.Codification.Psd;

internal class Channel : IPsdContent
{
    public byte[] RawContent { get; }

    public byte[][] RleCompressedContent { get; }

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            if (RleCompressedContent == null)
                return RawContent;

            using (var stream = new MemoryStream())
            {
                //Writes all byte counts for all the scan lines (rows * channels), with each count stored as a two-byte value.
                foreach (var b in RleCompressedContent)
                    stream.WriteUInt16(BitHelper.ConvertEndian((ushort)b.Length));

                //Writes down each layer, in planar order: AAA RRR GGG BBB.
                foreach (var b in RleCompressedContent)
                    stream.WriteBytes(b);

                return stream.ToArray();
            }
        }
    }

    public Channel(byte[] raw)
    {
        RawContent = raw;
    }

    public Channel(byte[][] content)
    {
        RleCompressedContent = content;
    }
}