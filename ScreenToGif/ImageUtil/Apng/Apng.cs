using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Apng
{
    internal class Apng : IDisposable
    {
        #region Properties

        /// <summary>
        /// The stream which the apgn is writen on.
        /// </summary>
        private Stream InternalStream { get; set; }

        /// <summary>
        /// The total number of frames.
        /// </summary>
        internal int FrameCount { get; set; } = 0;

        /// <summary>
        /// Repeat Count for the apng.
        /// </summary>
        internal int RepeatCount { get; set; } = 0;

        /// <summary>
        /// True if it's the first frame of the apgn.
        /// </summary>
        private bool IsFirstFrame { get; set; } = true;

        /// <summary>
        /// The sequence number of frame.
        /// </summary>
        private int SequenceNumber { get; set; } = 0;

        private enum DisposeOps
        {
            None = 0,
            Background = 1,
            Previous = 2
        }

        private enum BlendOps
        {
            Source = 0,
            Over = 1
        }
        
        #endregion

        internal Apng(Stream stream, int frameCount = 0, int repeatCount = 0)
        {
            InternalStream = stream;
            FrameCount = frameCount;
            RepeatCount = repeatCount;
        }

        internal void AddFrame(string path, Int32Rect rect, int delay = 66)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (IsFirstFrame)
                {
                    //Png Header: 8 bytes.
                    InternalStream.WriteBytes(stream.ReadBytes(8));

                    //IHDR chunk. 13 bytes (Length + Type + CRC, 4 bytes each) = 25 bytes.
                    InternalStream.WriteBytes(stream.ReadBytes(25));

                    //acTL: Animation control chunk. 8 bytes (Length + Type + CRC, 4 bytes each) = 20 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian(8u)); //Length, 4 bytes.
                    InternalStream.WriteBytes(Encoding.ASCII.GetBytes("acTL")); //Chunk type, 4 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)FrameCount)); //NumFrames, 4 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)RepeatCount)); //NumPlays, 4 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(InternalStream.PeekBytes(InternalStream.Position - 12, 12)))); //CRC, 4 bytes.
                }

                //fcTL: Frame control chunk. 26 bytes (Length + Type + CRC, 4 bytes each) = 38 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian(26u)); //Length, 4 bytes.
                InternalStream.WriteBytes(Encoding.ASCII.GetBytes("fcTL")); //Chunk type, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)SequenceNumber++)); //SequenceNumber, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)rect.Width)); //Width, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)rect.Height)); //Height, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)rect.X)); //OffsetX, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)rect.Y)); //OffsetY, 4 bytes.
                InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)delay)); //Delay numerator, 2 bytes.
                InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)1000)); //Delay denominator, 2 bytes.

                if (IsFirstFrame)
                {
                    InternalStream.WriteByte((byte)DisposeOps.None); //DisposeOp, 1 byte.
                    InternalStream.WriteByte((byte)BlendOps.Source); //BlendOp, 1 byte.
                }
                else
                {
                    InternalStream.WriteByte((byte)DisposeOps.None); //DisposeOp, 1 byte.
                    InternalStream.WriteByte((byte)BlendOps.Over); //BlendOp, 1 byte.
                }

                InternalStream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(InternalStream.PeekBytes(InternalStream.Position - 30, 30)))); //CRC, 4 bytes.

                //fdAT: Frame data chunk. 4 + n bytes (Length + Type + CRC, 4 bytes each) = 16 + n bytes, where n is the frame data.
                var dataList = GetData(stream);

                foreach (var data in dataList)
                {
                    if (IsFirstFrame)
                    {
                        InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)data.Length)); //Length, 4 bytes.
                        InternalStream.WriteBytes(Encoding.ASCII.GetBytes("IDAT")); //Chunk type, 4 bytes.
                        InternalStream.WriteBytes(data); //Frame data, n bytes.
                        InternalStream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(InternalStream.PeekBytes(InternalStream.Position - (data.Length + 4), data.Length + 4)))); //CRC, 4 bytes.
                    }
                    else
                    {
                        InternalStream.WriteUInt32(BitHelper.ConvertEndian(4 + (uint)data.Length)); //Length, 4 bytes.
                        InternalStream.WriteBytes(Encoding.ASCII.GetBytes("fdAT")); //Chunk type, 4 bytes.
                        InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)SequenceNumber++)); //SequenceNumber, 4 bytes.
                        InternalStream.WriteBytes(data); //Frame data, n bytes.
                        InternalStream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(InternalStream.PeekBytes(InternalStream.Position - (data.Length + 8), data.Length + 8)))); //CRC, 4 bytes.
                    }
                }
                
                IsFirstFrame = false;
            }
        }

        private static IEnumerable<byte[]> GetData(Stream ms)
        {
            ms.Position = 8 + 25;

            var list = new List<byte[]>();

            while (ms.CanRead)
            {
                var length = BitHelper.ConvertEndian(ms.ReadUInt32());
                var chunkType = Encoding.ASCII.GetString(ms.ReadBytes(4));
                var data = ms.ReadBytes(length);

                if (chunkType == "IDAT")
                    list.Add(data);

                if (chunkType == "IEND")
                    break;

                ms.ReadUInt32();
            }

            return list;
        }

        public void Dispose()
        {
            //IEND: The end of the Png datastream. 0 bytes (Length + Type + CRC, 4 bytes each) = 12 bytes.
            InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //Length, 4 bytes.
            InternalStream.WriteBytes(Encoding.ASCII.GetBytes("IEND")); //Chunk type, 4 bytes.
            InternalStream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(InternalStream.PeekBytes(InternalStream.Position - 4, 4)))); //CRC, 4 bytes.

            InternalStream.Flush();
            //Resets the stream position to save afterwards.
            InternalStream.Position = 0;
        }
    }
}