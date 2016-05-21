using System;
using System.Collections.Generic;

namespace ScreenToGif.ImageUtil.Encoder
{

    internal class BitEncoder
    {
        /// <summary>
        /// The last remaining bit
        /// </summary>
        private int _currentBit = 0;

        /// <summary>
        /// Output byte of data collection.
        /// </summary>
        internal List<Byte> OutList = new List<byte>();

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
            _currentVal |= (inByte << (_currentBit));

            _currentBit += InBit;

            //The output always use 8 bits, even if the codesize ranges from 3-12 bits.
            //So, it needs 3+3+3bits to output 1 byte (1 bit will be left to the next byte).
            while (_currentBit >= 8)
            {
                var outVal = (byte)(_currentVal & 0XFF);
                _currentVal = _currentVal >> 8;
                _currentBit -= 8;

                OutList.Add(outVal);
            }
        }

        internal void End()
        {
            //Should output the value even if does not fill 8bits.
            while (_currentBit > 0)
            {
                var outVal = (byte)(_currentVal & 0XFF);
                _currentVal = _currentVal >> 8;
                _currentBit -= 8;

                OutList.Add(outVal);
            }
        }
    }
}
