using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.ImageUtil.TestEncoder
{
    /// <summary>
    /// Graphic Control Extension.
    /// </summary>
    internal class GraphicExtension : GifConstants
    {
        /// <summary>
        /// Block Size, 4 bytes.
        /// </summary>
        internal const byte BlockSize = 4;

        #region Properties

        /// <summary>
        /// True of the has a reserved color, marked as transparent.
        /// </summary>
        internal bool HasTransparency { get; set; }

        internal DisposalMethod DisposalMethod { get; set; }

        /// <summary>
        /// Delay of the frame, as 1/100 of a second. Centisecond.
        /// 10 ms = 1 cs, 66 ms = 6 cs.
        /// </summary>
        internal short Delay { get; set; }

        internal byte TransparentIndex { get; set; }

        #endregion

        internal byte[] ToArray()
        {
            var list = new List<byte>();
            list.Add(ExtensionIntroducer);
            list.Add(GraphicControlLabel);
            list.Add(BlockSize);

            //Packed fields
            var bitArray = new BitArray(8);

            //Reserved for future use. Hahahaha. Yeah...
            bitArray.Set(0, false);
            bitArray.Set(1, false);
            bitArray.Set(2, false);

            var pixelBits = GifMethods.ToBitValues((int)DisposalMethod);

            bitArray.Set(3, pixelBits[0]);
            bitArray.Set(4, pixelBits[1]);
            bitArray.Set(5, pixelBits[2]);

            //User Input Flag.
            bitArray.Set(6, false);

            //Transparent Color Flag, uses tranparency?
            bitArray.Set(7, HasTransparency);

            list.Add(GifMethods.ConvertToByte(bitArray));
            list.AddRange(BitConverter.GetBytes(Delay));
            list.Add(TransparentIndex);
            list.Add(Terminator);

            return list.ToArray();
        }
    }
}
