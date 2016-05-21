using System;
using System.IO;
using System.Text;

namespace ScreenToGif.ImageUtil.Decoder
{
    public static class GifHelpers
    {
        public static string ReadString(Stream stream, int length)
        {
            byte[] bytes = new byte[length];
            stream.ReadAll(bytes, 0, length);
            return Encoding.ASCII.GetString(bytes);
        }

        public static byte[] ReadDataBlocks(Stream stream, bool discard)
        {
            MemoryStream ms = discard ? null : new MemoryStream();

            using (ms)
            {
                int len;

                while ((len = stream.ReadByte()) > 0)
                {
                    byte[] bytes = new byte[len];
                    stream.ReadAll(bytes, 0, len);

                    if (ms != null)
                        ms.Write(bytes, 0, len);
                }

                if (ms != null)
                    return ms.ToArray();

                return null;
            }
        }

        public static GifColor[] ReadColorTable(Stream stream, int size)
        {
            int length = 3 * size;
            byte[] bytes = new byte[length];
            stream.ReadAll(bytes, 0, length);
            GifColor[] colorTable = new GifColor[size];

            for (int i = 0; i < size; i++)
            {
                byte r = bytes[3 * i];
                byte g = bytes[3 * i + 1];
                byte b = bytes[3 * i + 2];
                colorTable[i] = new GifColor(r, g, b);
            }

            return colorTable;
        }

        public static bool IsNetscapeExtension(GifApplicationExtension ext)
        {
            return ext.ApplicationIdentifier == "NETSCAPE"
                && Encoding.ASCII.GetString(ext.AuthenticationCode) == "2.0";
        }

        public static ushort GetRepeatCount(GifApplicationExtension ext)
        {
            if (ext.Data.Length >= 3)
            {
                return BitConverter.ToUInt16(ext.Data, 1);
            }
            return 1;
        }

        public static Exception UnexpectedEndOfStreamException()
        {
            return new GifDecoderException("Unexpected end of stream before trailer was encountered");
        }

        public static Exception UnknownBlockTypeException(int blockId)
        {
            return new GifDecoderException("Unknown block type: 0x" + blockId.ToString("x2"));
        }

        public static Exception UnknownExtensionTypeException(int extensionLabel)
        {
            return new GifDecoderException("Unknown extension type: 0x" + extensionLabel.ToString("x2"));
        }

        public static Exception InvalidBlockSizeException(string blockName, int expectedBlockSize, int actualBlockSize)
        {
            return new GifDecoderException(string.Format("Invalid block size for {0}. Expected {1}, but was {2}",
                    blockName, expectedBlockSize, actualBlockSize));
        }

        public static Exception InvalidSignatureException(string signature)
        {
            return new GifDecoderException("Invalid file signature: " + signature);
        }

        public static Exception UnsupportedVersionException(string version)
        {
            return new GifDecoderException("Unsupported version: " + version);
        }

        public static void ReadAll(this Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;

            while (totalRead < count)
            {
                totalRead += stream.Read(buffer, offset + totalRead, count - totalRead);
            }
        }
    }
}
