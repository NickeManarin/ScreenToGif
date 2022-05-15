using ScreenToGif.Util.Codification.Psd.AdditionalLayers;
using System.IO;
using System.Text;

namespace ScreenToGif.Util.Codification.Psd;

internal class LayerRecord : IPsdContent
{
    public uint Top { get; set; }

    public uint Left { get; set; }

    public uint Bottom { get; set; }
        
    public uint Right { get; set; }

    public Dictionary<short, int> Channels { get; set; } = new();

    public string Name { get; set; }

    public List<IAdditionalLayerInfo> AdditionalInfo { get; set; } = new();

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteUInt32(BitHelper.ConvertEndian(Top)); //Top point, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian(Left)); //Left point, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian(Bottom)); //Bottom point, 4 bytes.
                stream.WriteUInt32(BitHelper.ConvertEndian(Right)); //Right point, 4 bytes.

                stream.WriteUInt16(BitHelper.ConvertEndian((ushort)Channels.Count)); //Number of channels on this layer, 2 bytes.

                foreach (var channel in Channels)
                {
                    stream.WriteInt16(BitHelper.ConvertEndian(channel.Key)); //Channel ID, 2 bytes.
                    stream.WriteInt32(BitHelper.ConvertEndian(channel.Value)); //Channel length, 4 bytes.
                }

                stream.WriteBytes(Encoding.ASCII.GetBytes("8BIM")); //Blend mode signature, 4 bytes.
                //stream.WriteInt32(BitHelper.ConvertEndian(0x6e6f726d)); Same as this one below.
                stream.WriteBytes(Encoding.ASCII.GetBytes("norm"));//Blend mode value, Normal, 4 bytes.
                stream.WriteByte(255); //Opacity, 1 byte.
                stream.WriteByte(0); //Clipping, 1 byte.
                stream.WriteByte(0); //Flags, Visible = true, 1 byte. (For invisible, try using 10)
                stream.WriteByte(0); //Filler, 1 byte

                var name = StreamHelpers.GetPascalStringAsBytes(Encoding.Unicode.GetBytes(Name));
                var additionalLayerInfo = AdditionalInfo.SelectMany(s => s.Content).ToArray();

                stream.WriteUInt32(BitHelper.ConvertEndian((uint)(4 + 4 + name.Length + additionalLayerInfo.Length))); //Extra data length, 4 bytes.
                stream.WriteInt32(BitHelper.ConvertEndian(0)); //Layer mask size, 4 bytes.
                stream.WriteInt32(BitHelper.ConvertEndian(0)); //Blending ranges size, 4 bytes.

                stream.WriteBytes(name); //Layer name, pascal string as bytes.
                stream.WriteBytes(additionalLayerInfo); //List of additional layer info, XX bytes.

                //Padding.

                return stream.ToArray();
            }
        }
    }
}