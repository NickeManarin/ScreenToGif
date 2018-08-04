using System;
using System.IO;
using System.Text;

namespace ScreenToGif.Util
{
    internal static class StreamHelpers
    {
        #region Peek

        public static byte[] PeekBytes(this Stream ms, long position, int count)
        {
            var prevPosition = ms.Position;

            ms.Position = position;
            var buffer = ReadBytes(ms, count);
            ms.Position = prevPosition;

            return buffer;
        }

        public static char PeekChar(this Stream ms)
        {
            return PeekChar(ms, (int)ms.Position);
        }

        public static char PeekChar(this Stream ms, int position)
        {
            return BitConverter.ToChar(PeekBytes(ms, position, 2), 0);
        }

        public static short PeekInt16(this Stream ms)
        {
            return PeekInt16(ms, (int)ms.Position);
        }

        public static short PeekInt16(this Stream ms, int position)
        {
            return BitConverter.ToInt16(PeekBytes(ms, position, 2), 0);
        }

        public static int PeekInt32(this Stream ms)
        {
            return PeekInt32(ms, (int)ms.Position);
        }

        public static int PeekInt32(this Stream ms, int position)
        {
            return BitConverter.ToInt32(PeekBytes(ms, position, 4), 0);
        }

        public static long PeekInt64(this Stream ms)
        {
            return PeekInt64(ms, (int)ms.Position);
        }

        public static long PeekInt64(this Stream ms, int position)
        {
            return BitConverter.ToInt64(PeekBytes(ms, position, 8), 0);
        }

        public static ushort PeekUInt16(this Stream ms)
        {
            return PeekUInt16(ms, (int)ms.Position);
        }

        public static ushort PeekUInt16(this Stream ms, int position)
        {
            return BitConverter.ToUInt16(PeekBytes(ms, position, 2), 0);
        }

        public static uint PeekUInt32(this Stream ms)
        {
            return PeekUInt32(ms, (int)ms.Position);
        }

        public static uint PeekUInt32(this Stream ms, int position)
        {
            return BitConverter.ToUInt32(PeekBytes(ms, position, 4), 0);
        }

        public static ulong PeekUInt64(this Stream ms)
        {
            return PeekUInt64(ms, (int)ms.Position);
        }

        public static ulong PeekUInt64(this Stream ms, int position)
        {
            return BitConverter.ToUInt64(PeekBytes(ms, position, 8), 0);
        }

        #endregion Peek

        #region Read

        public static byte[] ReadBytes(this Stream ms, int count)
        {
            var buffer = new byte[count];

            if (ms.Read(buffer, 0, count) != count)
                throw new Exception("End reached.");

            return buffer;
        }

        public static byte[] ReadBytes(this Stream ms, uint count)
        {
            var buffer = new byte[count];

            if (ms.Read(buffer, 0, (int)count) != count)
                throw new Exception("End reached.");

            return buffer;
        }

        public static char ReadChar(this Stream ms)
        {
            return BitConverter.ToChar(ReadBytes(ms, 2), 0);
        }

        public static short ReadInt16(this Stream ms)
        {
            return BitConverter.ToInt16(ReadBytes(ms, 2), 0);
        }

        public static int ReadInt32(this Stream ms)
        {
            return BitConverter.ToInt32(ReadBytes(ms, 4), 0);
        }

        public static long ReadInt64(this Stream ms)
        {
            return BitConverter.ToInt64(ReadBytes(ms, 8), 0);
        }

        public static ushort ReadUInt16(this Stream ms)
        {
            return BitConverter.ToUInt16(ReadBytes(ms, 2), 0);
        }

        public static uint ReadUInt32(this Stream ms)
        {
            return BitConverter.ToUInt32(ReadBytes(ms, 4), 0);
        }

        public static ulong ReadUInt64(this Stream ms)
        {
            return BitConverter.ToUInt64(ReadBytes(ms, 8), 0);
        }

        #endregion Read

        #region Write

        public static void WriteByte(this Stream ms, int position, byte value)
        {
            var prevPosition = ms.Position;

            ms.Position = position;
            ms.WriteByte(value);
            ms.Position = prevPosition;
        }

        public static void WriteBytes(this Stream ms, byte[] value)
        {
            ms.Write(value, 0, value.Length);
        }

        public static void WriteBytes(this Stream ms, int position, byte[] value)
        {
            var prevPosition = ms.Position;

            ms.Position = position;
            ms.Write(value, 0, value.Length);
            ms.Position = prevPosition;
        }

        /// <summary>
        /// Writes one stream into another.
        /// </summary>
        /// <param name="ms">The stream that will receive the other stream.</param>
        /// <param name="stream">The stream to be copied.</param>
        internal static void WriteStream(this Stream ms, Stream stream)
        {
            stream.Position = 0;

            stream.CopyTo(ms, (int)Math.Min(4096, stream.Length));
        }

        public static void WriteStringUtf8(this Stream ms, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            ms.Write(bytes, 0, bytes.Length);
        }

        public static void WriteString1252(this Stream ms, string value)
        {
            var bytes = Encoding.GetEncoding(1252).GetBytes(value);
            ms.Write(bytes, 0, bytes.Length);
        }

        public static void WriteInt16(this Stream ms, short value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 2);
        }

        public static void WriteInt16(this Stream ms, int position, short value)
        {
            WriteBytes(ms, position, BitConverter.GetBytes(value));
        }

        public static void WriteInt32(this Stream ms, int value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public static void WriteInt32(this Stream ms, int position, int value)
        {
            WriteBytes(ms, position, BitConverter.GetBytes(value));
        }

        public static void WriteInt64(this Stream ms, long value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 8);
        }

        public static void WriteInt64(this Stream ms, int position, long value)
        {
            WriteBytes(ms, position, BitConverter.GetBytes(value));
        }

        public static void WriteUInt16(this Stream ms, ushort value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 2);
        }

        public static void WriteUInt16(this Stream ms, int position, ushort value)
        {
            WriteBytes(ms, position, BitConverter.GetBytes(value));
        }

        public static void WriteUInt32(this Stream ms, uint value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public static void WriteUInt32(this Stream ms, int position, uint value)
        {
            WriteBytes(ms, position, BitConverter.GetBytes(value));
        }

        public static void WriteUInt64(this Stream ms, ulong value)
        {
            ms.Write(BitConverter.GetBytes(value), 0, 8);
        }

        public static void WriteUInt64(this Stream ms, int position, ulong value)
        {
            WriteBytes(ms, position, BitConverter.GetBytes(value));
        }

        #endregion
    }
}