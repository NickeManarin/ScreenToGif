using System.Net.Mail;

namespace ScreenToGif.Encoding
{
    public static class Constants
    {
        public static readonly char[] DelimiterChars = new char[] { '\\', '/' };

        public const int BufferSize = 0x2000;

        public const int LargeBufferSize = BufferSize * 1024;
    }
}
