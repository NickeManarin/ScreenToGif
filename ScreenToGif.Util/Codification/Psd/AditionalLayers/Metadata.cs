using System.IO;
using System.Text;

namespace ScreenToGif.Util.Codification.Psd.AdditionalLayers;

internal class Metadata : IAdditionalLayerInfo
{
    public string Key { get; } = "shmd";

    public List<MetadataContent> MetadataContentList = new();

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteBytes(Encoding.ASCII.GetBytes(Key)); //Additional layer info key, 4 bytes.

                foreach (var metadata in MetadataContentList)
                    stream.WriteBytes(metadata.Content);

                return stream.ToArray();
            }
        }
    }
}