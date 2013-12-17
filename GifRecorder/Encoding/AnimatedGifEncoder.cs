using System;
using System.Drawing;
using System.IO;

#region .NET Disclaimer/Info
//===============================================================================
//
// gOODiDEA, uland.com
//===============================================================================
//
// $Header :		$  
// $Author :		$
// $Date   :		$
// $Revision:		$
// $History:		$  
//  
//===============================================================================
#endregion 

#region Java
/**
 * Class AnimatedGifEncoder - Encodes a GIF file consisting of one or
 * more frames.
 * <pre>
 * Example:
 *    AnimatedGifEncoder e = new AnimatedGifEncoder();
 *    e.start(outputFileName);
 *    e.setDelay(1000);   // 1 frame per sec
 *    e.addFrame(image1);
 *    e.addFrame(image2);
 *    e.finish();
 * </pre>
 * No copyright asserted on the source code of this class.  May be used
 * for any purpose, however, refer to the Unisys LZW patent for restrictions
 * on use of the associated LZWEncoder class.  Please forward any corrections
 * to kweiner@fmsware.com.
 *
 * @author Kevin Weiner, FM Software
 * @version 1.03 November 2003
 *
 */
#endregion


namespace ScreenToGif.Encoding
{
	public class AnimatedGifEncoder : IDisposable
	{
		protected int width; // image size
		protected int height;
		protected Color transparent = Color.Empty; // transparent color if given
		protected int transIndex; // transparent index in color table
		protected int repeat = -1; // no repeat
		protected int delay = 0; // frame delay (hundredths)
		protected bool started = false; // ready to output frames
		//	protected BinaryWriter bw;
		protected FileStream fs;

		protected Image image; // current frame
		protected byte[] pixels; // BGR byte array from frame
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

		/**
		 * Sets the delay time between each frame, or changes it
		 * for subsequent frames (applies to last frame added).
		 *
		 * @param ms int delay time in milliseconds
		 */
		public void SetDelay(int ms) 
		{
			delay = ( int ) Math.Round(ms / 10.0f, MidpointRounding.AwayFromZero);
		}
	
		/**
		 * Sets the GIF frame disposal code for the last added frame
		 * and any subsequent frames.  Default is 0 if no transparent
		 * color has been set, otherwise 2.
		 * @param code int disposal code.
		 */
		public void SetDispose(int code) 
		{
			if (code >= 0) 
			{
				dispose = code;
			}
		}
	
		/**
		 * Sets the number of times the set of GIF frames
		 * should be played.  Default is 1; 0 means play
		 * indefinitely.  Must be invoked before the first
		 * image is added.
		 *
		 * @param iter int number of iterations.
		 * @return
		 */
		public void SetRepeat(int iter) 
		{
			if (iter >= 0) 
			{
				repeat = iter;
			}
		}
	
		/**
		 * Sets the transparent color for the last added frame
		 * and any subsequent frames.
		 * Since all colors are subject to modification
		 * in the quantization process, the color in the final
		 * palette for each frame closest to the given color
		 * becomes the transparent color for that frame.
		 * May be set to null to indicate no transparent color.
		 *
		 * @param c Color to be treated as transparent on display.
		 */
		public void SetTransparent(Color c) 
		{
			transparent = c;
		}
	
		/**
		 * Adds next GIF frame.  The frame is not written immediately, but is
		 * actually deferred until the next frame is received so that timing
		 * data can be inserted.  Invoking <code>finish()</code> flushes all
		 * frames.  If <code>setSize</code> was not invoked, the size of the
		 * first image is used for all subsequent frames.
		 *
		 * @param im BufferedImage containing frame to write.
		 * @return true if successful.
		 */
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
	
		/**
		 * Flushes any pending data and closes output file.
		 * If writing to an OutputStream, the stream is not
		 * closed.
		 */
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
	
		/**
		 * Sets frame rate in frames per second.  Equivalent to
		 * <code>setDelay(1000/fps)</code>.
		 *
		 * @param fps float frame rate (frames per second)
		 */
		public void SetFrameRate(float fps) 
		{
			if (fps != 0f) 
			{
                delay = (int)Math.Round(100f / fps, MidpointRounding.AwayFromZero);
			}
		}
	
		/**
		 * Sets quality of color quantization (conversion of images
		 * to the maximum 256 colors allowed by the GIF specification).
		 * Lower values (minimum = 1) produce better colors, but slow
		 * processing significantly.  10 is the default, and produces
		 * good color mapping at reasonable speeds.  Values greater
		 * than 20 do not yield significant improvements in speed.
		 *
		 * @param quality int greater than 0.
		 * @return
		 */
		public void SetQuality(int quality) 
		{
			if (quality < 1) quality = 1;
			sample = quality;
		}
	
		/**
		 * Sets the GIF frame size.  The default size is the
		 * size of the first frame added if this method is
		 * not invoked.
		 *
		 * @param w int frame width.
		 * @param h int frame width.
		 */
		public void SetSize(int w, int h) 
		{
			if (started && !firstFrame) return;
			width = w;
			height = h;
			if (width < 1) width = 320;
			if (height < 1) height = 240;
			sizeSet = true;
		}
	
		/**
		 * Initiates GIF file creation on the given stream.  The stream
		 * is not closed automatically.
		 *
		 * @param os OutputStream on which GIF images are written.
		 * @return false if initial write failed.
		 */
		public bool Start( FileStream os) 
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
	
		/**
		 * Initiates writing of a GIF file with the specified name.
		 *
		 * @param file String containing output file name.
		 * @return false if open or initial write failed.
		 */
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
	
		/**
		 * Analyzes image colors and creates color map.
		 */
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
	
		/**
		 * Returns index of palette color closest to c
		 *
		 */
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
	
		/**
		 * Extracts image pixels into byte array "pixels"
		 */
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
				ToDo:
				improve performance: use unsafe code 
			*/
			pixels = new Byte [ 3 * image.Width * image.Height ];
			int count = 0;
			Bitmap tempBitmap = new Bitmap( image );
			for (int th = 0; th < image.Height; th++)
			{
				for (int tw = 0; tw < image.Width; tw++)
				{
					Color color = tempBitmap.GetPixel(tw, th);
					pixels[count] = color.R;
					count++;
					pixels[count] = color.G;
					count++;
					pixels[count] = color.B;
					count++;
				}
			}

			//		pixels = ((DataBufferByte) image.getRaster().getDataBuffer()).getData();
		}
	
		/**
		 * Writes Graphic Control Extension
		 */
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
	
		/**
		 * Writes Image Descriptor
		 */
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
	
		/**
		 * Writes Logical Screen Descriptor
		 */
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
	
		/**
		 * Writes Netscape application extension to define
		 * repeat count.
		 */
		protected void WriteNetscapeExt()
		{
			fs.WriteByte(0x21); // extension introducer
			fs.WriteByte(0xff); // app extension label
			fs.WriteByte(11); // block size
			WriteString("NETSCAPE" + "2.0"); // app id + auth code
			fs.WriteByte(3); // sub-block size
			fs.WriteByte(1); // loop sub-block id
			WriteShort(repeat); // loop count (extra iterations, 0=repeat forever)
			fs.WriteByte(0); // block terminator
		}
	
		/**
		 * Writes color table
		 */
		protected void WritePalette()
		{
			fs.Write(colorTab, 0, colorTab.Length);
			int n = (3 * 256) - colorTab.Length;
			for (int i = 0; i < n; i++) 
			{
				fs.WriteByte(0);
			}
		}
	
		/**
		 * Encodes and writes pixel data
		 */
		protected void WritePixels()
		{
			LZWEncoder encoder =
				new LZWEncoder(width, height, indexedPixels, colorDepth);
			encoder.Encode( fs );
		}
	
		/**
		 *    Write 16-bit value to output stream, LSB first
		 */
		protected void WriteShort(int value)
		{
			fs.WriteByte( Convert.ToByte( value & 0xff));
			fs.WriteByte( Convert.ToByte( (value >> 8) & 0xff ));
		}
	
		/**
		 * Writes string to output stream
		 */
		protected void WriteString(String s)
		{
			char[] chars = s.ToCharArray();
			for (int i = 0; i < chars.Length; i++) 
			{
				fs.WriteByte((byte) chars[i]);
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