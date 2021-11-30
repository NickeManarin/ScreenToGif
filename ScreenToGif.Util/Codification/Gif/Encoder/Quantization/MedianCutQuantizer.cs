using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Codification.Gif.Encoder.Quantization;

/// <summary>
/// Based on:
/// https://github.com/ehotsk8/Picturea_ImageProcessing/blob/master/Picturea/PLL/Filters/MedianCutQuantizer.cs
/// </summary>
public class MedianCutQuantizer : Quantizer
{
    ///<summary>
    ///List of all colors in the palette
    ///</summary>
    protected List<Color> Colors = new();

    private List<MedianCutCube> _cubes = new();

    public MedianCutQuantizer() : base(false)
    { }

    /// <summary>
    /// Process the pixel in the first pass of the algorithm.
    /// </summary>
    /// <param name="pixel">The pixel to quantize</param>
    protected override void InitialQuantizePixel(Color pixel)
    {
        if (pixel.A == 0)
            return;

        Colors.Add(pixel);
    }

    /// <summary>
    /// Retrieve the palette for the quantized image
    /// </summary>
    /// <returns>The new color palette</returns>
    internal override List<Color> BuildPalette()
    {
        MaxColorsWithTransparency = TransparentColor.HasValue ? MaxColors - 1 : MaxColors;

        //Quantization.
        _cubes = new List<MedianCutCube> { new(Colors) };

        //Split the cube until we get required amount of colors.
        SplitCubes(_cubes, MaxColorsWithTransparency);

        //Get the final palette.
        var palette = new List<Color>(MaxColors);

        for (var i = 0; i < MaxColorsWithTransparency; i++)
        {
            palette.Add(_cubes[i].Color);
            _cubes[i].SetPaletteIndex(i);
        }

        //Add the transparent color to the last position.
        if (TransparentColor.HasValue)
            palette.Add(Color.FromArgb(0, TransparentColor.Value.R, TransparentColor.Value.G, TransparentColor.Value.B));

        return palette.ToList();
    }

    /// <summary>
    /// Override this to process the pixel in the second pass of the algorithm
    /// </summary>
    /// <param name="pixel">The pixel to quantize</param>
    /// <returns>The quantized value</returns>
    protected override byte QuantizePixel(Color pixel)
    {
        foreach (var cube in _cubes.Where(cube => cube.IsColorIn(pixel)))
            return (byte) cube.PaletteIndex;

        return 0;
    }

    /// <summary>
    /// Splits the list of cubes into smaller cubes until the list one gets the specified size.
    /// </summary>
    private void SplitCubes(List<MedianCutCube> cubes, int count)
    {
        var cubeIndexToSplit = cubes.Count - 1;

        while (cubes.Count < count)
        {
            var cubeToSplit = cubes[cubeIndexToSplit];
            MedianCutCube cube1, cube2;

            //Find the longest color size to use for splitting.
            if (cubeToSplit.RedSize >= cubeToSplit.GreenSize && cubeToSplit.RedSize >= cubeToSplit.BlueSize)
                cubeToSplit.SplitAtMedian(0, out cube1, out cube2);
            else if (cubeToSplit.GreenSize >= cubeToSplit.BlueSize)
                cubeToSplit.SplitAtMedian(1, out cube1, out cube2);
            else
                cubeToSplit.SplitAtMedian(2, out cube1, out cube2);

            //Remove the old "big" cube.
            cubes.RemoveAt(cubeIndexToSplit);

            //Add two smaller cubes instead
            cubes.Insert(cubeIndexToSplit, cube1);
            cubes.Insert(cubeIndexToSplit, cube2);

            if (--cubeIndexToSplit < 0)
                cubeIndexToSplit = cubes.Count - 1;
        }
    }

    private class MedianCutCube
    {
        private byte _redLowBound;
        private byte _redHighBound;

        private byte _greenLowBound;
        private byte _greenHighBound;

        private byte _blueLowBound;
        private byte _blueHighBound;

        private Color? _cubeColor = null;

        private readonly List<Color> _colorList;

        /// <summary>
        /// Length of the red side of the cube.
        /// </summary>
        public int RedSize => _redHighBound - _redLowBound;

        /// <summary>
        /// Length of the green size of the cube.
        /// </summary>
        public int GreenSize => _greenHighBound - _greenLowBound;

        /// <summary>
        /// Length of the blue size of the cube.
        /// </summary>
        public int BlueSize => _blueHighBound - _blueLowBound;

        public int PaletteIndex { get; private set; }

        /// <summary>
        /// The mean color of the cube.
        /// </summary>
        public Color Color
        {
            get
            {
                if (_cubeColor != null)
                    return _cubeColor.Value;

                int red = 0, green = 0, blue = 0;

                foreach (var color in _colorList)
                {
                    red += color.R;
                    green += color.G;
                    blue += color.B;
                }

                var colorsCount = _colorList.Count;

                if (colorsCount != 0)
                {
                    red /= colorsCount;
                    green /= colorsCount;
                    blue /= colorsCount;
                }

                _cubeColor = Color.FromRgb((byte)red, (byte)green, (byte)blue);

                return _cubeColor.Value;
            }
        }


        public MedianCutCube(List<Color> colors)
        {
            _colorList = colors;

            Shrink();
        }
            

        private void Shrink()
        {
            //Get the minimum/maximum values for each RGB component of specified colors.
            _redLowBound = _greenLowBound = _blueLowBound = 255;
            _redHighBound = _greenHighBound = _blueHighBound = 0;

            foreach (var colort in _colorList)
            {
                if (colort.R < _redLowBound) 
                    _redLowBound = colort.R;
                if (colort.R > _redHighBound) 
                    _redHighBound = colort.R;

                if (colort.G < _greenLowBound) 
                    _greenLowBound = colort.G;
                if (colort.G > _greenHighBound) 
                    _greenHighBound = colort.G;

                if (colort.B < _blueLowBound) 
                    _blueLowBound = colort.B;
                if (colort.B > _blueHighBound)
                    _blueHighBound = colort.B;
            }
        }

        /// <summary>
        /// Splits the cube into 2 smaller cubes using the specified color side for splitting.
        /// </summary>
        /// <param name="componentIndex"></param>
        /// <param name="medianCube1"></param>
        /// <param name="medianCube2"></param>
        public void SplitAtMedian(byte componentIndex, out MedianCutCube medianCube1, out MedianCutCube medianCube2)
        {
            switch (componentIndex)
            {
                case 0:
                    _colorList.Sort((p, n) => p.R.CompareTo(n.R));
                    break;

                case 1:
                    _colorList.Sort((p, n) => p.R.CompareTo(n.R));
                    break;

                case 2:
                    _colorList.Sort((p, n) => p.R.CompareTo(n.R));
                    break;
            }

            var medianIndex = _colorList.Count >> 1;

            medianCube1 = new MedianCutCube(_colorList.GetRange(0, medianIndex));
            medianCube2 = new MedianCutCube(_colorList.GetRange(medianIndex, _colorList.Count - medianIndex));
        }

        public void SetPaletteIndex(int newPaletteIndex)
        {
            PaletteIndex = newPaletteIndex;
        }

        public bool IsColorIn(Color color)
        {
            return (color.R >= _redLowBound && color.R <= _redHighBound) &&
                   (color.G >= _greenLowBound && color.G <= _greenHighBound) &&
                   (color.B >= _blueLowBound && color.B <= _blueHighBound);
        }
    }
}