using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.ImageUtil.TestEncoder
{
    internal static class GifMethods
    {
        internal static bool[] ToBitValues(int number)
        {
            return new BitArray(new[] { number }).Cast<bool>().Take(3).Reverse().ToArray();
        }

        internal static byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }

            byte[] bytes = new byte[1];
            var reversed = new BitArray(bits.Cast<bool>().Reverse().ToArray());
            reversed.CopyTo(bytes, 0);
            return bytes[0];
        }
    }
}
