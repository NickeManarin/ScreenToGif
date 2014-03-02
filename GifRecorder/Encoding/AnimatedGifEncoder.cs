using System;
using System.Drawing;
using System.IO;
using ScreenToGif.Util;

namespace ScreenToGif.Encoding
{
	public class AnimatedGifEncoder : IDisposable
    {
        #region Variables

        /// <summary>
        /// Image width.
        /// </summary>
		protected int width;

        /// <summary>
        /// Image height.
        /// </summary>
		protected int height;

        /// <summary>
        /// Transparent color if given.
        /// </summary>
		protected Color transparent = Color.Empty;

        /// <summary>
        /// Transparent index in color table.
        /// </summary>
		protected int transIndex;

        /// <summary>
        /// The number of interations, default as "no repeat".
        /// </summary>
		protected int repeat = -1;

        /// <summary>
        /// Frame delay.
        /// </summary>
		protected int delay = 0;

        /// <summary>
        /// Flag that tells about the output encoding.
        /// </summary>
		protected bool started = false;

		//	protected BinaryWriter bw;

        /// <summary>
        /// FileStream of the process.
        /// </summary>
		protected FileStream fs;

        /// <summary>
        /// Current frame.
        /// </summary>
		protected Image image;

        /// <summary>
        /// BGR byte array from frame.
        /// </summary>
		protected byte[] pixels;
		protected byte[] indexedPixels; // converted frame indexed to palette
		protected int colorDepth; // number of bit planes
		protected byte[] colorTab; // RGB palette
		protected bool[] usedEntry = new bool[256]; // active palette entries
		protected int palSize = 7; // color table size (bits-1)
		protected int dispose = -1; // disposal code (-1 = use default)
		protected bool closeStream = false; // close stream when finished
		protected bool firstFrame = true;
		protected bool sizeSet = false; // if false, get size from first frame
		protected int sample = 10; // default sample interval for quantizer

        #endregion

        /// <summary>
        /// Sets the delay time between each frame, or changes it
        /// for subsequent frames (applies to last frame added).
		/// </summary>
        /// <param name="ms">Delay time in milliseconds</param>
		public void SetDelay(int ms) 
		{
			delay = ( int ) Math.Round(ms / 10.0f, MidpointRounding.AwayFromZero);
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
				dispose = code;
			}
		}
	

		/// <summary>
        /// Sets the number of times the set of GIF frames
        /// should be played.  Default is 1; 0 means play
        /// indefinitely.  Must be invoked before the first
        /// image is added.
		/// </summary>
        /// <param name="iter">Number of iterations.</param>
		public void SetRepeat(int iter) 
		{
			if (iter >= 0) 
			{
				repeat = iter;
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
			transparent = c;
		}
	
		/// <summary>
        /// Adds next GIF frame.  The frame is not written immediately, but is
        /// actually deferred until the next frame is received so that timing
        /// data can be inserted.  Invoking <code>finish()</code> flushes all
        /// frames.  If <code>setSize</code> was not invoked, the size of the
        /// first image is used for all subsequent frames.
		/// </summary>
        /// <param name="im">BufferedImage containing frame to write.</param>
        /// <returns>True if successful.</returns>
		public bool AddFrame(Image im) 
		{
			if ((im == null) || !started) 
			{
				return false;
			}
			bool ok = true;
			try 
			{
				if (!sizeSet) 
				{
					// use first frame's size
					SetSize(im.Width, im.Height);
				}
				image = im;
				GetImagePixels(); // convert to correct format if necessary
				AnalyzePixels(); // build color table & map pixels
				if (firstFrame) 
				{
					WriteLSD(); // logical screen descriptior
					WritePalette(); // global color table
					if (repeat >= 0) 
					{
						// use NS app extension to indicate reps
						WriteNetscapeExt();
					}
				}
				WriteGraphicCtrlExt(); // write graphic control extension
				WriteImageDesc(); // image descriptor
				if (!firstFrame) 
				{
					WritePalette(); // local color table
				}
				WritePixels(); // encode and write pixel data
				firstFrame = false;
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
			if (!started) return false;
			bool ok = true;
			started = false;
			try 
			{
				fs.WriteByte( 0x3b ); // gif trailer
				fs.Flush();
				if (closeStream) 
				{
					fs.Close();
				}
			} 
			catch (IOException e) 
			{
				ok = false;
			}

			// reset for subsequent use
			transIndex = 0;
			fs = null;
			image = null;
			pixels = null;
			indexedPixels = null;
			colorTab = null;
			closeStream = false;
			firstFrame = true;

			return ok;
		}
	
		/// <summary>
        /// Sets frame rate in frames per second. Equivalent to <code>setDelay(1000/fps)</code>.
		/// </summary>
        /// <param name="fps">Frame rate (frames per second)</param>
		public void SetFrameRate(float fps) 
		{
			if (fps != 0f) 
			{
                delay = (int)Math.Round(100f / fps, MidpointRounding.AwayFromZero);
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
			sample = quality;
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
			if (started && !firstFrame) return;
			width = w;
			height = h;
			if (width < 1) width = 320;
			if (height < 1) height = 240;
			sizeSet = true;
		}
	
		/// <summary>
        /// Initiates GIF file creation on the given stream. The stream is not closed automatically.
		/// </summary>
        /// <param name="os">OutputStream on which GIF images are written.</param>
        /// <returns>False if initial write failed.</returns>
		public bool Start(FileStream os) 
		{
			if (os == null) return false;
			bool ok = true;
			closeStream = false;
			fs = os;
			try 
			{
				WriteString("GIF89a"); // header
			} 
			catch (IOException e) 
			{
				ok = false;
			}
			return started = ok;
		}
	
		/// <summary>
        /// Initiates writing of a GIF file with the specified name.
		/// </summary>
        /// <param name="file">String containing output file name.</param>
        /// <returns>False if open or initial write failed.</returns>
		public bool Start(String file) 
		{
			bool ok = true;
			try 
			{
				//			bw = new BinaryWriter( new FileStream( file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None ) );
				fs = new FileStream( file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None );
				ok = Start(fs);
				closeStream = true;
			} 
			catch (IOException e) 
			{
				ok = false;
			}
			return started = ok;
		}
	
		/// <summary>
        /// Analyzes image colors and creates color map.
		/// </summary>
		protected void AnalyzePixels() 
		{
			int len = pixels.Length;
			int nPix = len / 3;
			indexedPixels = new byte[nPix];
			NeuQuant nq = new NeuQuant(pixels, len, sample);
			// initialize quantizer
			colorTab = nq.Process(); // create reduced palette
			// convert map from BGR to RGB
            //for (int i = 0; i < colorTab.Length; i += 3)
            //{
            //    byte temp = colorTab[i];
            //    colorTab[i] = colorTab[i + 2];
            //    colorTab[i + 2] = temp;
            //    usedEntry[i / 3] = false;
            //}
			// map image pixels to new palette
			int k = 0;
            usedEntry = new bool[256];//here is the fix. from the internet, codeproject
            for (int i = 0; i < nPix; i++)
            {
                int index =
                    nq.Map(pixels[k++] & 0xff,
                    pixels[k++] & 0xff,
                    pixels[k++] & 0xff);
                usedEntry[index] = true;
                indexedPixels[i] = (byte)index;
            }
			pixels = null;
			colorDepth = 8;
			palSize = 7;
			// get closest match to transparent color if specified
			if (transparent != Color.Empty ) 
			{
				transIndex = FindClosest(transparent);
			}
		}
	
		/// <summary>
        /// Returns index of palette color closest to given color.
		/// </summary>
		/// <param name="c">The color to search for in the pallette.</param>
		/// <returns>The index of the pallete color.</returns>
		protected int FindClosest(Color c) 
		{
			if (colorTab == null) return -1;
			int r = c.R;
			int g = c.G;
			int b = c.B;
			int minpos = 0;
			int dmin = 256 * 256 * 256;
			int len = colorTab.Length;
			for (int i = 0; i < len;) 
			{
				int dr = r - (colorTab[i++] & 0xff);
				int dg = g - (colorTab[i++] & 0xff);
				int db = b - (colorTab[i] & 0xff);
				int d = dr * dr + dg * dg + db * db;
				int index = i / 3;
				if (usedEntry[index] && (d < dmin)) 
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
		protected void GetImagePixels() 
		{
			int w = image.Width;
			int h = image.Height;
			//		int type = image.GetType().;
			if ((w != width)
				|| (h != height)
				) 
			{
				// create new image with right size/format
				Image temp =
					new Bitmap(width, height );
				Graphics g = Graphics.FromImage( temp );
				g.DrawImage(image, 0, 0);
				image = temp;
				g.Dispose();
			}

			/*
                Performance upgrade, now encoding takes half of the time, due to Marshall calls.
			*/

			pixels = new Byte [ 3 * image.Width * image.Height ];
			int count = 0;
			Bitmap tempBitmap = new Bitmap( image );

            PixelUtil pixelUtil = new PixelUtil(tempBitmap);
            pixelUtil.LockBits();

            //Benchmark.Start();

            for (int th = 0; th < image.Height; th++)
			{
				for (int tw = 0; tw < image.Width; tw++)
				{
                    Color color = pixelUtil.GetPixel(tw, th);
					pixels[count] = color.R;
					count++;
					pixels[count] = color.G;
					count++;
					pixels[count] = color.B;
					count++;
				}
			}

            pixelUtil.UnlockBits();

            //Benchmark.End();
            //Console.WriteLine(Benchmark.GetSeconds());

			//pixels = ((DataBufferByte) image.getRaster().getDataBuffer()).getData();
		}
	
		/// <summary>
        /// Writes Graphic Control Extension.
		/// </summary>
		protected void WriteGraphicCtrlExt() 
		{
			fs.WriteByte(0x21); // extension introducer
			fs.WriteByte(0xf9); // GCE label
			fs.WriteByte(4); // data block size
			int transp, disp;
			if (transparent == Color.Empty ) 
			{
				transp = 0;
				disp = 0; // dispose = no action
			} 
			else 
			{
				transp = 1;
				disp = 2; // force clear if using transparent color
			}
			if (dispose >= 0) 
			{
				disp = dispose & 7; // user override
			}
			disp <<= 2;

			// packed fields
			fs.WriteByte( Convert.ToByte( 0 | // 1:3 reserved
				disp | // 4:6 disposal
				0 | // 7   user input - 0 = none
				transp )); // 8   transparency flag

			WriteShort(delay); // delay x 1/100 sec
			fs.WriteByte( Convert.ToByte( transIndex)); // transparent color index
			fs.WriteByte(0); // block terminator
		}
	
		/// <summary>
        /// Writes Image Descriptor.
		/// </summary>
		protected void WriteImageDesc()
		{
			fs.WriteByte(0x2c); // image separator
			WriteShort(0); // image position x,y = 0,0
			WriteShort(0);
			WriteShort(width); // image size
			WriteShort(height);
			// packed fields
			if (firstFrame) 
			{
				// no LCT  - GCT is used for first (or only) frame
				fs.WriteByte(0);
			} 
			else 
			{
				// specify normal LCT
				fs.WriteByte( Convert.ToByte( 0x80 | // 1 local color table  1=yes
					0 | // 2 interlace - 0=no
					0 | // 3 sorted - 0=no
					0 | // 4-5 reserved
					palSize ) ); // 6-8 size of color table
			}
		}
	
		/// <summary>
        /// Writes Logical Screen Descriptor
		/// </summary>
		protected void WriteLSD()  
		{
			// logical screen size
			WriteShort(width);
			WriteShort(height);
			// packed fields
			fs.WriteByte( Convert.ToByte (0x80 | // 1   : global color table flag = 1 (gct used)
				0x70 | // 2-4 : color resolution = 7
				0x00 | // 5   : gct sort flag = 0
				palSize) ); // 6-8 : gct size

			fs.WriteByte(0); // background color index
			fs.WriteByte(0); // pixel aspect ratio - assume 1:1
		}
	        
		/// <summary>
        /// Writes Netscape application extension to define the repeat count.
		/// </summary>
		protected void WriteNetscapeExt()
		{
			fs.WriteByte(0x21); // extension introducer
			fs.WriteByte(0xff); // app extension label
			fs.WriteByte(11); // block size
			WriteString("NETSCAPE" + "2.0"); // app id + auth code
			fs.WriteByte(3); // sub-block size
			fs.WriteByte(1); // loop sub-block id
			WriteShort(repeat); // loop count (extra iterations, 0=repeat forever) //-1 no repeat, 0 = forever, 1=once... n=extra repeat
			fs.WriteByte(0); // block terminator
		}
	
		/// <summary>
        /// Writes color table.
		/// </summary>
		protected void WritePalette()
		{
			fs.Write(colorTab, 0, colorTab.Length);
			int n = (3 * 256) - colorTab.Length;
			for (int i = 0; i < n; i++) 
			{
				fs.WriteByte(0);
			}
		}
	
		/// <summary>
        /// Encodes and writes pixel data.
		/// </summary>
		protected void WritePixels()
		{
			var encoder =
				new LZWEncoder(width, height, indexedPixels, colorDepth);
			encoder.Encode(fs);
		}
	
		/// <summary>
        /// Write 16-bit value to output stream, LSB first.
		/// </summary>
		/// <param name="value">The 16-bit value.</param>
		protected void WriteShort(int value)
		{
			fs.WriteByte( Convert.ToByte( value & 0xff));
			fs.WriteByte( Convert.ToByte( (value >> 8) & 0xff ));
		}
	
		/// <summary>
        /// Writes string to output stream.
		/// </summary>
		/// <param name="s">The string to write.</param>
		protected void WriteString(String s)
		{
		    char[] chars = s.ToCharArray();
		    foreach (char t in chars)
		    {
		        fs.WriteByte((byte) t);
		    }
		}

	    public void Dispose()
        {
            started = false;
            try
            {
                fs.WriteByte(0x3b); // gif trailer
                fs.Flush();
                if (closeStream)
                {
                    fs.Close();
                }
            }
            catch (IOException e)
            {
                
            }

            // reset for subsequent use
            transIndex = 0;
            fs = null;
            image = null;
            pixels = null;
            indexedPixels = null;
            colorTab = null;
            closeStream = false;
            firstFrame = true;

        }
    }

}