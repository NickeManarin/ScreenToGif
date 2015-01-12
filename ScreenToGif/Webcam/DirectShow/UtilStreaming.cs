using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Webcam.DirectShow
{
    public class UtilStreaming
    {
        [StructLayout(LayoutKind.Sequential), ComVisible(false)]
        public class DsOptInt64
        {
            public DsOptInt64(long Value)
            {
                this.Value = Value;
            }

            public long Value;
        }

        [StructLayout(LayoutKind.Sequential), ComVisible(false)]
        public struct DsRECT		// RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        // ---------------------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential, Pack = 2), ComVisible(false)]
        public struct BitmapInfoHeader
        {
            public int Size;
            public int Width;
            public int Height;
            public short Planes;
            public short BitCount;
            public int Compression;
            public int ImageSize;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public int ClrUsed;
            public int ClrImportant;
        }

    }
}
