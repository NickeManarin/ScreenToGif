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

#region Java
/* NeuQuant Neural-Net Quantization Algorithm
 * ------------------------------------------
 *
 * Copyright (c) 1994 Anthony Dekker
 *
 * NEUQUANT Neural-Net quantization algorithm by Anthony Dekker, 1994.
 * See "Kohonen neural networks for optimal colour quantization"
 * in "Network: Computation in Neural Systems" Vol. 5 (1994) pp 351-367.
 * for a discussion of the algorithm.
 *
 * Any party obtaining a copy of these files from the author, directly or
 * indirectly, is granted, free of charge, a full and unrestricted irrevocable,
 * world-wide, paid up, royalty-free, nonexclusive right and license to deal
 * in this software and documentation files (the "Software"), including without
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons who receive
 * copies from any such party to do so, with the only requirement being
 * that this copyright notice remain intact.
 */

// Ported to Java 12/00 K Weiner
#endregion

using System;

namespace ScreenToGif.Encoding
{
    /// <summary>
    /// Neural Quantization Class
    /// </summary>
    public class NeuQuant
    {
        /* Program Skeleton
           ----------------
           [select samplefac in range 1..30]
           [read image from input file]
           pic = (unsigned char*) malloc(3*width*height);
           initnet(pic,3*width*height,samplefac);
           learn();
           unbiasnet();
           [write output image header, using writecolourmap(f)]
           inxbuild();
           write output image using inxsearch(b,g,r)
        */

        #region Variables

        /// <summary>
        /// Number of colours used.
        /// </summary>
        private const int Netsize = 256;

        //Four primes near 500 - assume no image has a length so large
        //that it is divisible by all four primes.
        private const int Prime1 = 499;
        private const int Prime2 = 491;
        private const int Prime3 = 487;
        private const int Prime4 = 503;

        /// <summary>
        /// Minimum size for input image.
        /// </summary>
        private const int MinPictureBytes = (3 * Prime4);

        /* Network Definitions
           ------------------- */
        private const int Maxnetpos = (Netsize - 1);
        private const int Netbiasshift = 4; /* bias for colour values */
        private const int NumCycles = 100; /* no. of learning cycles */

        /* defs for freq and bias */
        private const int IntBiasShift = 16; /* bias for fractions */
        private const int IntBias = (((int) 1) << IntBiasShift);
        private const int GammaShift = 10; /* gamma = 1024 */
        private const int Gamma = (((int)1) << GammaShift);
        private const int BetaShift = 10;
        private const int Beta = (IntBias >> BetaShift); /* beta = 1/1024 */

        private const int BetaGamma = (IntBias << (GammaShift - BetaShift));

        /* defs for decreasing radius factor */
        private const int InitRad = (Netsize >> 3); /* for 256 cols, radius starts */
        private const int RadiusBiasShift = 6; /* at 32.0 biased by 6 bits */
        private const int RadiusBias = (((int) 1) << RadiusBiasShift);
        private const int InitRadius = (InitRad*RadiusBias); /* and decreases by a */
        private const int RadiusDec = 30; /* factor of 1/30 each cycle */

        /* defs for decreasing alpha factor */
        private const int AlphaBiasShift = 10; /* alpha starts at 1.0 */
        private const int InitAlpha = (((int) 1) << AlphaBiasShift);

        private int _alphadec; /* biased by 10 bits */

        /* radbias and alpharadbias used for radpower calculation */
        private const int RadBiasShift = 8;
        private const int RadBias = (((int) 1) << RadBiasShift);
        private const int AlphaRadBShift = (AlphaBiasShift + RadBiasShift);
        private const int AlphaRadBias = (((int) 1) << AlphaRadBShift);

        /* Types and Global Variables
        -------------------------- */

        private byte[] _thepicture; /* the input image itself */
        private int _lengthcount; /* _lengthcount = H*W*3 */

        private int _samplefac; /* sampling factor 1..30 */

        //   typedef int pixel[4];                /* BGRc */
        private int[][] _network; /* the network itself - [netsize][4] */

        private int[] _netindex = new int[256];
        /* for network lookup - really 256 */

        private int[] _bias = new int[Netsize];
        /* bias and freq arrays for learning */
        private int[] _freq = new int[Netsize];
        private int[] _radpower = new int[InitRad];
        /* radpower for precomputation */

        #endregion

        /// <summary>
        /// Initialise the quantization process in range (0,0,0) to (255,255,255).
        /// </summary>
        /// <param name="thepic">The image in bytes.</param>
        /// <param name="len">The length of the pixels.</param>
        /// <param name="sample">Sample interval for the quantitizer.</param>
        public NeuQuant(byte[] thepic, int len, int sample)
        {
            int i;
            int[] p;

            _thepicture = thepic;
            _lengthcount = len;
            _samplefac = sample;

            _network = new int[Netsize][];
            for (i = 0; i < Netsize; i++)
            {
                _network[i] = new int[4];
                p = _network[i];
                p[0] = p[1] = p[2] = (i << (Netbiasshift + 8)) / Netsize;
                _freq[i] = IntBias / Netsize; /* 1/netsize */
                _bias[i] = 0;
            }
        }

        private byte[] ColorMap()
        {
            var map = new byte[3 * Netsize];
            var index = new int[Netsize];

            for (int i = 0; i < Netsize; i++)
                index[_network[i][3]] = i;

            int k = 0;
            for (int i = 0; i < Netsize; i++)
            {
                int j = index[i];
                map[k++] = (byte)(_network[j][0]);
                map[k++] = (byte)(_network[j][1]);
                map[k++] = (byte)(_network[j][2]);
            }
            return map;
        }

        /// <summary>
        /// Insertion sort of network and building of _netindex[0..255] (to do after unbias).
        /// </summary>
        private void Inxbuild()
        {
            int i, j, smallpos, smallval;
            int[] p;
            int[] q;
            int previouscol, startpos;

            previouscol = 0;
            startpos = 0;
            for (i = 0; i < Netsize; i++)
            {
                p = _network[i];
                smallpos = i;
                smallval = p[1]; /* index on g */

                #region find smallest in i..netsize-1

                for (j = i + 1; j < Netsize; j++)
                {
                    q = _network[j];
                    if (q[1] < smallval)
                    { /* index on g */
                        smallpos = j;
                        smallval = q[1]; /* index on g */
                    }
                }

                #endregion

                q = _network[smallpos];
                /* swap p (i) and q (smallpos) entries */
                if (i != smallpos)
                {
                    j = q[0];
                    q[0] = p[0];
                    p[0] = j;
                    j = q[1];
                    q[1] = p[1];
                    p[1] = j;
                    j = q[2];
                    q[2] = p[2];
                    p[2] = j;
                    j = q[3];
                    q[3] = p[3];
                    p[3] = j;
                }
                /* smallval entry is now in position i */
                if (smallval != previouscol)
                {
                    _netindex[previouscol] = (startpos + i) >> 1;
                    for (j = previouscol + 1; j < smallval; j++)
                        _netindex[j] = i;
                    previouscol = smallval;
                    startpos = i;
                }
            }
            _netindex[previouscol] = (startpos + Maxnetpos) >> 1;
            for (j = previouscol + 1; j < 256; j++)
                _netindex[j] = Maxnetpos; /* really 256 */
        }

        /// <summary>
        /// Main Learning Loop.
        /// </summary>
        private void Learn()
        {
            int i, j, b, g, r;
            int radius, rad, alpha, step, delta, samplepixels;
            byte[] p;
            int pix, lim;

            if (_lengthcount < MinPictureBytes)
                _samplefac = 1;
            _alphadec = 30 + ((_samplefac - 1) / 3);
            p = _thepicture;
            pix = 0;
            lim = _lengthcount;
            samplepixels = _lengthcount / (3 * _samplefac);
            delta = samplepixels / NumCycles;
            alpha = InitAlpha;
            radius = InitRadius;

            rad = radius >> RadiusBiasShift;
            if (rad <= 1)
                rad = 0;
            for (i = 0; i < rad; i++)
                _radpower[i] =
                    alpha * (((rad * rad - i * i) * RadBias) / (rad * rad));

            //fprintf(stderr,"beginning 1D learning: initial radius=%d\n", rad);

            if (_lengthcount < MinPictureBytes)
                step = 3;
            else if ((_lengthcount % Prime1) != 0)
                step = 3 * Prime1;
            else
            {
                if ((_lengthcount % Prime2) != 0)
                    step = 3 * Prime2;
                else
                {
                    if ((_lengthcount % Prime3) != 0)
                        step = 3 * Prime3;
                    else
                        step = 3 * Prime4;
                }
            }

            i = 0;
            while (i < samplepixels)
            {
                b = (p[pix + 0] & 0xff) << Netbiasshift;
                g = (p[pix + 1] & 0xff) << Netbiasshift;
                r = (p[pix + 2] & 0xff) << Netbiasshift;
                j = Contest(b, g, r);

                Altersingle(alpha, j, b, g, r);
                if (rad != 0)
                    Alterneigh(rad, j, b, g, r); /* alter neighbours */

                pix += step;
                if (pix >= lim)
                    pix -= _lengthcount;

                i++;
                if (delta == 0)
                    delta = 1;
                if (i % delta == 0)
                {
                    alpha -= alpha / _alphadec;
                    radius -= radius / RadiusDec;
                    rad = radius >> RadiusBiasShift;
                    if (rad <= 1)
                        rad = 0;
                    for (j = 0; j < rad; j++)
                        _radpower[j] =
                            alpha * (((rad * rad - j * j) * RadBias) / (rad * rad));
                }
            }
            //fprintf(stderr,"finished 1D learning: readonly alpha=%f !\n",((float)alpha)/initalpha);
        }

        /// <summary>
        /// Search for BGR values 0..255 (after net is unbiased) and return color index.
        /// </summary>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <param name="r">Red</param>
        /// <returns>The color index</returns>
        public int Map(int b, int g, int r)
        {
            int i, j, dist, a, bestd;
            int[] p;

            bestd = 1000; /* biggest possible dist is 256*3 */
            int best = -1;
            i = _netindex[g]; /* index on g */
            j = i - 1; /* start at _netindex[g] and work outwards */

            while ((i < Netsize) || (j >= 0))
            {
                if (i < Netsize)
                {
                    p = _network[i];
                    dist = p[1] - g; /* inx key */
                    if (dist >= bestd)
                        i = Netsize; /* stop iter */
                    else
                    {
                        i++;
                        if (dist < 0)
                            dist = -dist;
                        a = p[0] - b;
                        if (a < 0)
                            a = -a;
                        dist += a;
                        if (dist < bestd)
                        {
                            a = p[2] - r;
                            if (a < 0)
                                a = -a;
                            dist += a;
                            if (dist < bestd)
                            {
                                bestd = dist;
                                best = p[3];
                            }
                        }
                    }
                }
                if (j >= 0)
                {
                    p = _network[j];
                    dist = g - p[1]; /* inx key - reverse dif */
                    if (dist >= bestd)
                        j = -1; /* stop iter */
                    else
                    {
                        j--;
                        if (dist < 0)
                            dist = -dist;
                        a = p[0] - b;
                        if (a < 0)
                            a = -a;
                        dist += a;
                        if (dist < bestd)
                        {
                            a = p[2] - r;
                            if (a < 0)
                                a = -a;
                            dist += a;
                            if (dist < bestd)
                            {
                                bestd = dist;
                                best = p[3];
                            }
                        }
                    }
                }
            }
            return (best);
        }

        /// <summary>
        /// Starts the quantization.
        /// </summary>
        /// <returns>The color map.</returns>
        public byte[] Process()
        {
            Learn();
            Unbiasnet();
            Inxbuild();
            return ColorMap();
        }

        /// <summary>
        /// Unbias network to give byte values 0..255 and record position i to prepare for sort.
        /// </summary>
        private void Unbiasnet()
        {
            int i, j;

            for (i = 0; i < Netsize; i++)
            {
                _network[i][0] >>= Netbiasshift;
                _network[i][1] >>= Netbiasshift;
                _network[i][2] >>= Netbiasshift;
                _network[i][3] = i; /* record color no */
            }
        }

        /// <summary>
        /// Move adjacent neurons by precomputed alpha*(1-((i-j)^2/[r]^2)) in _radpower[|i-j|]
        /// </summary>
        /// <param name="rad"></param>
        /// <param name="i">Biased</param>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <param name="r">Red</param>
        private void Alterneigh(int rad, int i, int b, int g, int r)
        {
            int j, k, lo, hi, a, m;
            int[] p;

            lo = i - rad;
            if (lo < -1)
                lo = -1;
            hi = i + rad;
            if (hi > Netsize)
                hi = Netsize;

            j = i + 1;
            k = i - 1;
            m = 1;

            while ((j < hi) || (k > lo))
            {
                a = _radpower[m++];
                if (j < hi)
                {
                    p = _network[j++];
                    try
                    {
                        p[0] -= (a * (p[0] - b)) / AlphaRadBias;
                        p[1] -= (a * (p[1] - g)) / AlphaRadBias;
                        p[2] -= (a * (p[2] - r)) / AlphaRadBias;
                    }
                    catch (Exception e)
                    {
                    } // prevents 1.3 miscompilation
                }
                if (k > lo)
                {
                    p = _network[k--];
                    try
                    {
                        p[0] -= (a * (p[0] - b)) / AlphaRadBias;
                        p[1] -= (a * (p[1] - g)) / AlphaRadBias;
                        p[2] -= (a * (p[2] - r)) / AlphaRadBias;
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Move neuron i towards biased (b,g,r) by factor alpha.
        /// </summary>
        /// <param name="alpha">Alpha</param>
        /// <param name="i">Biased</param>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <param name="r">Red</param>
        private void Altersingle(int alpha, int i, int b, int g, int r)
        {
            //alter hit neuron.
            int[] n = _network[i];
            n[0] -= (alpha * (n[0] - b)) / InitAlpha;
            n[1] -= (alpha * (n[1] - g)) / InitAlpha;
            n[2] -= (alpha * (n[2] - r)) / InitAlpha;
        }

        /// <summary>
        /// Search for biased BGR values.
        /// </summary>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <param name="r">Red</param>
        /// <returns>Best bias position</returns>
        private int Contest(int b, int g, int r)
        {
            /* finds closest neuron (min dist) and updates freq */
            /* finds best neuron (min dist-bias) and returns position */
            /* for frequently chosen neurons, _freq[i] is high and _bias[i] is negative */
            /* _bias[i] = gamma*((1/netsize)-_freq[i]) */

            int i, dist, a, biasdist, betafreq;
            int bestpos, bestbiaspos, bestd, bestbiasd;
            int[] n;

            bestd = ~(((int)1) << 31);
            bestbiasd = bestd;
            bestpos = -1;
            bestbiaspos = bestpos;

            for (i = 0; i < Netsize; i++)
            {
                n = _network[i];
                dist = n[0] - b;
                if (dist < 0)
                    dist = -dist;
                a = n[1] - g;
                if (a < 0)
                    a = -a;
                dist += a;
                a = n[2] - r;
                if (a < 0)
                    a = -a;
                dist += a;
                if (dist < bestd)
                {
                    bestd = dist;
                    bestpos = i;
                }
                biasdist = dist - ((_bias[i]) >> (IntBiasShift - Netbiasshift));

                if (biasdist < bestbiasd)
                {
                    bestbiasd = biasdist;
                    bestbiaspos = i;
                }

                betafreq = (_freq[i] >> BetaShift);
                _freq[i] -= betafreq;
                _bias[i] += (betafreq << GammaShift);
            }

            _freq[bestpos] += Beta;
            _bias[bestpos] -= BetaGamma;

            return (bestbiaspos);
        }
    }
}