#region Java Disclaimer
//  Adapted from Jef Poskanzer's Java port by way of J. M. G. Elliott.
//  K Weiner 12/00
#endregion

#region C Disclaimer

// GIFCOMPR.C       - GIF Image compression routines
//
// Lempel-Ziv compression based on 'compress'.  GIF modifications by
// David Rowley (mgardi@watdcsu.waterloo.edu)

// GIF Image compression - modified 'compress'
//
// Based on: compress.c - File compression ala IEEE Computer, June 1984.
//
// By Authors:  Spencer W. Thomas      (decvax!harpo!utah-cs!utah-gr!thomas)
//              Jim McKie              (decvax!mcvax!jim)
//              Steve Davies           (decvax!vax135!petsd!peora!srd)
//              Ken Turkowski          (decvax!decwrl!turtlevax!ken)
//              James A. Woods         (decvax!ihnp4!ames!jaw)
//              Joe Orost              (decvax!vax135!petsd!joe)

#endregion

using System.IO;

namespace ScreenToGif.Util.Codification.Gif.LegacyEncoder;

/// <summary>
/// Image compression routines.
/// </summary>
public class LzwEncoder
{
    #region Variables

    /// <summary>
    /// End of File.
    /// </summary>
    private const int Eof = -1;

    private readonly int _imgW;
    private readonly int _imgH;
    private readonly byte[] _pixAry;
    private readonly int _initCodeSize;
    //private int _remaining;
    private int _curPixel;

    private const int Bits = 12;

    /// <summary>
    /// 80% occupancy.
    /// </summary>
    private const int HSize = 5003;

    /// <summary>
    /// Number of bits/code.
    /// </summary>
    int _numBits;

    /// <summary>
    /// User settable max # bits/code.
    /// </summary>
    readonly int _maxBits = Bits;

    /// <summary>
    /// Maximum code, given n_bits.
    /// </summary>
    int _maxCode;

    /// <summary>
    /// Should NEVER generate this code
    /// </summary>
    private const int MaxMaxCode = 1 << Bits;

    int[] htab = new int[HSize];
    readonly int[] _codeTab = new int[HSize];

    /// <summary>
    /// For dynamic table sizing.
    /// </summary>
    private int _hSize = HSize;

    /// <summary>
    /// First unused entry
    /// </summary>
    int _freeEntry = 0;

    // block compression parameters -- after all codes are used up,
    // and compression rate changes, start over.
    bool clear_flg = false;

    // Algorithm:  use open addressing double hashing (no chaining) on the
    // prefix code / next character combination.  We do a variant of Knuth's
    // algorithm D (vol. 3, sec. 6.4) along with G. Knott's relatively-prime
    // secondary probe.  Here, the modular division first probe is gives way
    // to a faster exclusive-or manipulation.  Also do block compression with
    // an adaptive reset, whereby the code table is cleared when the compression
    // ratio decreases, but after the table fills.  The variable-length output
    // codes are re-sized at this point, and a special CLEAR code is generated
    // for the decompressor.  Late addition:  construct the table according to
    // file size for noticeable speed improvement on small files.  Please direct
    // questions about this implementation to ames!jaw.

    int g_init_bits;

    int ClearCode;
    int EOFCode;

    // output
    //
    // Output the given code.
    // Inputs:
    //      code:   A n_bits-bit integer.  If == -1, then EOF.  This assumes
    //              that n_bits =< wordsize - 1.
    // Outputs:
    //      Outputs code to the file.
    // Assumptions:
    //      Chars are 8 bits long.
    // Algorithm:
    //      Maintain a BITS character long buffer (so that 8 codes will
    // fit in it exactly).  Use the VAX insv instruction to insert each
    // code in turn.  When the buffer fills up empty it and start over.

    int cur_accum = 0;
    int cur_bits = 0;

    int[] masks =
    {
        0x0000,
        0x0001,
        0x0003,
        0x0007,
        0x000F,
        0x001F,
        0x003F,
        0x007F,
        0x00FF,
        0x01FF,
        0x03FF,
        0x07FF,
        0x0FFF,
        0x1FFF,
        0x3FFF,
        0x7FFF,
        0xFFFF };

    /// <summary>
    /// Number of characters so far in this 'packet'.
    /// </summary>
    int _charCount;

    /// <summary>
    /// Define the storage for the packet accumulator.
    /// </summary>
    readonly byte[] _accumulator = new byte[256];

    #endregion

    /// <summary>
    /// Constructor of the compression class.
    /// </summary>
    /// <param name="width">The image Width</param>
    /// <param name="height">The image Height</param>
    /// <param name="pixels">All the pixels</param>
    /// <param name="colorDepth">The Color depth of the image</param>
    public LzwEncoder(int width, int height, byte[] pixels, int colorDepth)
    {
        //_imgW = width;
        //_imgH = height;
        _pixAry = pixels;
        _initCodeSize = Math.Max(2, colorDepth);
    }

    /// <summary>
    /// Add a character to the end of the current packet, and if it is 254 characters, flush the packet to disk.
    /// </summary>
    /// <param name="c"></param>
    /// <param name="outs"></param>
    private void Add(byte c, Stream outs)
    {
        _accumulator[_charCount++] = c;

        if (_charCount >= 254)
            Flush(outs);
    }

    /// <summary>
    /// Clear out the hash table for block compress.
    /// </summary>
    /// <param name="outs"></param>
    private void ClearTable(Stream outs)
    {
        ResetCodeTable(_hSize);
        _freeEntry = ClearCode + 2;
        clear_flg = true;

        Output(ClearCode, outs);
    }

    /// <summary>
    /// Reset code table.
    /// </summary>
    /// <param name="hsize"></param>
    private void ResetCodeTable(int hsize)
    {
        for (int i = 0; i < hsize; ++i)
            htab[i] = -1;
    }

    private void Compress(int initBits, Stream outs)
    {
        int fcode;
        int c;

        //Set up the globals:  g_init_bits - initial number of bits
        g_init_bits = initBits;

        //Set up the necessary values
        clear_flg = false;
        _numBits = g_init_bits;
        _maxCode = MaxCode(_numBits);

        ClearCode = 1 << (initBits - 1);
        EOFCode = ClearCode + 1;
        _freeEntry = ClearCode + 2;

        _charCount = 0; //Clear packet

        var ent = NextPixel();

        var hshift = 0;
        for (fcode = _hSize; fcode < 65536; fcode *= 2)
            ++hshift;

        hshift = 8 - hshift; // set hash code range bound

        var hsizeReg = _hSize;
        ResetCodeTable(hsizeReg); // clear hash table

        Output(ClearCode, outs);

        outer_loop: //OMG, a GOTO label.
        while ((c = NextPixel()) != Eof)
        {
            fcode = (c << _maxBits) + ent;
            var i = (c << hshift) ^ ent;

            if (htab[i] == fcode)
            {
                ent = _codeTab[i];
                continue;
            }

            if (htab[i] >= 0)
            {
                #region If it is a non-empty slot

                var disp = hsizeReg - i;
                if (i == 0)
                    disp = 1;
                do
                {
                    if ((i -= disp) < 0)
                        i += hsizeReg;

                    if (htab[i] == fcode)
                    {
                        ent = _codeTab[i];
                        goto outer_loop;
                    }
                } while (htab[i] >= 0);

                #endregion
            }

            Output(ent, outs);
            ent = c;

            if (_freeEntry < MaxMaxCode)
            {
                _codeTab[i] = _freeEntry++; // code -> hashtable
                htab[i] = fcode;
            }
            else
                ClearTable(outs);
        }

        //Put out the final code.
        Output(ent, outs);
        Output(EOFCode, outs);
    }

    /// <summary>
    /// Write all data into Stream.
    /// </summary>
    /// <param name="os">The Stream to write.</param>
    public void Encode(Stream os)
    {
        os.WriteByte(Convert.ToByte(_initCodeSize)); //Write "initial code size" byte

        //_remaining = _imgW * _imgH; //Reset navigation variables
        _curPixel = 0;

        Compress(_initCodeSize + 1, os); //Compress and write the pixel data

        os.WriteByte(0); //Write block terminator
    }

    /// <summary>
    /// Flush the packet to disk, and reset the accumulator
    /// </summary>
    /// <param name="outs">The Stream</param>
    void Flush(Stream outs)
    {
        if (_charCount > 0)
        {
            outs.WriteByte(Convert.ToByte(_charCount));
            outs.Write(_accumulator, 0, _charCount);
            _charCount = 0;
        }
    }

    int MaxCode(int numBits)
    {
        return (1 << numBits) - 1;
    }

    /// <summary>
    /// Return the next pixel from the image.
    /// </summary>
    /// <returns>The next pixel index(?).</returns>
    private int NextPixel()
    {
        #region Old Code

        //if (remaining == 0)
        //    return EOF;

        //--remaining;

        //int temp = curPixel + 1;
        //if ( temp < pixAry.GetUpperBound( 0 ))
        //{
        //    byte pix = pixAry[curPixel++];

        //    return pix & 0xff;
        //}
        //return 0xff;

        #endregion

        if (_curPixel <= _pixAry.GetUpperBound(0))
        {
            byte pix = _pixAry[_curPixel++];
            return pix & 0xff;
        }

        return Eof;
    }

    void Output(int code, Stream outs)
    {
        cur_accum &= masks[cur_bits];

        if (cur_bits > 0)
            cur_accum |= (code << cur_bits);
        else
            cur_accum = code;

        cur_bits += _numBits;

        while (cur_bits >= 8)
        {
            Add((byte)(cur_accum & 0xff), outs);
            cur_accum >>= 8;
            cur_bits -= 8;
        }

        // If the next entry is going to be too big for the code size,
        // then increase it, if possible.
        if (_freeEntry > _maxCode || clear_flg)
        {
            if (clear_flg)
            {
                _maxCode = MaxCode(_numBits = g_init_bits);
                clear_flg = false;
            }
            else
            {
                ++_numBits;

                _maxCode = _numBits == _maxBits ? 
                    MaxMaxCode : 
                    MaxCode(_numBits);
            }
        }

        if (code == EOFCode)
        {
            // At EOF, write the rest of the buffer. 8 bits each time.
            while (cur_bits > 0)
            {
                Add((byte)(cur_accum & 0xff), outs);
                cur_accum >>= 8; 
                cur_bits -= 8;
            }

            Flush(outs);
        }
    }
}