using System.Runtime.InteropServices;

namespace ScreenToGif.Util;

/// <summary>
/// Word (short) level helper.
/// Divide integer into two shorts and get/set the higher and lower values.
/// </summary>
public class WordLevel
{
    /// <summary>
    /// Each field size is 8 bits.
    /// So, shifting an offset of 2, will jump 16 bits.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WordUnion
    {
        [FieldOffset(0)]
        public uint Number;

        [FieldOffset(0)]
        public short Low;

        [FieldOffset(2)]
        public short High;
    }

    public static short GetLower(uint number)
    {
        //BitConverter.ToInt16(BitConverter.GetBytes(number), 0);
        return new WordUnion { Number = number }.Low;
    }

    public static short GetHigher(uint number)
    {
        //BitConverter.ToInt16(BitConverter.GetBytes(number), 2);
        return new WordUnion { Number = number }.High;
    }

    public static uint SetLower(uint number, short low)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var union = new WordUnion { Number = number };
        union.Low = low;

        return union.Number;
    }

    public static uint SetHigher(uint number, short high)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var union = new WordUnion { Number = number };
        union.High = short.MaxValue;

        return union.Number;
    }
}