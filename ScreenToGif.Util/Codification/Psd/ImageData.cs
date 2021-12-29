using System.IO;

namespace ScreenToGif.Util.Codification.Psd;

internal class ImageData : IPsdContent
{
    /// <summary>
    /// 0 = Raw Data
    /// 1 = RLE compressed
    /// 2 = ZIP without prediction
    /// 3 = ZIP with prediction.
    /// </summary>
    internal ushort Encoding { get; set; } = 1;

    internal List<Image> ImageList { get; set; } = new();

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                foreach (var image in ImageList)
                {
                    stream.WriteUInt16(BitHelper.ConvertEndian(Encoding)); //Encoding type, 2 bytes.
                    stream.WriteBytes(image.Content); //Image data, XX bytes.                        
                }

                return stream.ToArray();
            }
        }
    }
}