using System.IO;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Psd
{
    internal class LayerAndMask : IPsdContent
    {
        internal LayerInfo LayerInfo = new LayerInfo();

        public long Length => Content?.Length ?? 0;

        public byte[] Content
        {
            get
            {
                using (var stream = new MemoryStream())
                {
                    var layerInfo = LayerInfo.Content;
                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)layerInfo.Length)); //Length of the LayerInfo block, 4 bytes.
                    stream.WriteBytes(layerInfo); //Layer info block, XX bytes.

                    stream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //Length of the Mask block, 4 bytes.

                    //Aditional LayerInfo goes here. TODO

                    stream.WritePadding(2); //Pad to multiple of 2 bytes.

                    return stream.ToArray();
                }
            }
        }
    }
}