#region Java

#endregion

using ScreenToGif.FileWriters.GifWriter;
/**
* Class GifDecoder - Decodes a GIF file into one or more frames.
* <br><pre>
* Example:
*    GifDecoder d = new GifDecoder();
*    d.read("sample.gif");
*    int n = d.getFrameCount();
*    for (int i = 0; i < n; i++) {
*       BufferedImage frame = d.getFrame(i);  // frame i
*       int t = d.getDelay(i);  // display duration of frame in milliseconds
*       // do something with frame
*    }
* </pre>
* No copyright asserted on the source code of this class.  May be used for
* any purpose, however, refer to the Unisys LZW patent for any additional
* restrictions.  Please forward any corrections to kweiner@fmsware.com.
*
* @author Kevin Weiner, FM Software; LZW decoder adapted from John Cristy's ImageMagick.
* @version 1.03 November 2003
*
*/
using System;
using System.Collections;
using System.Drawing;
using System.IO;

namespace ScreenToGif.ImageUtil
{
    /// <summary>
    /// Auxiliar class that holds the frame information.
    /// </summary>
    public class GifFrame
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="im">The frame image.</param>
        /// <param name="del">The frame delay.</param>
        public GifFrame(Image im, int del)
        {
            Image = im;
            Delay = del;
        }

        public Image Image { get; set; }
        public int Delay { get; set; }
    }

    /// <summary>
    /// Gif Reader Class
    /// </summary>
    public class GifDecoder
    {
        #region Constants

        /// <summary>
        /// File read status: No errors.
        /// </summary>
        private const int StatusOk = 0;

        /// <summary>
        /// File read status: Error decoding file (may be partially decoded)
        /// </summary>
        public const int StatusFormatError = 1;

        /// <summary>
        /// File read status: Unable to open source.
        /// </summary>
        public const int StatusOpenError = 2;

        #endregion

        #region Variables

        /// <summary>
        /// The inner Stream.
        /// </summary>
        private Stream _stream;

        private int _status;

        /// <summary>
        /// Full image width.
        /// </summary>
        private int _width;

        /// <summary>
        /// Full image height.
        /// </summary>
        private int _height;

        /// <summary>
        /// Global color table used.
        /// </summary>
        private bool _gctFlag;

        /// <summary>
        /// Size of global color table.
        /// </summary>
        private int _gctSize;

        /// <summary>
        /// Iterations; 0 = repeat forever.
        /// </summary>
        private int loopCount = 1;

        /// <summary>
        /// Global color table.
        /// </summary>
        private int[] _globalColorTable;

        /// <summary>
        /// Local color table.
        /// </summary>
        private int[] _localColorTable;

        /// <summary>
        /// Active color table.
        /// </summary>
        private int[] _activeColorTable;

        /// <summary>
        /// Background color index.
        /// </summary>
        private int _backgroundIndex;

        /// <summary>
        /// Background color.
        /// </summary>
        private int _backgroundColor;

        /// <summary>
        /// Previous background color.
        /// </summary>
        private int _lastBackgroundColor;

        /// <summary>
        /// Pixel aspect ratio.
        /// </summary>
        private int _pixelAspect;

        /// <summary>
        /// Local color table flag.
        /// </summary>
        private bool _lctFlag;

        /// <summary>
        /// Interlace flag.
        /// </summary>
        private bool _interlace;

        /// <summary>
        /// Local color table size.
        /// </summary>
        private int _lctSize; 

        /// <summary>
        /// Current image rectangle
        /// </summary>
        private int _internalX, _internalY, _internalWidth, _internalHeight;
        
        /// <summary>
        /// Last image rectangle.
        /// </summary>
        private Rectangle _lastRect;
        
        /// <summary>
        /// Current frame.
        /// </summary>
        private Image _image;
        
        private Bitmap _bitmap;

        /// <summary>
        /// Previous frame.
        /// </summary>
        private Image _lastImage;

        /// <summary>
        /// Current data block.
        /// </summary>
        private readonly byte[] _block = new byte[256];

        /// <summary>
        /// Block size.
        /// </summary>
        private int _blockSize = 0;

        /// <summary>
        /// Last graphic control extension info.
        /// </summary>
        private int _dispose = 0;

        /// <summary>
        /// 0 = No action; 
        /// 1 = Leave in place; 
        /// 2 = Restore to background; 
        /// 3 = Restore to previous.
        /// </summary>
        private int _lastDispose = 0;

        /// <summary>
        /// Use transparent color.
        /// </summary>
        private bool _transparency = false;

        /// <summary>
        /// Delay in milliseconds.
        /// </summary>
        private int _delay = 0;

        /// <summary>
        /// Transparent color index.
        /// </summary>
        private int _transIndex;

        /// <summary>
        /// Max decoder pixel stack size.
        /// </summary>
        private const int MaxStackSize = 4096;

        //LZW decoder working arrays
        private short[] _prefix;
        private byte[] _suffix;
        private byte[] _pixelStack;
        private byte[] _pixels;

        /// <summary>
        /// Frames read from current file.
        /// </summary>
        private ArrayList _frames;

        private int _frameCount;

        #endregion

        /// <summary>
        /// Gets display duration for specified frame.
        /// </summary>
        /// <param name="index">Int index of frame.</param>
        /// <returns>Delay in milliseconds.</returns>
        public int GetDelay(int index)
        {
            _delay = -1;

            if ((index >= 0) && (index < _frameCount))
            {
                _delay = ((GifFrame)_frames[index]).Delay;
            }

            return _delay;
        }

        /// <summary>
        ///Gets the number of frames read from file.
        /// </summary>
        /// <returns>The frame count.</returns>
        public int GetFrameCount()
        {
            return _frameCount;
        }

        /// <summary>
        ///Gets the first (or only) image read.
        /// </summary>
        /// <returns>BufferedImage containing first frame, or null if none.</returns>
        public Image GetImage()
        {
            return GetFrame(0);
        }

        /// <summary>
        /// Gets the "Netscape" iteration count, if any.
        /// A count of 0 means repeat indefinitiely.
        /// </summary>
        /// <returns>Iteration count if one was specified, else 1.</returns>
        public int GetLoopCount()
        {
            return loopCount;
        }

        /// <summary>
        /// Creates new frame image from current data (and previous frames as specified by their disposition codes).
        /// </summary>
        /// <param name="bitmap">The bitmap to get the pixels.</param>
        /// <returns>The pixel array.</returns>
        private int[] GetPixels(Bitmap bitmap)
        {
            int[] pixels = new int[3 * _image.Width * _image.Height];
            int count = 0;

            var image = new PixelUtil(bitmap);
            image.LockBits();

            for (int th = 0; th < _image.Height; th++)
            {
                for (int tw = 0; tw < _image.Width; tw++)
                {
                    Color color = image.GetPixel(tw, th);
                    pixels[count] = color.R;
                    count++;
                    pixels[count] = color.G;
                    count++;
                    pixels[count] = color.B;
                    count++;
                }
            }

            image.UnlockBits();
            
            return pixels;
        }

        private void SetPixels(int[] pixels)
        {
            var image = new PixelUtil(_bitmap);
            image.LockBits();

            int count = 0;
            for (int th = 0; th < _image.Height; th++)
            {
                for (int tw = 0; tw < _image.Width; tw++)
                {
                    Color color = Color.FromArgb(pixels[count++]);
                    image.SetPixel(tw, th, color);
                }
            }

            image.UnlockBits();
        }

        private void SetPixels()
        {
            //Expose destination image's pixels as int array
            //int[] dest = ((int) image.getRaster().getDataBuffer()).getData();
            int[] dest = GetPixels(_bitmap);

            //Fill in starting image contents based on last image's dispose code.
            if (_lastDispose > 0)
            {
                if (_lastDispose == 3 || _lastDispose == 1)
                {
                    //Use image before last
                    int n = _frameCount - 2;

                    _lastImage = n > 0 ? GetFrame(n - 1) : null;
                }

                if (_lastImage != null)
                {
                    //int[] prev = ((DataBufferInt) lastImage.getRaster().getDataBuffer()).getData();
                    int[] prev = GetPixels(new Bitmap(_lastImage));
                    Array.Copy(prev, 0, dest, 0, _width * _height);
                    //Copy pixels

                    //if (_lastDispose == 2)
                    //{
                    //    //Fill last image rect area with background color
                    //    using (var g = Graphics.FromImage(_image))
                    //    {
                    //        Color c = Color.Empty;

                    //        c = _transparency ? 
                    //            Color.FromArgb(0, 0, 0, 0) : 
                    //            Color.FromArgb(_lastBackgroundColor);

                    //        Brush brush = new SolidBrush(c);
                    //        g.FillRectangle(brush, _lastRect);
                    //        brush.Dispose();
                    //    }
                    //}
                }
            }

            //Copy each source line to the appropriate place in the destination.
            int pass = 1;
            int inc = 8;
            int iline = 0;

            for (int i = 0; i < _internalHeight; i++)
            {
                int line = i;

                if (_interlace)
                {
                    if (iline >= _internalHeight)
                    {
                        pass++;
                        switch (pass)
                        {
                            case 2:
                                iline = 4;
                                break;
                            case 3:
                                iline = 2;
                                inc = 4;
                                break;
                            case 4:
                                iline = 1;
                                inc = 2;
                                break;
                        }
                    }

                    line = iline;
                    iline += inc;
                }

                line += _internalY;

                if (line >= _height) continue;

                int k = line * _width;
                int dx = k + _internalX; // start of line in dest
                int dlim = dx + _internalWidth; // end of dest line

                if ((k + _width) < dlim)
                {
                    dlim = k + _width; // past dest edge
                }

                int sx = i * _internalWidth; // start of line in source

                while (dx < dlim)
                {
                    // map color and insert in destination
                    int index = ((int)_pixels[sx++]) & 0xff;
                    int c = _activeColorTable[index];

                    if (c != 0)
                    {
                        dest[dx] = c;
                    }

                    dx++;
                }
            }

            SetPixels(dest);
        }

        /// <summary>
        /// Gets the image contents of frame n.
        /// </summary>
        /// <param name="n"></param>
        /// <returns>BufferedImage representation of frame, or null if n is invalid.</returns>
        public Image GetFrame(int n)
        {
            Image im = null;

            if ((n >= 0) && (n < _frameCount))
            {
                im = ((GifFrame)_frames[n]).Image;
            }

            return im;
        }

        /// <summary>
        /// Gets image size.
        /// </summary>
        /// <returns>GIF image dimensions.</returns>
        public Size GetFrameSize()
        {
            return new Size(_width, _height);
        }

        /// <summary>
        /// Reads GIF image from stream.
        /// </summary>
        /// <param name="inStream">BufferedInputStream containing GIF file.</param>
        /// <returns>read status code (0 = no errors)</returns>
        public int Read(Stream inStream)
        {
            Init();

            if (inStream == null)
            {
                return StatusOpenError;
            }
            
            _stream = inStream;
            ReadHeader();

            if (!Error())
            {
                ReadContents();

                if (_frameCount < 0)
                {
                    _status = StatusFormatError;
                }
            }

            inStream.Close();

            return _status;
        }

        /// <summary>
        /// Reads GIF file from specified file/URL source (URL assumed if name contains ":/" or "file:")
        /// </summary>
        /// <param name="name">String containing source.</param>
        /// <returns>Status code (0 = no errors)</returns>
        public int Read(String name)
        {
            _status = StatusOk;

            try
            {
                name = name.Trim().ToLower();
                _status = Read(new FileInfo(name).OpenRead());
            }
            catch (IOException e)
            {
                _status = StatusOpenError;
            }

            return _status;
        }

        /// <summary>
        /// Decodes LZW image data into pixel array.
        /// Adapted from John Cristy's ImageMagick.
        /// </summary>
        protected void DecodeImageData()
        {
            int NullCode = -1;
            int npix = _internalWidth * _internalHeight;
            int available,
                clear,
                code_mask,
                code_size,
                end_of_information,
                in_code,
                old_code,
                bits,
                code,
                count,
                i,
                datum,
                data_size,
                first,
                top,
                bi,
                pi;

            if ((_pixels == null) || (_pixels.Length < npix))
            {
                _pixels = new byte[npix]; // allocate new pixel array
            }

            if (_prefix == null) _prefix = new short[MaxStackSize];
            if (_suffix == null) _suffix = new byte[MaxStackSize];
            if (_pixelStack == null) _pixelStack = new byte[MaxStackSize + 1];

            //Initialize GIF data stream decoder.

            data_size = Read();
            clear = 1 << data_size;
            end_of_information = clear + 1;
            available = clear + 2;
            old_code = NullCode;
            code_size = data_size + 1;
            code_mask = (1 << code_size) - 1;
            for (code = 0; code < clear; code++)
            {
                _prefix[code] = 0;
                _suffix[code] = (byte)code;
            }

            //Decode GIF pixel stream.

            datum = bits = count = first = top = pi = bi = 0;

            for (i = 0; i < npix; )
            {
                if (top == 0)
                {
                    if (bits < code_size)
                    {
                        //Load bytes until there are enough bits for a code.
                        if (count == 0)
                        {
                            //Read a new data block.
                            count = ReadBlock();
                            if (count <= 0)
                                break;
                            bi = 0;
                        }

                        datum += (((int)_block[bi]) & 0xff) << bits;
                        bits += 8;
                        bi++;
                        count--;
                        continue;
                    }

                    //Get the next code.

                    code = datum & code_mask;
                    datum >>= code_size;
                    bits -= code_size;

                    //Interpret the code

                    if ((code > available) || (code == end_of_information))
                        break;
                    if (code == clear)
                    {
                        //Reset decoder.
                        code_size = data_size + 1;
                        code_mask = (1 << code_size) - 1;
                        available = clear + 2;
                        old_code = NullCode;
                        continue;
                    }
                    if (old_code == NullCode)
                    {
                        _pixelStack[top++] = _suffix[code];
                        old_code = code;
                        first = code;
                        continue;
                    }
                    in_code = code;
                    if (code == available)
                    {
                        _pixelStack[top++] = (byte)first;
                        code = old_code;
                    }

                    while (code > clear)
                    {
                        _pixelStack[top++] = _suffix[code];
                        code = _prefix[code];
                    }

                    first = ((int)_suffix[code]) & 0xff;

                    //Add a new string to the string table,

                    if (available >= MaxStackSize)
                        break;

                    _pixelStack[top++] = (byte)first;
                    _prefix[available] = (short)old_code;
                    _suffix[available] = (byte)first;
                    available++;

                    if (((available & code_mask) == 0) && (available < MaxStackSize))
                    {
                        code_size++;
                        code_mask += available;
                    }

                    old_code = in_code;
                }

                //Pop a pixel off the pixel stack.

                top--;
                _pixels[pi++] = _pixelStack[top];
                i++;
            }

            for (i = pi; i < npix; i++)
            {
                _pixels[i] = 0; //Clear missing pixels
            }
        }

        /// <summary>
        /// The read state.
        /// </summary>
        /// <returns>True if an error was encountered during reading/decoding</returns>
        protected bool Error()
        {
            return _status != StatusOk;
        }

        /// <summary>
        /// Initializes or re-initializes reader.
        /// </summary>
        protected void Init()
        {
            _status = StatusOk;
            _frameCount = 0;
            _frames = new ArrayList();
            _globalColorTable = null;
            _localColorTable = null;
        }

        /// <summary>
        /// Reads a single byte from the input stream.
        /// </summary>
        /// <returns>The single byte.</returns>
        protected int Read()
        {
            int curByte = 0;

            try
            {
                curByte = _stream.ReadByte();
            }
            catch (IOException e)
            {
                _status = StatusFormatError;
            }

            return curByte;
        }

        /// <summary>
        /// Reads next variable length block from input.
        /// </summary>
        /// <returns>Number of bytes stored in "buffer".</returns>
        protected int ReadBlock()
        {
            _blockSize = Read();
            int n = 0;

            if (_blockSize > 0)
            {
                try
                {
                    while (n < _blockSize)
                    {
                        var count = _stream.Read(_block, n, _blockSize - n);

                        if (count == -1)
                            break;
                        n += count;
                    }
                }
                catch (IOException e)
                {
                    //Ignore?
                }

                if (n < _blockSize)
                {
                    _status = StatusFormatError;
                }
            }

            return n;
        }

        /// <summary>
        /// Reads color table as 256 RGB integer values
        /// </summary>
        /// <param name="numberColors">Int number of colors to read.</param>
        /// <returns>Int array containing 256 colors (packed ARGB with full alpha).</returns>
        private int[] ReadColorTable(int numberColors)
        {
            int nbytes = 3 * numberColors;
            int[] tab = null;
            byte[] c = new byte[nbytes];
            int n = 0;

            try
            {
                n = _stream.Read(c, 0, c.Length);
            }
            catch (IOException e)
            {
            }

            if (n < nbytes)
            {
                _status = StatusFormatError;
            }
            else
            {
                tab = new int[256]; //Max size to avoid bounds checks
                int i = 0;
                int j = 0;

                while (i < numberColors)
                {
                    int r = ((int)c[j++]) & 0xff;
                    int g = ((int)c[j++]) & 0xff;
                    int b = ((int)c[j++]) & 0xff;
                    tab[i++] = (int)(0xff000000 | (r << 16) | (g << 8) | b);
                }
            }

            return tab;
        }

        /// <summary>
        /// Main file parser. Reads GIF content blocks.
        /// </summary>
        protected void ReadContents()
        {
            //Read GIF file content blocks.
            bool done = false;
            
            while (!(done || Error()))
            {
                int code = Read();
                
                switch (code)
                {
                    case 0x2C: //Image separator.
                        ReadImage();
                        break;

                    case 0x21: //Extension.
                        code = Read();

                        switch (code)
                        {
                            case 0xf9: //Graphics Control Extension.
                                ReadGraphicControlExt();
                                break;

                            case 0xff: //Application Extension.
                                ReadBlock();
                                String app = "";

                                for (int i = 0; i < 11; i++)
                                {
                                    app += (char)_block[i];
                                }

                                if (app.Equals("NETSCAPE2.0"))
                                {
                                    ReadNetscapeExt();
                                }
                                else
                                    Skip(); //Don't care.
                                break;

                            default: //Uninteresting extension.
                                Skip();
                                break;
                        }

                        break;

                    case 0x3b: //Terminator.
                        done = true;
                        break;

                    case 0x00: //Bad byte, but keep going and see what happens.
                        break;

                    default:
                        _status = StatusFormatError;
                        break;
                }
            }
        }

        /// <summary>
        /// Reads Graphics Control Extension values.
        /// </summary>
        protected void ReadGraphicControlExt()
        {
            Read(); //Block size.
            
            int packed = Read(); //Packed fields.
            _dispose = (packed & 0x1c) >> 2; //Disposal method.
            
            if (_dispose == 0)
            {
                _dispose = 1; //Elect to keep old image if discretionary.
            }
            
            _transparency = (packed & 1) != 0;
            _delay = ReadShort() * 10; //Delay in milliseconds.
            _transIndex = Read(); //Transparent color index.

            Read(); //Block terminator.
        }

        /// <summary>
        /// Reads GIF file header information.
        /// </summary>
        protected void ReadHeader()
        {
            String id = "";

            for (int i = 0; i < 6; i++)
            {
                id += (char)Read();
            }

            if (!id.StartsWith("GIF"))
            {
                _status = StatusFormatError;
                return;
            }

            ReadLsd();

            if (_gctFlag && !Error())
            {
                _globalColorTable = ReadColorTable(_gctSize);
                _backgroundColor = _globalColorTable[_backgroundIndex];
            }
        }

        /// <summary>
        /// Reads next frame image.
        /// </summary>
        protected void ReadImage()
        {
            _internalX = ReadShort(); //(Sub)image position & size.
            _internalY = ReadShort();
            _internalWidth = ReadShort();
            _internalHeight = ReadShort();

            int packed = Read();
            _lctFlag = (packed & 0x80) != 0; // 1 - Local color table flag.
            _interlace = (packed & 0x40) != 0; // 2 - Interlace flag.
            // 3 - Sort flag
            // 4-5 - Reserved
            _lctSize = 2 << (packed & 7); // 6-8 - Local color table size.

            if (_lctFlag)
            {
                _localColorTable = ReadColorTable(_lctSize); //Read table.
                _activeColorTable = _localColorTable; //Make local table active.
            }
            else
            {
                _activeColorTable = _globalColorTable; //Make global table active.
                
                if (_backgroundIndex == _transIndex)
                    _backgroundColor = 0;
            }

            int save = 0;

            if (_transparency)
            {
                save = _activeColorTable[_transIndex];
                _activeColorTable[_transIndex] = 0; //Set transparent color if specified.
            }

            if (_activeColorTable == null)
            {
                _status = StatusFormatError; //No color table defined.
            }

            if (Error()) return;

            DecodeImageData(); //Decode pixel data.
            Skip();

            if (Error()) return;

            _frameCount++;

            //Create new image to receive frame data.
            //image = new BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB_PRE);

            _bitmap = new Bitmap(_width, _height);
            _image = _bitmap;
            SetPixels(); //Transfer pixel data to image.

            _frames.Add(new GifFrame(_bitmap, _delay)); //Add image to frame list. Error Its rounding up 16 -> 20, 66 -> 70

            if (_transparency)
            {
                _activeColorTable[_transIndex] = save;
            }

            ResetFrame();
        }

        /// <summary>
        /// Reads Logical Screen Descriptor
        /// </summary>
        protected void ReadLsd()
        {
            //Logical screen size.
            _width = ReadShort();
            _height = ReadShort();

            //Packed fields.
            int packed = Read();
            _gctFlag = (packed & 0x80) != 0; // 1 : Global color table flag.
            // 2-4 : Color resolution.
            // 5 : GCT sort flag.
            _gctSize = 2 << (packed & 7); // 6-8 : GCT size.

            _backgroundIndex = Read(); // Background color index.
            _pixelAspect = Read(); // Pixel aspect ratio.
        }

        /// <summary>
        /// Reads Netscape extenstion to obtain iteration count.
        /// </summary>
        protected void ReadNetscapeExt()
        {
            do
            {
                ReadBlock();

                if (_block[0] == 1)
                {
                    //Loop count sub-block.
                    int b1 = ((int)_block[1]) & 0xff;
                    int b2 = ((int)_block[2]) & 0xff;
                    loopCount = (b2 << 8) | b1;
                }
            } while ((_blockSize > 0) && !Error());
        }

        /// <summary>
        /// Reads next 16-bit value, LSB first.
        /// </summary>
        /// <returns></returns>
        protected int ReadShort()
        {
            //Read 16-bit value, LSB first
            return Read() | (Read() << 8);
        }

        /// <summary>
        /// Resets frame state for reading next image.
        /// </summary>
        protected void ResetFrame()
        {
            _lastDispose = _dispose;
            _lastRect = new Rectangle(_internalX, _internalY, _internalWidth, _internalHeight);
            _lastImage = _image;
            _lastBackgroundColor = _backgroundColor;
            //dispose = 0;

            _transparency = false;
            _delay = 0;
            _localColorTable = null;
        }

        /// <summary>
        /// Skips variable length blocks up to and including next zero length block.
        /// </summary>
        protected void Skip()
        {
            do
            {
                ReadBlock();
            } while ((_blockSize > 0) && !Error());
        }
    }
}
