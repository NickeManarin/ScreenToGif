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
        private const int Netsize = 256; //TODO: Support

        #region Four Primes

        //Four primes near 500 - assume no image has a length so large
        //that it is divisible by all four primes.
        private const int Prime1 = 499;
        private const int Prime2 = 491;
        private const int Prime3 = 487;
        private const int Prime4 = 503;

        #endregion

        /// <summary>
        /// Minimum size for input image.
        /// </summary>
        private const int MinPictureBytes = (3 * Prime4);

        #region  Network Definitions

        private const int Maxnetpos = (Netsize - 1);

        /// <summary>
        /// Bias for colour values.
        /// </summary>
        private const int Netbiasshift = 4;

        /// <summary>
        /// Number of learning cycles.
        /// </summary>
        private const int NumCycles = 100;

        #endregion

        #region Constants for Freqquency and Bias

        private const int IntBiasShift = 16; /* bias for fractions */
        private const int IntBias = (((int)1) << IntBiasShift);
        private const int GammaShift = 10; /* gamma = 1024 */
        private const int Gamma = (((int)1) << GammaShift);
        private const int BetaShift = 10;
        private const int Beta = (IntBias >> BetaShift); /* beta = 1/1024 */

        private const int BetaGamma = (IntBias << (GammaShift - BetaShift));

        #endregion

        #region Constants for Decreasing Radius Factor

        // For 256 cols, radius starts at 32.0 biased by 6 bits and decreases by a factor of 1/30 each cycle

        private const int InitRad = (Netsize >> 3);
        private const int RadiusBiasShift = 6;
        private const int RadiusBias = (((int)1) << RadiusBiasShift);
        private const int InitRadius = (InitRad * RadiusBias);
        private const int RadiusDec = 30;

        #endregion

        #region Constants for Decreasing Alpha Factor

        private const int AlphaBiasShift = 10; /* alpha starts at 1.0 */
        private const int InitAlpha = (((int)1) << AlphaBiasShift);

        #endregion

        /// <summary>
        /// Biased by 10 bits
        /// </summary>
        private int _alphadec;

        #region Radbias and AlphaRadBias used for Radpower Calculation

        private const int RadBiasShift = 8;
        private const int RadBias = (((int)1) << RadBiasShift);
        private const int AlphaRadBShift = (AlphaBiasShift + RadBiasShift);
        private const int AlphaRadBias = (((int)1) << AlphaRadBShift);

        #endregion

        #region Other Variables

        /// <summary>
        /// The input image itself as a byte Array.
        /// </summary>
        private readonly byte[] _thepicture;

        /// <summary>
        /// Height * Width *3 (H*W*3).
        /// </summary>
        private readonly int _lengthCount;

        /// <summary>
        /// Sampling factor 1..30
        /// </summary>
        private int _samplefac;

        /// <summary>
        /// The network itself [netsize][4]
        /// </summary>
        private readonly int[][] _network;

        /// <summary>
        /// For network lookup - really 256 (0 to 255)
        /// </summary>
        private readonly int[] _netIndex = new int[256];

        /// <summary>
        /// Bias Array for learning.
        /// </summary>
        private readonly int[] _bias = new int[Netsize];

        /// <summary>
        /// Frequency Array for learning.
        /// </summary>
        private readonly int[] _freq = new int[Netsize];

        /// <summary>
        /// Radpower for precomputation.
        /// </summary>
        private readonly int[] _radPower = new int[InitRad];

        #endregion

        #endregion

        /// <summary>
        /// Initialise the quantization process in range (0,0,0) to (255,255,255).
        /// </summary>
        /// <param name="thePic">The image in bytes.</param>
        /// <param name="len">The length of the pixels.</param>
        /// <param name="sample">Sample interval for the quantitizer.</param>
        public NeuQuant(byte[] thePic, int len, int sample)
        {
            _thepicture = thePic;
            _lengthCount = len;
            _samplefac = sample;

            _network = new int[Netsize][];

            for (int i = 0; i < Netsize; i++)
            {
                _network[i] = new int[4];

                _network[i][0] = _network[i][1] = _network[i][2] = (i << (Netbiasshift + 8)) / Netsize;
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
            int j;

            var previouscol = 0;
            var startpos = 0;

            for (int i = 0; i < Netsize; i++)
            {
                int[] p = _network[i];
                int smallpos = i;
                int smallval = p[1];

                #region Find Smallest in i..netsize-1

                int[] q;
                for (int b = i + 1; b < Netsize; b++)
                {
                    q = _network[b];

                    if (q[1] < smallval)
                    {
                        /* index on g */
                        smallpos = b;
                        smallval = q[1]; /* index on g */
                    }
                }

                #endregion

                q = _network[smallpos];

                #region Swap p (i) and q (smallpos) entries.

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

                #endregion

                //smallval entry is now in position i.
                if (smallval != previouscol)
                {
                    _netIndex[previouscol] = (startpos + i) >> 1;
                    for (j = previouscol + 1; j < smallval; j++)
                        _netIndex[j] = i;
                    previouscol = smallval;
                    startpos = i;
                }
            }

            _netIndex[previouscol] = (startpos + Maxnetpos) >> 1;

            for (j = previouscol + 1; j < 256; j++)
                _netIndex[j] = Maxnetpos; //Really 256.
        }

        /// <summary>
        /// Main Learning Loop.
        /// </summary>
        private void Learn()
        {
            int i;
            int step;

            if (_lengthCount < MinPictureBytes)
                _samplefac = 1;

            _alphadec = 30 + ((_samplefac - 1) / 3);

            var p = _thepicture;
            var pix = 0;
            var lim = _lengthCount;

            var samplepixels = _lengthCount / (3 * _samplefac);
            var delta = samplepixels / NumCycles;
            var alpha = InitAlpha;
            var radius = InitRadius;

            var rad = radius >> RadiusBiasShift;
            if (rad <= 1)
                rad = 0;
            for (i = 0; i < rad; i++)
                _radPower[i] =
                    alpha * (((rad * rad - i * i) * RadBias) / (rad * rad));

            //Console.WriteLine("Beginning 1D learning: Initial radius= " + rad);

            if (_lengthCount < MinPictureBytes)
                step = 3;
            else if ((_lengthCount % Prime1) != 0)
                step = 3 * Prime1;
            else
            {
                if ((_lengthCount % Prime2) != 0)
                    step = 3 * Prime2;
                else
                {
                    if ((_lengthCount % Prime3) != 0)
                        step = 3 * Prime3;
                    else
                        step = 3 * Prime4;
                }
            }

            i = 0;
            while (i < samplepixels)
            {
                #region Get Blue-Green-Red

                var b = (p[pix + 0] & 0xff) << Netbiasshift;
                var g = (p[pix + 1] & 0xff) << Netbiasshift;
                var r = (p[pix + 2] & 0xff) << Netbiasshift;

                #endregion

                var bestBias = Contest(b, g, r);

                Altersingle(alpha, bestBias, b, g, r);

                if (rad != 0)
                    AlterNeighbour(rad, bestBias, b, g, r);

                pix += step;
                if (pix >= lim)
                    pix -= _lengthCount;

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

                    for (bestBias = 0; bestBias < rad; bestBias++)
                        _radPower[bestBias] =
                            alpha * (((rad * rad - bestBias * bestBias) * RadBias) / (rad * rad));
                }
            }

            //Console.WriteLine("Finished 1D learning: alpha= " + ((float)alpha) / InitAlpha);
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
            int bestD = 1000;
            int best = -1;

            int i = _netIndex[g];
            int j = i - 1;

            while (i < Netsize || j >= 0)
            {
                int dist;
                int a;

                if (i < Netsize)
                {
                    dist = _network[i][1] - g; //Inx key.

                    if (dist >= bestD)
                        i = Netsize; //Stop iteration.
                    else
                    {
                        if (dist < 0)
                            dist = -dist;
                        a = _network[i][0] - b;

                        if (a < 0)
                            a = -a;
                        dist += a;

                        if (dist < bestD)
                        {
                            a = _network[i][2] - r;
                            if (a < 0)
                                a = -a;
                            dist += a;

                            if (dist < bestD)
                            {
                                bestD = dist;
                                best = _network[i][3];
                            }
                        }

                        i++;
                    }
                }

                if (j >= 0)
                {
                    dist = g - _network[j][1]; //Inx key - Reverse difference.

                    if (dist >= bestD)
                        j = -1; //Stop iteration.
                    else
                    {
                        if (dist < 0)
                            dist = -dist;
                        a = _network[j][0] - b;

                        if (a < 0)
                            a = -a;
                        dist += a;

                        if (dist < bestD)
                        {
                            a = _network[j][2] - r;

                            if (a < 0)
                                a = -a;

                            dist += a;

                            if (dist < bestD)
                            {
                                bestD = dist;
                                best = _network[j][3];
                            }
                        }

                        j--;
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
        /// Unbias network to give byte values 0..255 and record position to prepare for sort.
        /// </summary>
        private void Unbiasnet()
        {
            for (int pos = 0; pos < Netsize; pos++)
            {
                _network[pos][0] >>= Netbiasshift;
                _network[pos][1] >>= Netbiasshift;
                _network[pos][2] >>= Netbiasshift;
                _network[pos][3] = pos; //Record color number for sorting.
            }
        }

        /// <summary>
        /// Move adjacent neurons by precomputed alpha*(1-((i-j)^2/[r]^2)) in _radpower[|i-j|]
        /// </summary>
        /// <param name="radValue">Biased Radius</param>
        /// <param name="bestBias">Biased Position</param>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <param name="r">Red</param>
        private void AlterNeighbour(int radValue, int bestBias, int b, int g, int r)
        {
            #region Low and High

            int low = bestBias - radValue;

            if (low < -1)
                low = -1;

            int high = bestBias + radValue;

            if (high > Netsize)
                high = Netsize;

            #endregion

            int j = bestBias + 1;
            int k = bestBias - 1;
            int m = 1;

            while (j < high || k > low)
            {
                int rad = _radPower[m++];

                if (j < high)
                {
                    try
                    {
                        _network[j][0] -= (rad * (_network[j][0] - b)) / AlphaRadBias;
                        _network[j][1] -= (rad * (_network[j][1] - g)) / AlphaRadBias;
                        _network[j][2] -= (rad * (_network[j][2] - r)) / AlphaRadBias;
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e.Message);
                    }

                    j++;
                }

                if (k > low)
                {
                    try
                    {
                        _network[k][0] -= (rad * (_network[k][0] - b)) / AlphaRadBias;
                        _network[k][1] -= (rad * (_network[k][1] - g)) / AlphaRadBias;
                        _network[k][2] -= (rad * (_network[k][2] - r)) / AlphaRadBias;
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e.Message);
                    }

                    k--;
                }
            }
        }

        /// <summary>
        /// Move neuron (bestBias) towards biased (b,g,r) by factor alpha.
        /// </summary>
        /// <param name="alpha">Alpha</param>
        /// <param name="bestBias">Biased</param>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <param name="r">Red</param>
        private void Altersingle(int alpha, int bestBias, int b, int g, int r)
        {
            //Alter hit neuron.
            _network[bestBias][0] -= (alpha * (_network[bestBias][0] - b)) / InitAlpha;
            _network[bestBias][1] -= (alpha * (_network[bestBias][1] - g)) / InitAlpha;
            _network[bestBias][2] -= (alpha * (_network[bestBias][2] - r)) / InitAlpha;
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
            /* Finds closest neuron (minimum distance) and updates freq */
            /* Finds best neuron (min dist-bias) and returns position */
            /* For frequently chosen neurons, _freq[i] is high and _bias[i] is negative */
            /* _bias[i] = gamma*((1/netsize)-_freq[i]) */

            int bestD = ~(1 << 31); //Bitwise inverted.
            int bestBiasD = bestD;
            int bestPos = -1;
            int bestBiasPos = bestPos;

            for (int i = 0; i < Netsize; i++)
            {
                int dist = _network[i][0] - b;

                if (dist < 0)
                    dist = -dist;

                int a = _network[i][1] - g;

                if (a < 0)
                    a = -a;

                dist += a;
                a = _network[i][2] - r;

                if (a < 0)
                    a = -a;

                dist += a;

                if (dist < bestD)
                {
                    bestD = dist;
                    bestPos = i;
                }

                int biasdist = dist - (_bias[i] >> (IntBiasShift - Netbiasshift));

                if (biasdist < bestBiasD)
                {
                    bestBiasD = biasdist;
                    bestBiasPos = i;
                }

                int betafreq = (_freq[i] >> BetaShift);
                _freq[i] -= betafreq;
                _bias[i] += (betafreq << GammaShift);
            }

            _freq[bestPos] += Beta;
            _bias[bestPos] -= BetaGamma;

            return bestBiasPos;
        }
    }
}