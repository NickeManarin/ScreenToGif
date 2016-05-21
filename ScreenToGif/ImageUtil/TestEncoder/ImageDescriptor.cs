using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.ImageUtil.TestEncoder
{
    class ImageDescriptor
    {
        internal short XOffSet { get; set; }

        internal short YOffSet { get; set; }

        internal short Width { get; set; }

        internal short Height { get; set; }

        internal bool HasLocalColorTable { get; set; }

        internal bool IsInterlaced { get; set; }

        internal bool IsSorted { get; set; }

        internal int LocalColorTableSize { get; set; }

        internal byte[] ToArray()
        {
            var list = new List<byte>();
            list.Add(GifConstants.ImageDescriptorLabel);
            list.AddRange(BitConverter.GetBytes(XOffSet));
            list.AddRange(BitConverter.GetBytes(YOffSet));
            list.AddRange(BitConverter.GetBytes(Width));
            list.AddRange(BitConverter.GetBytes(Height));

            //Packed fields.
            var bitArray = new BitArray(8);

            //Uses local color table?
            bitArray.Set(0, HasLocalColorTable);

            //Interlace Flag.
            bitArray.Set(1, IsInterlaced);

            //Sort Flag.
            bitArray.Set(2, IsSorted);

            //Reserved for future use. Hahahah again.
            bitArray.Set(3, false);
            bitArray.Set(4, false);

            //Size of Local Color Table.
            var sizeInBits = GifMethods.ToBitValues(LocalColorTableSize);

            bitArray.Set(5, sizeInBits[0]);
            bitArray.Set(6, sizeInBits[1]);
            bitArray.Set(7, sizeInBits[2]);

            //Write the packed fields.
            list.Add(GifMethods.ConvertToByte(bitArray));

            return list.ToArray();
        }
    }
}
