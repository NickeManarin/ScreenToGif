using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.ImageUtil.TestEncoder
{
    internal class LogicalScreenDescriptor
    {
        internal short Width { get; set; }

        internal short Height { get; set; }

        internal byte BackgroundIndex { get; set; }

        internal bool HasGlobalColorTable { get; set; }

        internal byte ColorResolution { get; set; }

        internal bool IsSorted { get; set; }

        internal int GlobalColorTableSize { get; set; }

        internal byte[] ToArray()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(Width));
            list.AddRange(BitConverter.GetBytes(Height));

            //Packed fields, 1 byte
            var bitArray = new BitArray(8);
            bitArray.Set(0, HasGlobalColorTable);

            //Color resolution: 111 = (8 bits - 1)
            //Color depth - 1
            //Global colors count = 2^color depth
            var pixelBits = GifMethods.ToBitValues(GlobalColorTableSize);

            bitArray.Set(1, pixelBits[0]);
            bitArray.Set(2, pixelBits[1]);
            bitArray.Set(3, pixelBits[2]);

            //Sort flag (for the global color table): 0
            bitArray.Set(4, IsSorted);

            //Size of the Global Color Table (Zero, if not used.): 
            var sizeInBits = GifMethods.ToBitValues(GlobalColorTableSize);

            bitArray.Set(5, sizeInBits[0]);
            bitArray.Set(6, sizeInBits[1]);
            bitArray.Set(7, sizeInBits[2]);

            list.Add(GifMethods.ConvertToByte(bitArray));
            list.Add(BackgroundIndex); //Background color index, 1 byte
            list.Add(0); //Pixel aspect ratio - Assume 1:1, 1 byte

            return list.ToArray();
        }
    }
}
