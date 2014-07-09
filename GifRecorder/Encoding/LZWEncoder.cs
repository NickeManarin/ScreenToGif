#region .NET Disclaimer/Info
//==
//
// gOODiDEA, uland.com
//==
//
// $Header :		$  
// $Author :		$
// $Date   :		$
// $Revision:		$
// $History:		$  
//  
//==
#endregion

#region Java Disclaimer
//=
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

using System;
using System.IO;

namespace ScreenToGif.Encoding
{
    /// <summary>
    /// Image compression routines.
    /// </summary>
    public class LZWEncoder
    {
        #region Variables

        private static readonly int EOF = -1;

        private int imgW, imgH;
        private byte[] pixAry;
        private int initCodeSize;
        private int remaining;
        private int curPixel;

        // General DEFINEs

        static readonly int BITS = 12;

        static readonly int HSIZE = 5003; // 80% occupancy


        int n_bits; // number of bits/code
        int maxbits = BITS; // user settable max # bits/code
        int maxcode; // maximum code, given n_bits
        int maxmaxcode = 1 << BITS; // should NEVER generate this code

        int[] htab = new int[HSIZE];
        int[] codetab = new int[HSIZE];

        int hsize = HSIZE; // for dynamic table sizing

        int free_ent = 0; // first unused entry

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
        int a_count;

        /// <summary>
        /// Define the storage for the packet accumulator.
        /// </summary>
        byte[] accum = new byte[256];

        #endregion

        /// <summary>
        /// Constructor of the compression class.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="pixels"></param>
        /// <param name="color_depth"></param>
        public LZWEncoder(int width, int height, byte[] pixels, int color_depth)
        {
            imgW = width;
            imgH = height;
            pixAry = pixels;
            initCodeSize = Math.Max(2, color_depth);
        }

        /// <summary>
        /// Add a character to the end of the current packet, and if it is 254 characters, flush the packet to disk.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="outs"></param>
        private void Add(byte c, Stream outs)
        {
            accum[a_count++] = c;
            if (a_count >= 254)
                Flush(outs);
        }

        //Clear out the hash table
        //Table clear for block compress
        private void ClearTable(Stream outs)
        {
            ResetCodeTable(hsize);
            free_ent = ClearCode + 2;
            clear_flg = true;

            Output(ClearCode, outs);
        }

        //Reset code table
        private void ResetCodeTable(int hsize)
        {
            for (int i = 0; i < hsize; ++i)
                htab[i] = -1;
        }

        private void Compress(int initBits, Stream outs)
        {
            int fcode;
            int i /* = 0 */;
            int c;
            int ent;
            int disp;
            int hsize_reg;
            int hshift;

            // Set up the globals:  g_init_bits - initial number of bits
            g_init_bits = initBits;

            // Set up the necessary values
            clear_flg = false;
            n_bits = g_init_bits;
            maxcode = MaxCode(n_bits);

            ClearCode = 1 << (initBits - 1);
            EOFCode = ClearCode + 1;
            free_ent = ClearCode + 2;

            a_count = 0; // clear packet

            ent = NextPixel();

            hshift = 0;
            for (fcode = hsize; fcode < 65536; fcode *= 2)
                ++hshift;
            hshift = 8 - hshift; // set hash code range bound

            hsize_reg = hsize;
            ResetCodeTable(hsize_reg); // clear hash table

            Output(ClearCode, outs);

            outer_loop: //OMG, a GOTO label.
            while ((c = NextPixel()) != EOF)
            {
                fcode = (c << maxbits) + ent;
                i = (c << hshift) ^ ent; // xor hashing

                if (htab[i] == fcode)
                {
                    ent = codetab[i];
                    continue;
                }
                else if (htab[i] >= 0) // non-empty slot
                {
                    disp = hsize_reg - i; // secondary hash (after G. Knott)
                    if (i == 0)
                        disp = 1;
                    do
                    {
                        if ((i -= disp) < 0)
                            i += hsize_reg;

                        if (htab[i] == fcode)
                        {
                            ent = codetab[i];
                            goto outer_loop;
                        }
                    } while (htab[i] >= 0);
                }
                Output(ent, outs);
                ent = c;
                if (free_ent < maxmaxcode)
                {
                    codetab[i] = free_ent++; // code -> hashtable
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
            os.WriteByte(Convert.ToByte(initCodeSize)); //Write "initial code size" byte

            remaining = imgW * imgH; //Reset navigation variables
            curPixel = 0;

            Compress(initCodeSize + 1, os); //Compress and write the pixel data

            os.WriteByte(0); //Write block terminator
        }

        /// <summary>
        /// Flush the packet to disk, and reset the accumulator
        /// </summary>
        /// <param name="outs">The Stream</param>
        void Flush(Stream outs)
        {
            if (a_count > 0)
            {
                outs.WriteByte(Convert.ToByte(a_count));
                outs.Write(accum, 0, a_count);
                a_count = 0;
            }
        }

        int MaxCode(int n_bits)
        {
            return (1 << n_bits) - 1;
        }

        /// <summary>
        /// Return the next pixel from the image.
        /// </summary>
        /// <returns>The next pixel index(?).</returns>
        private int NextPixel()
        {
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

            if (curPixel <= pixAry.GetUpperBound(0))
            {
                byte pix = pixAry[curPixel++];
                return pix & 0xff;
            }
            else
                return (EOF);
        }

        void Output(int code, Stream outs)
        {
            cur_accum &= masks[cur_bits];

            if (cur_bits > 0)
                cur_accum |= (code << cur_bits);
            else
                cur_accum = code;

            cur_bits += n_bits;

            while (cur_bits >= 8)
            {
                Add((byte)(cur_accum & 0xff), outs);
                cur_accum >>= 8;
                cur_bits -= 8;
            }

            // If the next entry is going to be too big for the code size,
            // then increase it, if possible.
            if (free_ent > maxcode || clear_flg)
            {
                if (clear_flg)
                {
                    maxcode = MaxCode(n_bits = g_init_bits);
                    clear_flg = false;
                }
                else
                {
                    ++n_bits;
                    if (n_bits == maxbits)
                        maxcode = maxmaxcode;
                    else
                        maxcode = MaxCode(n_bits);
                }
            }

            if (code == EOFCode)
            {
                // At EOF, write the rest of the buffer.
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
}