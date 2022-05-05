using System.IO;
using System.Text;

namespace ScreenToGif.Util.Codification.Psd.AdditionalLayers;

internal class MetadataContent : IAdditionalLayerInfo
{
    internal string Signature { get; }

    public string Key { get; }

    internal bool CopyOnSheetDuplication { get; } = true;

    internal string Data { get; }

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteBytes(Encoding.ASCII.GetBytes(Signature)); //Signature, 4 bytes.
                stream.WriteBytes(Encoding.ASCII.GetBytes(Key)); //Key, 4 bytes.
                stream.WriteByte((byte)(CopyOnSheetDuplication ? 1: 0)); //Copy on sheet duplication, 1 byte.
                stream.Position += 3; //Padding 3 bytes.

                var bytes = Encoding.UTF8.GetBytes(Data);

                stream.WriteUInt32((uint)BitHelper.ConvertEndian(bytes.Length)); //Data length, 4 bytes.
                stream.WriteBytes(bytes);
                    
                return stream.ToArray();
            }
        }
    }
}