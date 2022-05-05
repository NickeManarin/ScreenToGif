using System.Collections;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Codification.Gif.Encoder.Quantization;

public class OctreeQuantizer : Quantizer
{
    private readonly Octree _octree;

    public OctreeQuantizer(int maxColorBits = 8) : base(false)
    {
        if (maxColorBits is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(maxColorBits), maxColorBits, "This should be between 1 and 8");

        //Construct the octree.
        _octree = new Octree(maxColorBits);
    }

    /// <summary>
    /// Process the pixel in the first pass of the algorithm.
    /// </summary>
    /// <param name="pixel">The pixel to quantize.</param>
    protected override void InitialQuantizePixel(Color pixel)
    {
        if (pixel.A == 0)
            return;

        //Add the color to the octree.
        _octree.AddColor(pixel);
    }

    /// <summary>
    /// Override this to process the pixel in the second pass of the algorithm.
    /// </summary>
    /// <param name="pixel">The pixel to quantize.</param>
    /// <returns>The quantized value.</returns>
    protected override byte QuantizePixel(Color pixel)
    {
        return (byte)_octree.GetPaletteIndex(pixel);
    }

    /// <summary>
    /// Retrieves the palette for the quantized image.
    /// </summary>
    /// <returns>The new color palette.</returns>
    internal override List<Color> BuildPalette()
    {
        MaxColorsWithTransparency = TransparentColor.HasValue ? MaxColors - 1 : MaxColors;

        //First off convert the octree to _maxColors colors
        var palette = _octree.Palletize(MaxColorsWithTransparency);

        //TODO: Since the color table changes in size by ^2 (64, 128, 256), if there is still space in the color table, there's no need for the (-1). Check

        //Add the transparent color to the last position.
        if (TransparentColor.HasValue)
            palette.Add(Color.FromArgb(0, TransparentColor.Value.R, TransparentColor.Value.G, TransparentColor.Value.B)); //I need to set a color that is not being used in the gif.

        //Just convert the array to a list.
        return palette.Cast<Color>().ToList();
    }

    /// <summary>
    /// Class responsible for color quantization.
    /// </summary>
    private class Octree
    {
        /// <summary>
        /// Mask used when getting the appropriate pixels for a given node.
        /// </summary>
        private static readonly int[] Mask = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

        /// <summary>
        /// The root of the octree.
        /// </summary>
        private readonly OctreeNode _root;

        /// <summary>
        /// Returns the array of reducible nodes.
        /// </summary>
        protected OctreeNode[] ReducibleNodes { get; }

        /// <summary>
        /// Maximum number of significant bits in the image.
        /// </summary>
        private readonly int _maxColorBits;

        /// <summary>
        /// Stores the last node quantized.
        /// </summary>
        private OctreeNode _previousNode;

        /// <summary>
        /// Caches the previous color quantizeds
        /// </summary>
        private Color _previousColor;

        /// <summary>
        /// Gets/Sets the number of leaves in the tree.
        /// </summary>
        private int Leaves { get; set; }

        /// <summary>
        /// Construct the octree.
        /// </summary>
        /// <param name="maxColorBits">The maximum number of significant bits in the image</param>
        public Octree(int maxColorBits)
        {
            _maxColorBits = maxColorBits;
            Leaves = 0;
            ReducibleNodes = new OctreeNode[9];
                
            _root = new OctreeNode(0, _maxColorBits, this);
            _previousColor = Colors.Transparent;
            _previousNode = null;
        }


        /// <summary>
        /// Add a given color value to the octree
        /// </summary>
        /// <param name="pixel"></param>
        public void AddColor(Color pixel)
        {
            //Check if this request is for the same color as the last
            if (_previousColor == pixel)
            {
                //If so, check if I have a previous node setup. This will only occur if the first color in the image
                //happens to be black, with an alpha component of zero.
                if (null == _previousNode)
                {
                    _previousColor = pixel;
                    _root.AddColor(pixel, _maxColorBits, 0, this);
                }
                else
                    //Just update the previous node
                    _previousNode.Increment(pixel);
            }
            else
            {
                _previousColor = pixel;
                _root.AddColor(pixel, _maxColorBits, 0, this);
            }
        }

        /// <summary>
        /// Reduce the depth of the tree.
        /// </summary>
        private void Reduce()
        {
            int index;

            //Find the deepest level containing at least one reducible node
            for (index = _maxColorBits - 1; index > 0 && null == ReducibleNodes[index]; index--);

            //Reduce the node most recently added to the list at level 'index'
            var node = ReducibleNodes[index];
            ReducibleNodes[index] = node.NextReducible;

            //Decrement the leaf count after reducing the node
            Leaves -= node.Reduce();

            //And just in case I've reduced the last color to be added, and the next color to
            //be added is the same, invalidate the previousNode...
            _previousNode = null;
        }

        /// <summary>
        /// Keep track of the previous node that was quantized.
        /// </summary>
        /// <param name="node">The node last quantized.</param>
        protected void TrackPrevious(OctreeNode node)
        {
            _previousNode = node;
        }

        /// <summary>
        /// Convert the nodes in the octree to a palette with a maximum of colorCount colors.
        /// </summary>
        /// <param name="colorCount">The maximum number of colors.</param>
        /// <returns>An arraylist with the palettized colors</returns>
        public ArrayList Palletize(int colorCount)
        {
            while (Leaves > colorCount)
                Reduce();

            //Now palettize the nodes.
            var palette = new ArrayList(Leaves);
            var paletteIndex = 0;

            _root.ConstructPalette(palette, ref paletteIndex);

            //And return the palette.
            return palette;
        }

        /// <summary>
        /// Get the palette index for the passed color
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        public int GetPaletteIndex(Color pixel)
        {
            return _root.GetPaletteIndex(pixel, 0);
        }



        /// <summary>
        /// Class which encapsulates each node in the tree
        /// </summary>
        protected class OctreeNode
        {
            /// <summary>
            /// Construct the node
            /// </summary>
            /// <param name="level">The level in the tree = 0 - 7</param>
            /// <param name="colorBits">The number of significant color bits in the image</param>
            /// <param name="octree">The tree to which this node belongs</param>
            public OctreeNode(int level, int colorBits, Octree octree)
            {
                //Construct the new node.
                _leaf = level == colorBits;

                _red = _green = _blue = 0;
                _pixelCount = 0;

                //If a leaf, increment the leaf count.
                if (_leaf)
                {
                    octree.Leaves++;
                    NextReducible = null;
                    Children = null;
                }
                else
                {
                    //Otherwise add this to the reducible nodes.
                    NextReducible = octree.ReducibleNodes[level];
                    octree.ReducibleNodes[level] = this;
                    Children = new OctreeNode[8];
                }
            }

            /// <summary>
            /// Add a color into the tree.
            /// </summary>
            /// <param name="pixel">The color</param>
            /// <param name="colorBits">The number of significant color bits</param>
            /// <param name="level">The level in the tree</param>
            /// <param name="octree">The tree to which this node belongs</param>
            public void AddColor(Color pixel, int colorBits, int level, Octree octree)
            {
                //Update the color information if this is a leaf
                if (_leaf)
                {
                    Increment(pixel);

                    //Setup the previous node.
                    octree.TrackPrevious(this);
                }
                else
                {
                    //Go to the next level down in the tree.
                    var shift = 7 - level;
                    var index = ((pixel.R & Mask[level]) >> (shift - 2)) |
                                ((pixel.G & Mask[level]) >> (shift - 1)) |
                                ((pixel.B & Mask[level]) >> (shift));

                    var child = Children[index];

                    if (null == child)
                    {
                        //Create a new child node & store in the array.
                        child = new OctreeNode(level + 1, colorBits, octree);
                        Children[index] = child;
                    }

                    //Add the color to the child node.
                    child.AddColor(pixel, colorBits, level + 1, octree);
                }
            }

            /// <summary>
            /// Get/Set the next reducible node.
            /// </summary>
            public OctreeNode NextReducible { get; private set; }

            /// <summary>
            /// Pointers to any child nodes.
            /// </summary>
            private OctreeNode[] Children { get; }

            /// <summary>
            /// Reduce this node by removing all of its children.
            /// </summary>
            /// <returns>The number of leaves removed.</returns>
            public int Reduce()
            {
                _red = _green = _blue = 0;
                var children = 0;

                //Loop through all children and add their information to this node.
                for (var index = 0; index < 8; index++)
                {
                    if (null == Children[index]) 
                        continue;

                    _red += Children[index]._red;
                    _green += Children[index]._green;
                    _blue += Children[index]._blue;
                    _pixelCount += Children[index]._pixelCount;
                    ++children;

                    Children[index] = null;
                }

                //Now change this to a leaf node.
                _leaf = true;

                //Return the number of nodes to decrement the leaf count by.
                return children - 1;
            }

            /// <summary>
            /// Traverse the tree, building up the color palette.
            /// </summary>
            /// <param name="palette">The palette.</param>
            /// <param name="paletteIndex">The current palette index.</param>
            public void ConstructPalette(IList palette, ref int paletteIndex)
            {
                if (_leaf)
                {
                    //Consume the next palette index.
                    _paletteIndex = paletteIndex++;
                        
                    //And set the color of the palette entry.
                    palette.Add(Color.FromRgb((byte)(_red / _pixelCount), (byte)(_green / _pixelCount), (byte)(_blue / _pixelCount)));
                }
                else
                {
                    //Loop through children looking for leaves.
                    for (var index = 0; index < 8; index++)
                    {
                        if (null != Children[index])
                            Children[index].ConstructPalette(palette, ref paletteIndex);
                    }
                }
            }

            /// <summary>
            /// Returns the palette index for the passed color.
            /// </summary>
            public int GetPaletteIndex(Color pixel, int level)
            {
                var paletteIndex = _paletteIndex;

                if (_leaf) 
                    return paletteIndex;

                var shift = 7 - level;
                var index = ((pixel.R & Mask[level]) >> (shift - 2)) |
                            ((pixel.G & Mask[level]) >> (shift - 1)) |
                            ((pixel.B & Mask[level]) >> (shift));

                if (null != Children[index])
                    paletteIndex = Children[index].GetPaletteIndex(pixel, level + 1);
                else
                    throw new Exception("Not expected!");

                return paletteIndex;
            }

            /// <summary>
            /// Increment the pixel count and add to the color information.
            /// </summary>
            public void Increment(Color pixel)
            {
                _pixelCount++;
                _red += pixel.R;
                _green += pixel.G;
                _blue += pixel.B;
            }

            /// <summary>
            /// Flag indicating that this is a leaf node.
            /// </summary>
            private bool _leaf;

            /// <summary>
            /// Number of pixels in this node.
            /// </summary>
            private int _pixelCount;

            private int _red;

            private int _green;

            private int _blue;

            /// <summary>
            /// The index of this node in the palette.
            /// </summary>
            private int _paletteIndex;
        }
    }
}