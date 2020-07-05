using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ScreenToGif.ImageUtil.Gif.Encoder.Quantization
{
    #region Copyright (C) Anthony Dekker, Kevin Weiner, gOODiDEA.NET, Simon Bridewell, Nicke Manarin
    // 
    // This program is free software; you can redistribute it and/or
    // modify it under the terms of the GNU General Public License
    // as published by the Free Software Foundation; either version 3
    // of the License, or (at your option) any later version.
    // 
    // This program is distributed in the hope that it will be useful,
    // but WITHOUT ANY WARRANTY; without even the implied warranty of
    // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    // GNU General Public License for more details.
    // 
    // You should have received a copy of the GNU General Public License
    // along with this program; if not, write to the Free Software
    // Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

    // You can read the full text of the GNU General Public License at:
    // http://www.gnu.org/licenses/gpl.html

    // See also the Wikipedia entry on the GNU GPL at:
    // http://en.wikipedia.org/wiki/GNU_General_Public_License
    #endregion

    public class NeuralQuantizer : Quantizer
    {
        #region Variables

        #region Network definitions

        /// <summary>
        /// Number of neurons in the neural network. Also the maximum number of colors in the quantized frame.
        /// Each neuron represents one of the colors in the palette of the quantized image.
        /// </summary>
        private readonly int _networkSize;

        /// <summary>
        /// Maximum possible neuron index within the neural network.
        /// One less than the number of neurons in the network.
        /// </summary>
        private int _maximumNeuronIndex;

        /// <summary>
        /// Controls the relationship between values supplied in the learning data and the values of the neurons in the network during learning.
        /// The larger this value, the larger the values held in the neurons will be in comparison to the values supplied during the learning loop.
        /// This allows a more precise positioning of the neurons whilst still using integer arithmetic.
        /// </summary>
        private const int NetworkBiasShift = 4;

        /// <summary>
        /// Number of learning cycles. The greater this value, the more often the alpha values used to move neurons will be decremented.
        /// </summary>
        private const int NumberOfLearningCycles = 100;

        #endregion

        #region Primes

        /// <summary>
        /// First prime number near 500.
        /// Assumes no image has a length so large that it is divisible all four primes.
        /// </summary>
        private const int Prime1 = 499;

        /// <summary>
        /// Second prime number near 500.
        /// Assumes no image has a length so large that it is divisible all four primes.
        /// </summary>
        private const int Prime2 = 491;

        /// <summary>
        /// Third prime number near 500.
        /// Assumes no image has a length so large that it is divisible all four primes.
        /// </summary>
        private const int Prime3 = 487;

        /// <summary>
        /// Fourth prime number near 500.
        /// Assumes no image has a length so large that it is divisible all four primes.
        /// </summary>
        private const int Prime4 = 503;

        #endregion

        #region Bias and frequency

        /// <summary>
        /// Array of values which decrease as the frequency of the corresponding neuron increases.
        /// The index into this array is the same as the index into the array of neurons.
        /// </summary>
        private int[] _biases;

        /// <summary>
        /// Array of values which indicate how often each neuron in the network has been chosen during the learning process.
        /// The index to this array is the same as the index in the array of neurons.
        /// </summary>
        private int[] _frequencies;

        /// <summary>
        /// Alpha values controlling how far towards a target co-ordinate any neighbouring neurons are moved.
        /// </summary>
        private int[] _neighbourhoodAlphas;

        #endregion

        #region Definitions for decreasing alpha factor

        /// <summary>
        /// The initial value of alpha will be set to 1, left shifted by this many bits.
        /// </summary>
        private const int AlphaBiasShift = 10; /* alpha starts at 1.0 */

        /// <summary>
        /// The starting value of alpha.
        /// Alpha is a factor which controls how far neurons are moved during the learning loop, and it decreases as learning proceeds.
        /// </summary>
        private const int InitialAlpha = 1 << AlphaBiasShift;

        #endregion

        #region Frequency and bias definitions

        /// <summary>
        /// Bias for fractions. The larger this value is, the larger IntBias will be.
        /// Larger values will also make the bias of a neuron a more significant factor than the distance from the supplied co-ordinate when identifying the best neuron for a given co-ordinate.
        /// </summary>
        private const int IntBiasShift = 16;

        /// <summary>
        /// The larger this value is, the higher the initial frequency will be for each neuron, and the more the bias and frequency of the closest neuron will be adjusted by during the learning loop.
        /// </summary>
        private const int IntBias = 1 << IntBiasShift;

        /// <summary>
        /// The larger this value is, the larger Gamma will be.
        /// Larger values also result in the bias of all neurons being increased by a greater amount in each iteration through the learning process.
        /// </summary>
        private const int GammaShift = 10; /* Gamma = 1024 */

        /// <summary>
        /// The larger this value is, the smaller ClosestNeuronFrequencyIncrement and ClosestNeuronBiasDecrement will be.
        /// This means that larger values will also result in the frequency of all neurons being decreased by less and the bias being increased by less at each step of the learning loop.
        /// </summary>
        private const int BetaShift = 10;

        /// <summary>
        /// The larger this value is, the more the frequency of the closest neuron will be increased by during the learning loop.
        /// </summary>
        private const int ClosestNeuronFrequencyIncrement = IntBias >> BetaShift; /* Beta = 1/1024 */

        /// <summary>
        /// The larger this value is, the more the bias of the closest neuron will be decreased by during the learning loop.
        /// </summary>
        private const int ClosestNeuronBiasDecrement = IntBias << (GammaShift - BetaShift);

        #endregion

        #region Definitions for decreasing radius factor

        /// <summary>
        /// Initial radius.
        /// The initial unbiased neuron neighbourhood size is set to this multiplied by the neighbourhood size bias.
        /// This is also the size of the array of alphas for shifting neighbouring neurons.
        /// </summary>
        private int _initialNeighbourhoodSize;

        /// <summary>
        /// The neuron neighbourhood size is set by shifting the unbiased neighbourhood size this many bits to the right.
        /// </summary>
        private const int NeighbourhoodSizeBiasShift = 6;

        /// <summary>
        /// Radius bias.
        /// The initial unbiased neuron neighbourhood size is set to this multiplied by the initial radius.
        /// </summary>
        private const int NeighbourhoodSizeBias = 1 << NeighbourhoodSizeBiasShift;

        /// <summary>
        /// The initial value for the unbiased size of a neuron neighbourhood.
        /// </summary>
        private int _initialUnbiasedNeighbourhoodSize;

        /// <summary>
        /// Factor for reducing the unbiased neighbourhood size.
        /// </summary>
        private const int UnbiasedNeighbourhoodSizeDecrement = 30;

        #endregion

        #region Definitions for radius calculations

        /// <summary>
        /// The greater this value, the greater RadBias and AlphaRadBiasShift will be.
        /// </summary>
        private const int RadiusBiasShift = 8;

        /// <summary>
        /// The greater this value, the larger alpha will be, and the more neighbouring neurons will be moved by during the learning process.
        /// </summary>
        private const int RadiusBias = 1 << RadiusBiasShift;

        /// <summary>
        /// The greater this value, the greater _alphaRadBias will be, and so the less neighbouring neurons will be moved by during the learning process.
        /// </summary>
        private const int AlphaRadiusBiasShift = (AlphaBiasShift + RadiusBiasShift);

        /// <summary>
        /// The greater this value, the less neighbouring neurons will be moved by during the learning process.
        /// </summary>
        private const int AlphaRadiusBias = 1 << AlphaRadiusBiasShift;

        #endregion

        #region Other variables

        /// <summary>
        /// Height * Width *3 (H*W*3).
        /// </summary>
        private int _pixelBytesCount;

        /// <summary>
        /// Gets and sets quality of color quantization (conversion of images to the maximum 256 colors allowed by the GIF specification).
        /// Lower values (minimum = 1) produce better colors, but slow processing significantly.
        /// 10 is the default, and produces good color mapping at reasonable speeds.  
        /// Values greater than 20 do not yield significant improvements in speed.
        /// </summary>
        private int _samplingFactor;

        /// <summary>
        /// The neural network.
        /// An array of 256 neurons, each of which is an array of 4 bytes.
        /// Each neuron holds a colour intensity, in the order red, green, blue.
        /// The fourth element of the array holds the neuron's original index in the network before it is sorted.
        /// </summary>
        private int[][] _network; /* the network itself - [netsize][4] */

        /// <summary>
        /// Used for locating colours in the neural network - the index of this array is the green value of the colour to look for.
        /// </summary>
        private int[] _indexOfGreen;

        #endregion

        #endregion

        /// <summary>
        /// Neural network color quantization.
        /// </summary>
        /// <param name="samplingFactor">From 1 to 20. Using 1 will give the best results, but it will be slower.</param>
        /// <param name="maximumColors">Maximum quantity of colors.</param>
        public NeuralQuantizer(int samplingFactor, int maximumColors = 256) : base(false)
        {
            _samplingFactor = samplingFactor;
            _networkSize = maximumColors;
        }


        internal override void FirstPass(byte[] pixels)
        {
            #region Prepare variables

            MaxColorsWithTransparency = TransparentColor.HasValue ? _networkSize - 1 : _networkSize;
            _maximumNeuronIndex = MaxColorsWithTransparency - 1;
            _network = new int[MaxColorsWithTransparency][];
            _indexOfGreen = new int[256];
            _biases = new int[MaxColorsWithTransparency];
            _frequencies = new int[MaxColorsWithTransparency];
            _initialNeighbourhoodSize = Math.Max(MaxColorsWithTransparency >> 3, 1);
            _neighbourhoodAlphas = new int[_initialNeighbourhoodSize];
            _initialUnbiasedNeighbourhoodSize = _initialNeighbourhoodSize * NeighbourhoodSizeBias;

            for (var neuronIndex = 0; neuronIndex < MaxColorsWithTransparency; neuronIndex++)
            {
                _network[neuronIndex] = new int[4];
                _network[neuronIndex][0] = _network[neuronIndex][1] = _network[neuronIndex][2] = (neuronIndex << (NetworkBiasShift + 8)) / MaxColorsWithTransparency;

                _frequencies[neuronIndex] = IntBias / MaxColorsWithTransparency;
                _biases[neuronIndex] = 0;
            }

            #endregion

            Learn(pixels);

            UnbiasNetwork();
            BuildIndex();
        }

        internal override List<Color> BuildPalette()
        {
            var map = new byte[3 * MaxColorsWithTransparency];
            var index = new int[MaxColorsWithTransparency];

            //Gets the index of each color.
            for (var i = 0; i < MaxColorsWithTransparency; i++)
                index[_network[i][3]] = i;

            var colors = new List<Color>();

            var k = 0;
            for (var i = 0; i < MaxColorsWithTransparency; i++)
            {
                var j = index[i];

                //BGR.
                map[k++] = (byte)(_network[j][0]);
                map[k++] = (byte)(_network[j][1]);
                map[k++] = (byte)(_network[j][2]);

                //Add repeated colors?

                colors.Add(new Color
                {
                    A = 255,
                    B = map[k - 3],
                    G = map[k - 2],
                    R = map[k - 1]
                });
            }

            if (TransparentColor.HasValue)
                colors.Add(TransparentColor.Value);

            return colors;
        }

        protected override byte QuantizePixel(Color pixel)
        {
            return MapColor(pixel.B, pixel.G, pixel.R);
        }
        

        private void Learn(byte[] pixels)
        {
            _pixelBytesCount = pixels.Length;

            #region Preparations for learning

            //If the image is so small that it has fewer pixels than the largest prime number used to determine how to step through the pixels, include every pixel in the sample.
            if (_pixelBytesCount < Prime4 * 4)
                _samplingFactor = 1;

            var alphaDecrement = 30 + (_samplingFactor - 1) / 4;
            var pixelIndex = 0;

            //Set the number of elements of the learning data to be examined during the learning loop. Pixels are in BGRA.
            //If _samplingFactor is 1 then every element will be examined. 
            //If _samplingFactor is 10 then one tenth of the elements will be examined.
            var pixelsToExamine = _pixelBytesCount / (4 * _samplingFactor);

            //Set how often the alpha value for shifting neurons is updated.
            //A value of 1 means it is updated once per pixel examined, 10 means it is updated every 10 pixels, and so on.
            var alphaUpdateFrequency = Math.Max(1, pixelsToExamine / NumberOfLearningCycles);

            //Alpha is a factor which controls how far neurons are moved during the learning loop, and it decreases as learning proceeds.
            var alpha = InitialAlpha;

            //Set the size of the neighbourhood which makes up the neighbouring neurons which also need to be moved when a neuron is moved.
            var unbiasedNeighbourhoodSize = _initialUnbiasedNeighbourhoodSize;

            var neighbourhoodSize = unbiasedNeighbourhoodSize >> NeighbourhoodSizeBiasShift;

            //Is this possible?
            if (neighbourhoodSize < 1)
                neighbourhoodSize = 1;

            //Set the initial alpha values for neighbouring neurons.
            SetNeighbourhoodAlphas(_neighbourhoodAlphas, neighbourhoodSize, alpha, RadiusBias);

            //Get the number of pixels to skip beween samples.
            var step = GetPixelIndexIncrement(_pixelBytesCount);

            #endregion

            #region Learning

            var pixelsExamined = 0;

            //var hashTable = new HashSet<int>();

            while (pixelsExamined < pixelsToExamine)
            {
                //By trying to ignore repeated colors, this gives the opportunity to other colors to be used instead, which may cause visual imperfections, specially with full frames (with lots of colors).
                //var hash = BitConverter.ToInt32(new[] { byte.MaxValue, pixels[pixelIndex + 0], pixels[pixelIndex + 1], pixels[pixelIndex + 2] }, 0);

                //Only ignore transparent pixels.
                if (pixels[pixelIndex + 3] > 0)// && !hashTable.Contains(hash))
                {
                    #region Move neurons

                    var blue = (pixels[pixelIndex + 0] & 0xff) << NetworkBiasShift;
                    var green = (pixels[pixelIndex + 1] & 0xff) << NetworkBiasShift;
                    var red = (pixels[pixelIndex + 2] & 0xff) << NetworkBiasShift;

                    var bestNeuronIndex = FindClosestAndReturnBestNeuron(blue, green, red);

                    //Move this neuron closer to the current element of the learning data by a factor of alpha.
                    MoveNeuron(alpha, bestNeuronIndex, blue, green, red);

                    //If appropriate, move neighbouring neurons closer to the color of the current pixel.
                    if (neighbourhoodSize != 0)
                        MoveNeighbouringNeurons(neighbourhoodSize, bestNeuronIndex, blue, green, red);

                    #endregion
                }

                //hashTable.Add(hash);

                #region Move on to the next learning data element to be examined

                pixelIndex += step;

                //If gone past the end of the learning data, wrap around to the start again.
                if (pixelIndex >= _pixelBytesCount)
                    pixelIndex -= _pixelBytesCount;

                //Keep track of how many elements have been examined so far.
                pixelsExamined++;

                #endregion

                #region Update the alpha values for moving neurons if appropriate

                if (pixelsExamined % alphaUpdateFrequency == 0)
                {
                    alpha -= alpha / alphaDecrement;
                    unbiasedNeighbourhoodSize -= unbiasedNeighbourhoodSize / UnbiasedNeighbourhoodSizeDecrement;
                    neighbourhoodSize = unbiasedNeighbourhoodSize >> NeighbourhoodSizeBiasShift;

                    if (neighbourhoodSize <= 1)
                        neighbourhoodSize = 0;

                    //Update the alpha values to be used for moving neighbouring neurons.
                    SetNeighbourhoodAlphas(_neighbourhoodAlphas, neighbourhoodSize, alpha, RadiusBias);
                }

                #endregion
            }

            #endregion
        }

        /// <summary>
        /// Sets the alpha values for moving neighbouring neurons.
        /// </summary>
        private static void SetNeighbourhoodAlphas(int[] neighbourhoodAlphas, int neighbourhoodSize, int alpha, int radiusBias)
        {
            //Get neighbourhood size squared - only need to calculate this once.
            var squared = neighbourhoodSize * neighbourhoodSize;

            for (var i = 0; i < neighbourhoodSize; i++)
                neighbourhoodAlphas[i] = alpha * ((squared - i * i) * radiusBias / squared);
        }

        /// <summary>
        /// Calculates an increment to step through the pixels of the image, such that all pixels will eventually be examined, but not sequentially.
        /// This is required because the learning loop needs to examine the pixels in a pseudo-random order.
        /// </summary>
        /// <returns>The increment.</returns>
        private static int GetPixelIndexIncrement(int pictureByteCount)
        {
            int step;
            
            if (pictureByteCount < Prime4 * 4)
                step = 4;
            else if (pictureByteCount % Prime1 != 0) //The number of pixels is not divisible by the first prime number.
                step = Prime1 * 4;
            else if (pictureByteCount % Prime2 != 0) //The number of pixels is not divisible by the second prime number.
                step = Prime2 * 4;
            else if (pictureByteCount % Prime3 != 0) //The number of pixels is not divisible by the third prime number.
                step = Prime3 * 4;
            else
            {
                //The number of pixels is divisible by the first, second and third prime numbers.
                //To cover this in a test case we'd need learning data consisting of over 119 million neurons!
                step = Prime4 * 4;
            }

            return step;
        }

        /// <summary>
        /// Finds the neuron which is closest to the supplied color, increases its frequency and decreases its bias (Search for biased BGR values).
        /// Finds the best neuron (close to the supplied color but not already chosen too many times) and returns its index in the neural network.
        /// </summary>
        /// <returns>
        /// The index in the neural network of a neuron which is close to the supplied co-ordinate but which hasn't already been chosen too many times.
        /// </returns>
        private int FindClosestAndReturnBestNeuron(int blue, int green, int red)
        {
            var bestDistance = ~(1 << 31); //Bitwise inverted.
            var bestBiasDistance = bestDistance;
            var closestNeuronIndex = -1;
            var bestBiasNeuronIndex = closestNeuronIndex;

            for (var neuronIndex = 0; neuronIndex < MaxColorsWithTransparency; neuronIndex++)
            {
                #region Calculate the distance

                ////Computes differences between neuron (color), and provided color.
                //var deltaRed = _network[neuronIndex][2] - red;
                //var deltaGreen = _network[neuronIndex][1] - green;
                //var deltaBlue = _network[neuronIndex][0] - blue;

                ////Makes values absolute.
                //if (deltaRed < 0)
                //    deltaRed = -deltaRed;
                //if (deltaGreen < 0)
                //    deltaGreen = -deltaGreen;
                //if (deltaBlue < 0)
                //    deltaBlue = -deltaBlue;

                ////Sums the distance.
                //var distance = deltaRed + deltaGreen + deltaBlue;

                ////If best so far, store it.
                //if (distance < bestDistance)
                //{
                //    bestDistance = distance;
                //    closestNeuronIndex = neuronIndex;
                //}

                var distance = _network[neuronIndex][0] - blue;

                if (distance < 0)
                    distance = -distance;

                var distanceIncrement = _network[neuronIndex][1] - green;
                
                if (distanceIncrement < 0) 
                    distanceIncrement = -distanceIncrement;
                
                distance += distanceIncrement;
                distanceIncrement = _network[neuronIndex][2] - red;
                
                if (distanceIncrement < 0)
                    distanceIncrement = -distanceIncrement;
                
                distance += distanceIncrement;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closestNeuronIndex = neuronIndex;
                }

                #endregion

                #region Calculate the bias distance

                //Bias distance takes into account the distance between the neuron and the co-ordinate, and also the neuron's bias.
                //The more frequently a neuron has already been chosen, the lower its bias, so less frequently-chosen neurons have a better chance of being returned by this method.
                //This ensures that the distribution of neurons is densest in areas of the network space which have most co-ordinates in the learning data.
                var biasDistance = distance - (_biases[neuronIndex] >> (IntBiasShift - NetworkBiasShift));

                if (biasDistance < bestBiasDistance)
                {
                    bestBiasDistance = biasDistance;
                    bestBiasNeuronIndex = neuronIndex;
                }

                #endregion

                #region Decrease the frequency and increase the bias for all neurons 
                
                var betaFrequency = _frequencies[neuronIndex] >> BetaShift;
                
                _frequencies[neuronIndex] -= betaFrequency;
                _biases[neuronIndex] += betaFrequency << GammaShift;

                #endregion
            }

            //Increase the frequency and decrease the bias for just the closest neuron.
            _frequencies[closestNeuronIndex] += ClosestNeuronFrequencyIncrement;
            _biases[closestNeuronIndex] -= ClosestNeuronBiasDecrement;

            return bestBiasNeuronIndex;
        }

        /// <summary>
        /// Moves the neuron at the supplied index in the neural network closer to the supplied color by a factor of alpha.
        /// Move neuron i towards biased (b,g,r) by factor alpha.
        /// </summary>
        private void MoveNeuron(int alpha, int neuronIndexToMove, int blue, int green, int red)
        {
            _network[neuronIndexToMove][0] -= (alpha * (_network[neuronIndexToMove][0] - blue)) / InitialAlpha;
            _network[neuronIndexToMove][1] -= (alpha * (_network[neuronIndexToMove][1] - green)) / InitialAlpha;
            _network[neuronIndexToMove][2] -= (alpha * (_network[neuronIndexToMove][2] - red)) / InitialAlpha;
        }

        /// <summary>
        /// Moves neighbours of the neuron at the supplied index in the network closer to the supplied colour.
        /// Move adjacent neurons by precomputed alpha * (1-((i-j)^2/[r]^2)) in radpower[|i-j|]
        /// </summary>
        private void MoveNeighbouringNeurons(int neighbourhoodSize, int neuronIndex, int blue, int green, int red)
        {
            #region Set lower and upper bounds of the neighbourhood of neurons to be moved

            var lowNeuronIndexLimit = neuronIndex - neighbourhoodSize;

            if (lowNeuronIndexLimit < -1)
                lowNeuronIndexLimit = -1;

            var highNeuronIndexLimit = neuronIndex + neighbourhoodSize;

            if (highNeuronIndexLimit > _network.Length)
                highNeuronIndexLimit = _network.Length;

            #endregion

            //Start with the neurons immediately before and after the specified index and work outwards.
            var highNeuronIndex = neuronIndex + 1;
            var lowNeuronIndex = neuronIndex - 1;
            var neighbourAlphaIndex = 1;

            while (highNeuronIndex < highNeuronIndexLimit || lowNeuronIndex > lowNeuronIndexLimit)
            {
                var neighbourhoodAlpha = _neighbourhoodAlphas[neighbourAlphaIndex++];

                if (highNeuronIndex < highNeuronIndexLimit)
                    MoveNeighbour(highNeuronIndex++, neighbourhoodAlpha, AlphaRadiusBias, blue, green, red);

                if (lowNeuronIndex > lowNeuronIndexLimit)
                    MoveNeighbour(lowNeuronIndex--, neighbourhoodAlpha, AlphaRadiusBias, blue, green, red);
            }
        }

        /// <summary>
        /// Moves an individual neighbouring neuron closer to the supplied colour by a factor of alpha.
        /// </summary>
        private void MoveNeighbour(int neuronIndexToMove, int alpha, int alphaRadiusBias, int blue, int green, int red)
        {
            _network[neuronIndexToMove][0] -= (alpha * (_network[neuronIndexToMove][0] - blue)) / alphaRadiusBias;
            _network[neuronIndexToMove][1] -= (alpha * (_network[neuronIndexToMove][1] - green)) / alphaRadiusBias;
            _network[neuronIndexToMove][2] -= (alpha * (_network[neuronIndexToMove][2] - red)) / alphaRadiusBias;
        }


        /// <summary>
        /// Unbias network to give byte values 0..255 and record position i to prepare for sort.
        /// </summary>
        private void UnbiasNetwork()
        {
            for (var neuronIndex = 0; neuronIndex < MaxColorsWithTransparency; neuronIndex++)
            {
                _network[neuronIndex][0] >>= NetworkBiasShift;
                _network[neuronIndex][1] >>= NetworkBiasShift;
                _network[neuronIndex][2] >>= NetworkBiasShift;
                _network[neuronIndex][3] = neuronIndex;  //Record the color number.
            }
        }


        /// <summary>
        /// Insertion sort of network and building of netindex[0..255] (to do after unbias).
        /// Populates the _indexOfGreen array with the indices in the network of colors with green values closest to 0 to 255.
        /// </summary>
        private void BuildIndex()
        {
            int greenValue;
            var previousLeastGreenValue = 0;
            var startingGreenValue = 0;

            for (var thisNeuronIndex = 0; thisNeuronIndex < MaxColorsWithTransparency; thisNeuronIndex++)
            {
                var thisNeuron = _network[thisNeuronIndex];

                //Find the least green neuron between the current neuron and the end of the network.
                var indexOfLeastGreenNeuron = IndexOfLeastGreenNeuron(thisNeuronIndex);
                var leastGreenNeuron = _network[indexOfLeastGreenNeuron];
                var greenValueOfLeastGreenNeuron = leastGreenNeuron[1];

                //Move the neuron with the lowest index towards the beginning of the array.
                if (thisNeuronIndex != indexOfLeastGreenNeuron)
                    SwapNeurons(thisNeuron, leastGreenNeuron);

                if (greenValueOfLeastGreenNeuron != previousLeastGreenValue)
                {
                    //Then we've found a new least green neuron so update the array of green indices accordingly
                    _indexOfGreen[previousLeastGreenValue] = (startingGreenValue + thisNeuronIndex) >> 1;

                    for (greenValue = previousLeastGreenValue + 1; greenValue < greenValueOfLeastGreenNeuron; greenValue++)
                        _indexOfGreen[greenValue] = thisNeuronIndex;

                    previousLeastGreenValue = greenValueOfLeastGreenNeuron;
                    startingGreenValue = thisNeuronIndex;
                }
            }

            _indexOfGreen[previousLeastGreenValue] = (startingGreenValue + _maximumNeuronIndex) >> 1;

            //Fill the remainder of the _indexOfGreen array with the index of the last neuron in the network.
            for (greenValue = previousLeastGreenValue + 1; greenValue < 256; greenValue++)
                _indexOfGreen[greenValue] = _maximumNeuronIndex;
        }

        /// <summary>
        /// Gets the index in the network of the neuron with the lowest green value, between the supplied index and the end of the network.
        /// </summary>
        /// <param name="startNeuronIndex">The index in the network to start searching at.</param>
        /// <returns>
        /// The index of the least green neuron.
        /// </returns>
        private int IndexOfLeastGreenNeuron(int startNeuronIndex)
        {
            //Start with the current neuron, its index and green value.
            var indexOfLeastGreenNeuron = startNeuronIndex;
            var greenValueOfLeastGreenNeuron = _network[startNeuronIndex][1];

            //And compare it with the remaining neurons.
            for (var otherNeuronIndex = startNeuronIndex + 1; otherNeuronIndex < MaxColorsWithTransparency; otherNeuronIndex++)
            {
                var otherNeuron = _network[otherNeuronIndex];

                if (otherNeuron[1] < greenValueOfLeastGreenNeuron)
                {
                    //The green value of otherNeuron is lower than that of the least green neuron seen so far, so otherNeuron becomes the least green.
                    indexOfLeastGreenNeuron = otherNeuronIndex;
                    greenValueOfLeastGreenNeuron = otherNeuron[1];
                }
            }

            return indexOfLeastGreenNeuron;
        }

        /// <summary>
        /// Swaps the values of the two supplied neurons.
        /// </summary>
        /// <param name="neuron1">One of the neurons whose value should be swapped with the other neuron.</param>
        /// <param name="neuron2">The other neuron, whose value should be swapped with the first neuron.</param>
        private static void SwapNeurons(int[] neuron1, int[] neuron2)
        {
            //Swaps the values of each of the co-ordinates of the 2 neurons.
            for (var i = 0; i < neuron1.Length; i++)
            {
                var temp = neuron1[i];

                neuron1[i] = neuron2[i];
                neuron2[i] = temp;
            }
        }


        /// <summary>
        /// Gets the index in the color table of the color closest to the supplied color.
        /// </summary>
        /// <param name="blue">Blue</param>
        /// <param name="green">Green</param>
        /// <param name="red">Red</param>
        /// <returns>Index in the colour table</returns>
        internal byte MapColor(int blue, int green, int red)
        {
            var bestIndex = -1;
            var bestDistance = 1000; //Biggest possible dist is 256 * 3.
            var highNeuronIndex = _indexOfGreen[green]; //Index on g.
            var lowNeuronIndex = highNeuronIndex - 1; //Start at netindex[g] and work outwards.

            while (highNeuronIndex < MaxColorsWithTransparency || lowNeuronIndex >= 0)
            {
                int distance;
                int[] thisNeuron;
                int distanceIncrement;

                if (highNeuronIndex < MaxColorsWithTransparency)
                {
                    thisNeuron = _network[highNeuronIndex];
                    distance = thisNeuron[1] - green; //Index key.

                    if (distance >= bestDistance)
                    {
                        highNeuronIndex = MaxColorsWithTransparency; //Stop iteration.
                    }
                    else
                    {
                        highNeuronIndex++;

                        if (distance < 0)
                            distance = -distance;

                        distanceIncrement = thisNeuron[0] - blue;

                        if (distanceIncrement < 0)
                            distanceIncrement = -distanceIncrement;

                        distance += distanceIncrement;

                        if (distance < bestDistance)
                        {
                            distanceIncrement = thisNeuron[2] - red;

                            if (distanceIncrement < 0)
                                distanceIncrement = -distanceIncrement;

                            distance += distanceIncrement;

                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestIndex = thisNeuron[3];
                            }
                        }
                    }
                }

                if (lowNeuronIndex >= 0)
                {
                    thisNeuron = _network[lowNeuronIndex];
                    distance = green - thisNeuron[1]; //Index key.

                    if (distance >= bestDistance)
                    {
                        lowNeuronIndex = -1; //Stop iteration.
                    }
                    else
                    {
                        lowNeuronIndex--;

                        if (distance < 0)
                            distance = -distance;

                        distanceIncrement = thisNeuron[0] - blue;

                        if (distanceIncrement < 0)
                            distanceIncrement = -distanceIncrement;

                        distance += distanceIncrement;

                        if (distance < bestDistance)
                        {
                            distanceIncrement = thisNeuron[2] - red;

                            if (distanceIncrement < 0)
                                distanceIncrement = -distanceIncrement;

                            distance += distanceIncrement;

                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestIndex = thisNeuron[3];
                            }
                        }
                    }
                }
            }

            return (byte) Math.Min(bestIndex, MaxColorsWithTransparency);
        }
    }
}