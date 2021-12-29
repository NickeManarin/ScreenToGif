namespace ScreenToGif.Util.Codification.Gif.Encoder;

internal class BitEncoder
{
    /// <summary>
    /// The last remaining bit
    /// </summary>
    private int _currentBit = 0;

    /// <summary>
    /// Output byte of data collection.
    /// </summary>
    internal List<byte> OutList = new();

    /// <summary>
    /// Current length of the output.
    /// </summary>
    internal int Length => OutList.Count;

    internal int InBit { get; set; }

    private int _currentVal;

    internal BitEncoder(int initBit = 8)
    {
        InBit = initBit;
    }

    /// <summary> 
    /// Adds the code into 
    /// </summary> 
    /// <param name="inByte">The input data</param>
    internal void Add(int inByte)
    {
        //Debug.WriteLine(InBit + " : " + inByte);

        //Shifts the input value to the bit position (0 to 8).
        //Merges the current value with the shifted input value.
        //They will never colide, 00000100 | 00000101 = 00101100 (4 | 5 = 44)
        _currentVal |= (inByte << (_currentBit));

        _currentBit += InBit;

        //The output always use 8 bits, even if the codesize ranges from 3-12 bits.
        //So, it needs 3+3+3bits to output 1 byte (1 bit will be left to the next byte).
        while (_currentBit >= 8)
        {
            var outVal = (byte)(_currentVal & 0XFF);
            _currentVal = _currentVal >> 8; //"Eats" the first eight positions to the right.
            _currentBit -= 8;

            OutList.Add(outVal);
        }
    }

    internal void End()
    {
        //Should output the value even if does not fill 8 bits.
        while (_currentBit > 0)
        {
            var outVal = (byte)(_currentVal & 0XFF);
            _currentVal = _currentVal >> 8;
            _currentBit -= 8;

            OutList.Add(outVal);
        }
    }
}