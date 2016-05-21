using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.ImageUtil.TestEncoder
{
    internal class ApplicationExtension
    {
        /// <summary>
        /// Block Size, 11 bytes.
        /// </summary>
        internal const byte BlockSize = 0x0B;

        internal short RepeatCount { get; set; } = 0;

        internal byte[] ToArray()
        {
            var list = new List<byte>();
            list.Add(GifConstants.ExtensionIntroducer);
            list.Add(GifConstants.ApplicationExtensionLabel);
            list.Add(BlockSize);

            list.AddRange("NETSCAPE2.0".ToArray().Select(letter => (byte) letter));

            list.Add(0x03); // Application block length
            list.Add(0x01); //Loop sub-block ID. 1 byte
            list.AddRange(BitConverter.GetBytes(RepeatCount)); // Repeat count. 2 bytes.
            list.Add(GifConstants.Terminator);

            return list.ToArray();
        }
    }
}
