using System.Collections.Generic;
using System.IO;
using System.Text;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Psd.AditionalLayers
{
    internal class Metadata : IAditionalLayerInfo
    {
        public string Key { get; } = "shmd";

        public List<MetadataContent> MetadataContentList = new List<MetadataContent>();

        public long Length => Content?.Length ?? 0;

        public byte[] Content
        {
            get
            {
                using (var stream = new MemoryStream())
                {
                    stream.WriteBytes(Encoding.ASCII.GetBytes(Key)); //Aditional layer info key, 4 bytes.

                    foreach (var metadata in MetadataContentList)
                        stream.WriteBytes(metadata.Content);

                    return stream.ToArray();
                }
            }
        }
    }
}