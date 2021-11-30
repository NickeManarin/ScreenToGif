using System.IO;

namespace ScreenToGif.Util.Codification.Psd.ImageResourceBlocks;

internal class ImageResources : IPsdContent
{
    internal List<IImageResource> ImageResourceList = new();

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                //If there's no ImageResource block, return a size of 0 bytes.
                if (ImageResourceList.Count == 0)
                {
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)0));
                    return stream.ToArray();
                }

                var bytes = ImageResourceList.SelectMany(s => s.Content).ToArray();

                stream.WriteUInt32(BitHelper.ConvertEndian((uint)bytes.Length));
                stream.WriteBytes(bytes);

                return stream.ToArray();
            }
        }
    }
}