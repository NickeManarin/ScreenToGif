using System.IO;

namespace ScreenToGif.Util.Codification.Psd;

internal class LayerInfo : IPsdContent
{
    internal List<LayerRecord> LayerList = new();
    internal List<ImageChannelData> ImageChannelDataList = new();

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                var layers = LayerList.SelectMany(s => s.Content).ToArray();
                //var channels = ImageChannelDataList.SelectMany(s => s.Content).ToArray();

                stream.WriteInt16(BitHelper.ConvertEndian((short)(LayerList.Count))); //Number of channels of this layer, 2 bytes. Negative if absolute alpha.

                stream.WriteBytes(layers); //Layer records, XX bytes. 
                //stream.WriteBytes(channels); //List of channel data, XX bytes. 

                //List of channel data, XX bytes. Use this to avoid airthmetic overflows.
                foreach (var channel in ImageChannelDataList)
                    stream.WriteBytes(channel.Content);

                stream.WritePadding(4); //Pad to multiple of 4 bytes.
                    
                return stream.ToArray();
            }
        }
    }
}