using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ScreenToGif.FileWriters;

namespace ScreenToGif.ImageUtil.Gif.LegacyEncoder
{
    /// <summary>
    /// Animated Gif Encoder Class
    /// </summary>
    public class AnimatedGifEncoder : IDisposable
    {
        #region Variables

        /// <summary>
        /// Image width.
        /// </summary>
        private int _width;

        /// <summary>
        /// Image height.
        /// </summary>
        private int _height;

        /// <summary>
        /// Transparent color if given.
        /// </summary>
        private Color _transparent = Color.Empty;

        /// <summary>
        /// Transparent index in color table.
        /// </summary>
        private int _transIndex;

        /// <summary>
        /// The number of interations, default as "no repeat".
        /// </summary>
        private int _repeat = -1;

        /// <summary>
        /// Frame delay.
        /// </summary>
        private int _delay = 0;

        /// <summary>
        /// Flag that tells about the output encoding.
        /// </summary>
        private bool _started = false;

        //	protected BinaryWriter bw;

        /// <summary>
        /// FileStream of the process.
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// Current frame.
        /// </summary>
        private Image _image;

        /// <summary>
        /// BGR byte array from frame.
        /// </summary>
        private byte[] _pixels;

        private List<Color> _colorList;

        /// <summary>
        /// Converted frame indexed to palette.
        /// </summary>
        private byte[] _indexedPixels;

        /// <summary>
        /// Number of bit planes.
        /// </summary>
        private int _colorDepth;

        /// <summary>
        /// BGR palette.
        /// </summary>
        private byte[] _colorTab;

        /// <summary>
        /// Active palette entries. Same as the number of color being used.
        /// </summary>
        private bool[] _usedEntry = new bool[256];

        /// <summary>
        /// Color table size (bits-1).
        /// </summary>
        private int _palSize = 7;

        /// <summary>
        /// Disposal code (-1 = use default).
        /// </summary>
        private int _dispose = -1;

        /// <summary>
        /// Close stream when finished.
        /// </summary>
        private bool _closeStream = false;

        /// <summary>
        /// True only for th first frame.
        /// </summary>
        private bool _firstFrame = true;

        /// <summary>
        /// If False, get size from first frame.
        /// </summary>
        private bool _sizeSet = false;

        /// <summary>
        /// Default sample interval for quantizer.
        /// </summary>
        private int _sample = 10;

        #endregion

        /// <summary>
        /// Sets the delay time between each frame, or changes it
        /// for subsequent frames (applies to last frame added).
        /// </summary>
        /// <param name="ms">Delay time in milliseconds</param>
        public void SetDelay(int ms)
        {
            _delay = (int)Math.Round(ms / 10.0f, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Sets the GIF frame disposal code for the last added frame
        /// and any subsequent frames.  Default is 0 if no transparent
        /// color has been set, otherwise 2.
        /// </summary>
        /// <param name="code">Disposal code.</param>
        public void SetDispose(int code)
        {
            if (code >= 0)
            {
                _dispose = code;
            }
        }

        /// <summary>
        /// Sets the number of times the set of GIF frames
        /// should be played.  Default is -t1; 0 means play
        /// indefinitely.  Must be invoked before the first
        /// image is added.
        /// </summary>
        /// <param name="iter">Number of iterations.</param>
        public void SetRepeat(int iter)
        {
            if (iter >= 0)
            {
                _repeat = iter;
            }
        }

        /// <summary>
        /// Sets the transparent color for the last added frame and any subsequent frames.
        /// Since all colors are subject to modification
        /// in the quantization process, the color in the final
        /// palette for each frame closest to the given color
        /// becomes the transparent color for that frame.
        /// May be set to null to indicate no transparent color.
        /// </summary>
        /// <param name="c">Color to be treated as transparent on display.</param>
        public void SetTransparent(Color c)
        {
            _transparent = c;
        }

        /// <summary>
        /// Adds next GIF frame.  The frame is not written immediately, but is
        /// actually deferred until the next frame is received so that timing
        /// data can be inserted.  Invoking <code>finish()</code> flushes all
        /// frames.  If <code>setSize</code> was not invoked, the size of the
        /// first image is used for all subsequent frames.
        /// </summary>
        /// <param name="im">BufferedImage containing frame to write.</param>
        /// <param name="x">The horizontal position of the frame</param>
        /// <param name="y">The vertical position of the frame</param>
        /// <returns>True if successful.</returns>
        public bool AddFrame(Image im, int x = 0, int y = 0)
        {
            if (im == null || !_started)
                return false;
            
            var ok = true;

            try
            {
                if (!_sizeSet)
                {
                    //Use first frame's size.
                    SetSize(im.Width, im.Height);
                }

                _image = im;
                GetImagePixels(); //Convert to correct format if necessary.
                AnalyzePixels(); //Build color table & map pixels.

                if (_firstFrame)
                {
                    WriteLsd(); //Logical screen descriptor.
                    WritePalette(); //Global color table.

                    if (_repeat >= 0)
                    {
                        //Use Netscape app extension to indicate a gif with multiple frames.
                        WriteNetscapeExt();
                    }
                }

                WriteGraphicCtrlExt(); //Write graphic control extension.
                WriteImageDesc(im.Width, im.Height, x, y); //Image descriptor.

                if (!_firstFrame)
                {
                    WritePalette(); //Local color table.
                }

                WritePixels(im.Width, im.Height); //Encode and write pixel data.
                _firstFrame = false;
            }
            catch (IOException e)
            {
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Flushes any pending data and closes output file. If writing to an OutputStream, the stream is not closed.
        /// </summary>
        /// <returns></returns>
        public bool Finish()
        {
            if (!_started) return false;

            var ok = true;
            _started = false;

            try
            {
                _fs.WriteByte(0x3b); //Gif trailer
                _fs.Flush();

                if (_closeStream)
                {
                    _fs.Close();
                }
            }
            catch (IOException e)
            {
                ok = false;
            }

            //Reset for subsequent use.
            _transIndex = 0;
            _fs = null;
            _image = null;
            _pixels = null;
            _indexedPixels = null;
            _colorTab = null;
            _closeStream = false;
            _firstFrame = true;

            return ok;
        }

        /// <summary>
        /// Sets frame rate in frames per second. Equivalent to <code>SetDelay(1000/fps)</code>.
        /// </summary>
        /// <param name="fps">Frame rate (frames per second)</param>
        public void SetFrameRate(float fps)
        {
            if (fps != 0f)
            {
                _delay = (int)Math.Round(100f / fps, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Sets quality of color quantization (conversion of images
        /// to the maximum 256 colors allowed by the GIF specification).
        /// Lower values (minimum = 1) produce better colors, but slow
        /// processing significantly.  10 is the default, and produces
        /// good color mapping at reasonable speeds.  Values greater
        /// than 20 do not yield significant improvements in speed.
        /// </summary>
        /// <param name="quality">Quality value greater than 0.</param>
        public void SetQuality(int quality)
        {
            if (quality < 1) quality = 1;
            _sample = quality;
        }

        /// <summary>
        /// Sets the GIF frame size. The default size is the
        /// size of the first frame added if this method is
        /// not invoked.
        /// </summary>
        /// <param name="w">The frame width.</param>
        /// <param name="h">The frame weight.</param>
        public void SetSize(int w, int h)
        {
            if (_started && !_firstFrame) return;

            _width = w;
            _height = h;

            if (_width < 1) throw new ArgumentException("Width can't be smaller than 1 pixel.");

            if (_height < 1) throw new ArgumentException("Height can't be smaller than 1 pixel.");

            _sizeSet = true;
        }

        /// <summary>
        /// Initiates GIF file creation on the given stream. The stream is not closed automatically.
        /// </summary>
        /// <param name="os">OutputStream on which GIF images are written.</param>
        /// <returns>False if initial write failed.</returns>
        public bool Start(FileStream os)
        {
            if (os == null) return false;

            var ok = true;
            _closeStream = false;
            _fs = os;

            try
            {
                WriteString("GIF89a"); //Header
            }
            catch (IOException e)
            {
                LogWriter.Log(e, "Writing the header of the gif.");
                ok = false;
            }

            return _started = ok;
        }

        /// <summary>
        /// Initiates writing of a GIF file with the specified name.
        /// </summary>
        /// <param name="file">String containing output file name.</param>
        /// <returns>False if open or initial write failed.</returns>
        public bool Start(string file)
        {
            bool ok;

            try
            {
                //if (File.Exists(file))
                //    File.Delete(file);

                _fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
                ok = Start(_fs);
                _closeStream = true;
            }
            catch (IOException e)
            {
                LogWriter.Log(e, "Tried to start the file stream.");
                ok = false;
            }

            return _started = ok;
        }

        /// <summary>
        /// Analyzes image colors and creates color map.
        /// </summary>
        private void AnalyzePixels()
        {
            var nPix = _pixels.Length / 3;
            _indexedPixels = new byte[nPix];

            var colorTable = _colorList.AsParallel().GroupBy(x => x) //Grouping based on its value
                .OrderByDescending(g => g.Count()) //Order by most frequent values
                .Select(g => g.FirstOrDefault()) //take the first among the group
                .ToList(); //Could use .Take(256)

            _usedEntry = new bool[256];

            if (colorTable.Count <= 256)
            {
                #region No quantitizer needed

                _colorTab = new byte[768];

                int indexAux = 0;
                foreach (var color in colorTable)
                {
                    _colorTab[indexAux++] = color.R;
                    _colorTab[indexAux++] = color.G;
                    _colorTab[indexAux++] = color.B;
                }

                //var grouped = _pixels.Select((x, i) => new { x, i })
                //    .GroupBy(x => x.i / 3)
                //    .Select(g => g.ToList())
                //    .Select(g => new { R = g[0], G = g[1], B = g[2] })
                //    .Distinct();

                var k = 0;
                for (var i = 0; i < nPix; i++)
                {
                    var b = _pixels[k++];
                    var g = _pixels[k++];
                    var r = _pixels[k++];

                    var pos = colorTable.IndexOf(Color.FromArgb(r, g, b));

                    if (pos == -1 || pos > 255)
                        pos = 0;

                    _usedEntry[pos] = true;
                    _indexedPixels[i] = (byte)pos;
                }

                //Get closest match to transparent color if specified.
                if (_transparent != Color.Empty)
                {
                    _transIndex = colorTable.IndexOf(Color.FromArgb(_transparent.R, _transparent.G, _transparent.B));

                    //if (_transIndex == -1)
                    //    _transIndex = 0;
                }

                #endregion
            }
            else
            {
                #region Quantitizer needed

                //Neural quantitizer.
                var nq = new NeuQuant(_pixels, _sample);

                //Create reduced palette.
                _colorTab = nq.Process();

                //Map image pixels to new palette.
                var k = 0;
                for (var i = 0; i < nPix; i++)
                {
                    var index = nq.Map(_pixels[k++], _pixels[k++], _pixels[k++]);

                    _usedEntry[index] = true;
                    _indexedPixels[i] = (byte)index;
                }

                //Get closest match to transparent color if specified.
                if (_transparent != Color.Empty)
                {
                    _transIndex = nq.Map(_transparent.B, _transparent.G, _transparent.R);
                }

                #endregion
            }

            _colorDepth = 8;
            _palSize = 7;
            _pixels = null;
        }

        /// <summary>
        /// Returns index of palette color closest to given color.
        /// </summary>
        /// <param name="c">The color to search for in the pallette.</param>
        /// <returns>The index of the pallete color.</returns>
        private int FindClosest(Color c)
        {
            if (_colorTab == null) return -1;

            int r = c.R;
            int g = c.G;
            int b = c.B;
            var minpos = 0;
            var dmin = 256 * 256 * 256;
            var len = _colorTab.Length;

            for (var i = 0; i < len;)
            {
                var dr = r - (_colorTab[i++] & 0xff);
                var dg = g - (_colorTab[i++] & 0xff);
                var db = b - (_colorTab[i] & 0xff);
                var d = dr * dr + dg * dg + db * db;
                var index = i / 3;

                if (_usedEntry[index] && (d < dmin))
                {
                    dmin = d;
                    minpos = index;
                }

                i++;
            }

            return minpos;
        }

        /// <summary>
        /// Extracts image pixels into byte array "pixels".
        /// </summary>
        private void GetImagePixels()
        {
            //Performance upgrade, now encoding takes half of the time, due to Marshal calls.
            var count = 0;
            var tempBitmap = new Bitmap(_image);

            var pixelUtil = new PixelUtilOld(tempBitmap);
            pixelUtil.LockBits();

            _colorList = new List<Color>();
            _pixels = new byte[3 * _image.Width * _image.Height];

            for (var th = 0; th < _image.Height; th++)
            {
                for (var tw = 0; tw < _image.Width; tw++)
                {
                    var color = pixelUtil.GetPixel(tw, th);
                    _colorList.Add(color);

                    _pixels[count] = color.B;
                    count++;
                    _pixels[count] = color.G;
                    count++;
                    _pixels[count] = color.R;
                    count++;
                }
            }

            pixelUtil.UnlockBits();
        }

        /// <summary>
        /// Writes Graphic Control Extension.
        /// </summary>
        private void WriteGraphicCtrlExt()
        {
            _fs.WriteByte(0x21); //Extension introducer
            _fs.WriteByte(0xf9); //GCE label
            _fs.WriteByte(4); //Data block size

            //Use Inplace if you want to Leave the last frame pixel.
            //#define GCE_DISPOSAL_NONE 0 //Same as "Undefined" undraw method
            //#define GCE_DISPOSAL_INPLACE 1 //Same as "Leave" undraw method in MS Gif Animator 1.01
            //#define GCE_DISPOSAL_BACKGROUND 2 //Same as "Restore background"
            //#define GCE_DISPOSAL_RESTORE 3 //Same as "Restore previous"

            //If transparency is set:
            //First frame as "Leave" with no Transparency.
            //Following frames as "Undefined" with Transparency.

            int transp = 0, disp = 0;

            if (_transparent != Color.Empty)
            {
                if (_firstFrame)
                {
                    transp = 0x00;
                    disp = 1 << 2; //It needs a shift of 2.
                }
                else
                {
                    transp = 1;
                    disp = 0;
                }
            }

            if (_transIndex == -1)
            {
                transp = 0;
                _transIndex = 0;
            }

            //Packed fields
            _fs.WriteByte(Convert.ToByte(
                000 | //#1:3 - Reserved
                disp | //#4:6 - Disposal
                0 | //#7 - User Input (0 = None)
                transp)); //#8 - Transparency Flag
                
            WriteShort(_delay); //Delay x 1/100 sec
            _fs.WriteByte(Convert.ToByte( _transIndex)); //Transparent color index
            _fs.WriteByte(0); //Block terminator
        }

        /// <summary>
        /// Writes Image Descriptor.
        /// </summary>
        private void WriteImageDesc(int width, int heigth, int x = 0, int y = 0)
        {
            _fs.WriteByte(0x2c); //Image separator
            WriteShort(x); //Image position x,y = 0,0
            WriteShort(y);

            //Image size
            WriteShort(width);
            WriteShort(heigth);

            if (_firstFrame)
            {
                //No LCT  - GCT is used for first (or only) frame
                _fs.WriteByte(0);
            }
            else
            {
                //Specify normal LCT, Packed Fields
                _fs.WriteByte(Convert.ToByte(
                    0x80 | //#1 Local Color Table? - (1 = Yes)
                    0 | //#2 Interlace? - (0 = No)
                    0 | //#3 Sorted? - (0 = No)
                    0 | //#4-5 Reserved
                    _palSize)); //#6-8 Size of Color Table
            }
        }

        /// <summary>
        /// Writes Logical Screen Descriptor
        /// </summary>
        private void WriteLsd()
        {
            //Logical screen size
            WriteShort(_width);
            WriteShort(_height);

            //Packed field
            _fs.WriteByte(Convert.ToByte(
                    0x80 |   //#1   : Global Color Table Flag (1 = GCT Used)
                    0x70 |   //#2-4 : Color Resolution = 7
                    0x00 |   //#5   : GCT Sort Flag = (0 = Not Sorted)
                    _palSize)); //#6-8 : GCT Size
                    
            _fs.WriteByte(0); //Background color index
            _fs.WriteByte(0); //Pixel aspect ratio - assume 1:1
        }

        /// <summary>
        /// Writes Netscape application extension to define the repeat count.
        /// </summary>
        private void WriteNetscapeExt()
        {
            _fs.WriteByte(0x21); //Extension introducer
            _fs.WriteByte(0xff); //App extension label
            _fs.WriteByte(11); //Block size
            WriteString("NETSCAPE" + "2.0"); //App id + auth code
            _fs.WriteByte(3); //Sub-block size
            _fs.WriteByte(1); //Loop sub-block id
            WriteShort(_repeat); //Loop count (extra iterations, 0=repeat forever) //-1 no repeat, 0 = forever, 1=once... n=extra repeat
            _fs.WriteByte(0); //Block terminator
        }

        /// <summary>
        /// Writes color table.
        /// </summary>
        private void WritePalette()
        {
            //Writes the color palette.
            _fs.Write(_colorTab, 0, _colorTab.Length);

            //Calculates the space left.
            var n = (3 * 256) - _colorTab.Length;

            //Fills the rest of palette with zeros (If any space left).
            for (var i = 0; i < n; i++)
            {
                _fs.WriteByte(0);
            }
        }

        /// <summary>
        /// Encodes and writes pixel data.
        /// </summary>
        private void WritePixels(int width, int height)
        {
            var encoder = new LzwEncoder(width, height, _indexedPixels, _colorDepth);
            encoder.Encode(_fs);
        }

        /// <summary>
        /// Writes the comment for the animation.
        /// </summary>
        /// <param name="comment">The Comment to write.</param>
        private void WriteComment(string comment)
        {
            _fs.WriteByte(0x21);
            _fs.WriteByte(0xfe);

            //byte[] lenght = StringToByteArray(comment.Length.ToString("X"));

            //foreach (byte b in lenght)
            //{
            //    fs.WriteByte(b);
            //}

            var bytes = System.Text.Encoding.ASCII.GetBytes(comment);

            _fs.WriteByte((byte)bytes.Length);
            _fs.Write(bytes, 0, bytes.Length);
            _fs.WriteByte(0);
        }

        /// <summary>
        /// Converts a String to a byte Array.
        /// </summary>
        /// <param name="hex">The string to convert</param>
        /// <returns>A byte array corresponding to the string</returns>
        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1) //if odd
            {
                hex = hex.PadLeft(1, '0');
            }

            var numberChars = hex.Length / 2;
            var bytes = new byte[numberChars];

            using (var sr = new StringReader(hex))
            {
                for (var i = 0; i < numberChars; i++)
                    bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Write 16-bit value to output stream, LSB first.
        /// </summary>
        /// <param name="value">The 16-bit value.</param>
        private void WriteShort(int value)
        {
            _fs.WriteByte(Convert.ToByte(value & 0xff));
            _fs.WriteByte(Convert.ToByte((value >> 8) & 0xff));
        }

        /// <summary>
        /// Writes string to output stream.
        /// </summary>
        /// <param name="s">The string to write.</param>
        private void WriteString(string s)
        {
            var chars = s.ToCharArray();

            foreach (var t in chars)
            {
                _fs.WriteByte((byte)t);
            }
        }

        public void Dispose()
        {
            _started = false;

            try
            {
                if (_fs == null)
                    throw new Exception("Tried disposing without starting the encoding");

                WriteComment("Made with ScreenToGif");

                //Gif trailer, end of the gif.
                //fs.WriteByte(0x00);
                _fs.WriteByte(0x3b);

                _fs.Flush();

                if (_closeStream)
                {
                    _fs.Close();
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "End of the gif");
            }

            // reset for subsequent use
            _transIndex = 0;
            _fs = null;
            _image = null;
            _pixels = null;
            _indexedPixels = null;
            _colorTab = null;
            _closeStream = false;
            _firstFrame = true;
        }
    }
}