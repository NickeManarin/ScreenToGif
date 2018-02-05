using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using ScreenToGif.Util;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ScreenToGif.ImageUtil.Video
{
    /// <summary>
    /// Wrapper for the Win32 AVIFile library.
    /// </summary>
    public class AviWriter : IDisposable
    {
        #region Variables

        /// <summary>Handle to the AVI file.</summary>
        private IntPtr _aviFile;
        /// <summary>Handle to the AVI stream.</summary>
        private IntPtr _aviStream;
        /// <summary>Handle to the compressed AVI stream.</summary>
        private IntPtr _compStream;
        /// <summary>Number of frames written to the AVI file.</summary>
        private int _frameCount;
        /// <summary>The width of the video.</summary>
        private readonly int _width;
        /// <summary>The height of the video.</summary>
        private readonly int _height;
        /// <summary>The stride of the video.</summary>
        private readonly uint _stride;
        /// <summary>Whether this AviFile has been disposed.</summary>
        private bool _disposed;

        #endregion

        /// <summary>Initialize the AviFile.</summary>
        /// <param name="path">The path to the output file.</param>
        /// <param name="frameRate">The frame rate for the video.</param>
        /// <param name="width">The width of the video.</param>
        /// <param name="height">The height of the video.</param>
        /// <param name="quality">Video quality 0 to 10000.</param>
        public AviWriter(string path, int frameRate, int width, int height, uint quality = 10000)
        {
            #region Validation

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (frameRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameRate), frameRate, "The frame rate must be at least 1 frame per second.");

            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be at least 1.");

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be at least 1.");

            #endregion

            //Store parameters.
            _width = width;
            _height = height;

            _disposed = false;

            //Get the stride information by creating a new bitmap and querying it
            using (var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                _stride = (uint)bmpData.Stride;
                bmp.UnlockBits(bmpData);
            }

            try
            {
                //Initialize the AVI library.
                AVIFileInit();

                //Open the output AVI file.
                var rv = AVIFileOpen(out _aviFile, path, AVI_OPEN_MODE_CREATEWRITE, IntPtr.Zero);

                if (rv != 0)
                    throw new Win32Exception(((AviErrors)rv).ToString());

                //Create a new stream in the avi file.
                var aviStreamInfo = new AVISTREAMINFOW
                {
                    fccType = GetFourCc("vids"),
                    fccHandler = GetFourCc("CVID"), // 808810089, //IV50
                    dwScale = 1,
                    dwRate = (uint)frameRate,
                    dwSuggestedBufferSize = (uint)(_height * _stride),
                    dwQuality = quality, //-1 default 0xffffffff, 0 to 10.000

                    rcFrame = new Native.Rect
                    {
                        Bottom = _height,
                        Right = _width
                    }
                };

                rv = AVIFileCreateStream(_aviFile, out _aviStream, ref aviStreamInfo);

                if (rv != 0)
                    throw new Win32Exception(((AviErrors)rv).ToString());

                //Set compress options.
                var options = new AVICOMPRESSOPTIONS
                {
                    fccType = GetFourCc("vids"),
                    lpParms = IntPtr.Zero,
                    lpFormat = IntPtr.Zero
                };

                var mem = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AVICOMPRESSOPTIONS)));

                Marshal.StructureToPtr(options, mem, false);
                var streams = new[] { _aviStream };
                var infPtrs = new[] { mem };

                var ok = AVISaveOptions(IntPtr.Zero, ICMF_CHOOSE_KEYFRAME | ICMF_CHOOSE_DATARATE, 1, streams, infPtrs);

                if (ok)
                    options = (AVICOMPRESSOPTIONS)Marshal.PtrToStructure(mem, typeof(AVICOMPRESSOPTIONS));

                Marshal.FreeHGlobal(mem);

                if (!ok)
                    throw new Exception("User cancelled the operation.");

                rv = AVIMakeCompressedStream(out _compStream, _aviStream, ref options, 0);

                if (rv != 0)
                    throw new Win32Exception(((AviErrors)rv).ToString());

                //Configure the compressed stream.
                var streamFormat = new BITMAPINFOHEADER
                {
                    biSize = 40,
                    biWidth = _width,
                    biHeight = _height,
                    biPlanes = 1,
                    biBitCount = 24,
                    biSizeImage = (uint)(_stride * _height)
                };

                rv = AVIStreamSetFormat(_compStream, 0, ref streamFormat, 40);

                if (rv != 0)
                    throw new Win32Exception(((AviErrors)rv).ToString());
            }
            catch
            {
                // Clean up
                Dispose(false);

                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch { }

                throw;
            }
        }

        /// <summary>
        /// Clean up the AviFile.
        /// </summary>
        ~AviWriter()
        {
            Dispose(false);
        }

        /// <summary>
        /// Adds a Bitmap to the end of the AviFile video sequence.
        /// </summary>
        /// <param name="frame">The frame to be added.</param>
        public void AddFrame(Bitmap frame, bool flip = true)
        {
            // Validate the bitmap
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (frame.Width != _width || frame.Height != _height)
                throw new ArgumentException("The frame bitmap is the incorrect size for this video.", nameof(frame));

            //Write the frame to the file.
            //TODO: Verify if this is needed.
            if (flip)
                frame.RotateFlip(RotateFlipType.RotateNoneFlipY);

            BitmapData frameData = null;

            try
            {
                frameData = frame.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                var rv = AVIStreamWrite(_compStream, _frameCount, 1, frameData.Scan0, (int)(_stride * _height), 0, IntPtr.Zero, IntPtr.Zero);

                if (rv != 0)
                    throw new Win32Exception(((AviErrors)rv).ToString());

                frame.UnlockBits(frameData);
            }
            catch
            {
                try
                {
                    if (frameData != null)
                        frame.UnlockBits(frameData);
                }
                catch { }

                throw;
            }

            frame.Dispose();

            _frameCount++;
        }

        /// <summary>
        /// Adds a BitmapSource to the end of the AviFile video sequence. Not working...
        /// </summary>
        /// <param name="source">The BitmapFrame to be added.</param>
        public void AddFrame(BitmapSource source)
        {
            // Validate the bitmap
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (source == null)
                throw new ArgumentNullException("frame");
            if ((int)source.Width != _width || (int)source.Height != _height)
                throw new ArgumentException("The frame bitmap is the incorrect size for this video.", "frame");

            var writeBit = new WriteableBitmap(source);

            try
            {
                writeBit.Lock();
                var rv = AVIStreamWrite(_aviStream, _frameCount, 1, writeBit.BackBuffer, (int)(_stride * _height), 0, IntPtr.Zero, IntPtr.Zero);

                if (rv != 0)
                    throw new Win32Exception(rv, "Unable to write the frame to the AVI.");

                writeBit.Unlock();
            }
            catch
            {
                try
                {
                    writeBit.Unlock();
                }
                catch { }

                throw;
            }

            _frameCount++;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Clean up the AviFile.
        /// </summary>
        /// <param name="disposing">Whether this is being called from Dispose or from the finalizer.</param>
        protected void Dispose(bool disposing)
        {
            if (_disposed) return;

            _disposed = true;

            if (disposing)
                GC.SuppressFinalize(this);

            if (_aviStream != IntPtr.Zero)
            {
                AVIStreamRelease(_aviStream);
                AVIStreamRelease(_compStream);
                _aviStream = IntPtr.Zero;
            }

            if (_aviFile != IntPtr.Zero)
            {
                AVIFileRelease(_aviFile);
                _aviFile = IntPtr.Zero;
            }

            AVIFileExit();
        }

        private uint GetFourCc(string fcc)
        {
            if (fcc == null) throw new ArgumentNullException(nameof(fcc));
            if (fcc.Length != 4) throw new ArgumentOutOfRangeException(nameof(fcc), fcc, "FOURCC codes must be four characters in length.");

            return Convert.ToUInt32(char.ToLower(fcc[0]) | char.ToLower(fcc[1]) << 8 | char.ToLower(fcc[2]) << 16 | char.ToLower(fcc[3]) << 24);
        }

        #region Native

        /// <summary>
        /// The AVISTREAMINFO structure contains information for a single stream.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AVISTREAMINFOW
        {
            /// <summary>Four-character code indicating the stream type.</summary>
            public uint fccType;

            /// <summary>Four-character code of the compressor handler that will compress this video stream when it is saved.</summary>
            public uint fccHandler;

            /// <summary>Applicable flags for the stream.</summary>
            public uint dwFlags;

            /// <summary>Capability flags; currently unused.</summary>
            public uint dwCaps;

            /// <summary>Priority of the stream.</summary>
            public ushort wPriority;

            /// <summary>Language of the stream.</summary>
            public ushort wLanguage;

            /// <summary>Time scale applicable for the stream.</summary>
            public uint dwScale;

            /// <summary>Rate in an integer format.</summary>
            public uint dwRate;

            /// <summary>Sample number of the first frame of the AVI file.</summary>
            public uint dwStart;

            /// <summary>Length of this stream.</summary>
            public uint dwLength;

            /// <summary>Audio skew.  Specifies how much to skew the audio data ahead of the video frames in interleaved files.</summary>
            public uint dwInitialFrames;

            /// <summary>Recommended buffer size, in bytes, for the stream.</summary>
            public uint dwSuggestedBufferSize;

            /// <summary>Quality indicator of the video data in the stream. Quality is represented as a number between 0 and 10,000.</summary>
            public uint dwQuality;

            /// <summary>Size, in bytes, of a single data sample.</summary>
            public uint dwSampleSize;

            /// <summary>Dimensions of the video destination rectangle.</summary>
            public Native.Rect rcFrame;

            /// <summary>Number of times the stream has been edited.</summary>
            public uint dwEditCount;

            /// <summary>Number of times the stream format has changed.</summary>
            public uint dwFormatChangeCount;

            /// <summary>Null-terminated string containing a description of the stream.</summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szName;
        }

        /// <summary>The AVICOMPRESSOPTIONS structure contains information about a stream and how it is compressed and saved.</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AVICOMPRESSOPTIONS
        {
            /// <summary>Four-character code indicating the stream type.</summary>
            public uint fccType;
            /// <summary>Four-character code for the compressor handler that will compress this video stream when it is saved.</summary>
            public uint fccHandler;
            /// <summary>Maximum period between video key frames. Only used with AVICOMPRESSF_KEYFRAMES.</summary>
            public uint dwKeyFrameEvery;
            /// <summary>Quality value passed to a video compressor.</summary>
            public uint dwQuality;
            /// <summary>Video compressor data rate. Only used with AVICOMPRESSF_DATARATE.</summary>
            public uint dwBytesPerSecond;
            /// <summary>Flags used for compression.</summary>
            public uint dwFlags;
            /// <summary>Pointer to a structure defining the data format.</summary>
            public IntPtr lpFormat;
            /// <summary>Size, in bytes, of the data referenced by lpFormat.</summary>
            public uint cbFormat;
            /// <summary>Video-compressor-specific data; used internally.</summary>
            public IntPtr lpParms;
            /// <summary>Size, in bytes, of the data referenced by lpParms.</summary>
            public uint cbParms;
            /// <summary>Interleave factor for interspersing stream data with data from the first stream.</summary>
            public uint dwInterleaveEvery;
        }

        /// <summary>The BITMAPINFOHEADER structure contains information about the dimensions and color format of a DIB.</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BITMAPINFOHEADER
        {
            /// <summary>Specifies the number of bytes required by the structure.</summary>
            public uint biSize;
            /// <summary>Specifies the width of the bitmap, in pixels.</summary>
            public int biWidth;
            /// <summary>Specifies the height of the bitmap, in pixels.</summary>
            public int biHeight;
            /// <summary>Specifies the number of planes for the target device. This value must be set to 1.</summary>
            public short biPlanes;
            /// <summary>Specifies the number of bits-per-pixel.</summary>
            public short biBitCount;
            /// <summary>Specifies the type of compression for a compressed bottom-up bitmap.</summary>
            public uint biCompression;
            /// <summary>Specifies the size, in bytes, of the image.</summary>
            public uint biSizeImage;
            /// <summary>Specifies the horizontal resolution, in pixels-per-meter, of the target device for the bitmap.</summary>
            public int biXPelsPerMeter;
            /// <summary>Specifies the vertical resolution, in pixels-per-meter, of the target device for the bitmap.</summary>
            public int biYPelsPerMeter;
            /// <summary>Specifies the number of color indexes in the color table that are actually used by the bitmap.</summary>
            public uint biClrUsed;
            /// <summary>Specifies the number of color indexes that are required for displaying the bitmap.</summary>
            public uint biClrImportant;
        }


        /// <summary>Open mode value for AVIs to create and write to the file.</summary>
        private const int AVI_OPEN_MODE_CREATEWRITE = 0x00001000 | 0x00000001;

        public const uint ICMF_CHOOSE_KEYFRAME = 0x0001;
        public const uint ICMF_CHOOSE_DATARATE = 0x0002;
        public const uint ICMF_CHOOSE_PREVIEW = 0x0004;

        /// <summary>The AVIFileInit function initializes the AVIFile library.</summary>
        [DllImport("avifil32.dll")]
        private static extern void AVIFileInit();

        /// <summary>The AVIFileOpen function opens an AVI file and returns the address of a file interface used to access it.</summary>
        /// <param name="ppfile">Pointer to a buffer that receives the new IAVIFile interface pointer.</param>
        /// <param name="szFile">Null-terminated string containing the name of the file to open.</param>
        /// <param name="uMode">Access mode to use when opening the file.</param>
        /// <param name="pclsidHandler">Pointer to a class identifier of the standard or custom handler you want to use.</param>
        /// <returns>Returns zero if successful or an error otherwise.</returns>
        [DllImport("avifil32.dll", PreserveSig = true, CharSet = CharSet.Auto)]
        public static extern int AVIFileOpen(out IntPtr ppfile, string szFile, int uMode, IntPtr pclsidHandler);

        /// <summary>The AVIFileCreateStream function creates a new stream in an existing file and creates an interface to the new stream.</summary>
        /// <param name="pfile">Handle to an open AVI file.</param>
        /// <param name="ppavi">Pointer to the new stream interface.</param>
        /// <param name="psi">Pointer to a structure containing information about the new stream, including the stream type and its sample rate.</param>
        /// <returns>Returns zero if successful or an error otherwise.</returns>
        [DllImport("avifil32.dll")]
        private static extern int AVIFileCreateStream(IntPtr pfile, out IntPtr ppavi, ref AVISTREAMINFOW psi);

        /// <summary>The AVIMakeCompressedStream function creates a compressed stream from an uncompressed stream and a compression filter, and returns the address of a pointer to the compressed stream.</summary>
        /// <param name="ppsCompressed">Pointer to a buffer that receives the compressed stream pointer.</param>
        /// <param name="ppsSource">Pointer to the stream to be compressed.</param>
        /// <param name="lpOptions">Pointer to a structure that identifies the type of compression to use and the options to apply.</param>
        /// <param name="pclsidHandler">Pointer to a class identifier used to create the stream.</param>
        /// <returns></returns>
        [DllImport("avifil32.dll")]
        private static extern int AVIMakeCompressedStream(out IntPtr ppsCompressed, IntPtr ppsSource, ref AVICOMPRESSOPTIONS lpOptions, int pclsidHandler);

        /// <summary>
        /// The AVISaveOptions function retrieves the save options for a file and returns them in a buffer.
        /// </summary>
        /// <param name="hwnd">Handle to the parent window for the Compression Options dialog box.</param>
        /// <param name="uiFlags">Flags for displaying the Compression Options dialog box. The following flags are defined.</param>
        /// <param name="nStreams">Number of streams that have their options set by the dialog box. </param>
        /// <param name="ppavi">Pointer to an array of stream interface pointers. The nStreams parameter indicates the number of pointers in the array.</param>
        /// <param name="plpOptions">Pointer to an array of pointers to AVICOMPRESSOPTIONS structures. These structures hold the compression options set by the dialog box. 
        /// The nStreams parameter indicates the number of pointers in the array.</param>
        /// <returns>Returns TRUE if the user pressed OK, FALSE for CANCEL, or an error otherwise.</returns>
        [DllImport("avifil32.dll")]
        private static extern bool AVISaveOptions(IntPtr hwnd, uint uiFlags, int nStreams, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppavi, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] plpOptions);

        /// <summary>The AVIStreamSetFormat function sets the format of a stream at the specified position.</summary>
        /// <param name="pavi">Handle to an open stream.</param>
        /// <param name="lPos">Position in the stream to receive the format.</param>
        /// <param name="lpFormat">Pointer to a structure containing the new format.</param>
        /// <param name="cbFormat">Pointer to a structure containing the new format.</param>
        /// <returns>Returns zero if successful or an error otherwise.</returns>
        [DllImport("avifil32.dll")]
        private static extern int AVIStreamSetFormat(IntPtr pavi, int lPos, ref BITMAPINFOHEADER lpFormat, int cbFormat);

        /// <summary>The AVIStreamWrite function writes data to a stream.</summary>
        /// <param name="pavi">Handle to an open stream.</param>
        /// <param name="lStart">First sample to write.</param>
        /// <param name="lSamples">Number of samples to write.</param>
        /// <param name="lpBuffer">Pointer to a buffer containing the data to write.</param>
        /// <param name="cbBuffer">Size of the buffer referenced by lpBuffer.</param>
        /// <param name="dwFlags">Flag associated with this data.</param>
        /// <param name="plSampWritten">Pointer to a buffer that receives the number of samples written.</param>
        /// <param name="plBytesWritten">Pointer to a buffer that receives the number of bytes written.</param>
        /// <returns>Returns zero if successful or an error otherwise.</returns>
        [DllImport("avifil32.dll")]
        private static extern int AVIStreamWrite(IntPtr pavi, int lStart, int lSamples, IntPtr lpBuffer, int cbBuffer, int dwFlags, IntPtr plSampWritten, IntPtr plBytesWritten);

        /// <summary>The AVIStreamRelease function decrements the reference count of an AVI stream interface handle, and closes the stream if the count reaches zero.</summary>
        /// <param name="pavi">Handle to an open stream.</param>
        /// <returns>Returns the current reference count of the stream.</returns>
        [DllImport("avifil32.dll")]
        private static extern int AVIStreamRelease(IntPtr pavi);

        /// <summary>The AVIFileRelease function decrements the reference count of an AVI file interface handle and closes the file if the count reaches zero.</summary>
        /// <param name="pfile">Handle to an open AVI file.</param>
        /// <returns>Returns the reference count of the file.</returns>
        [DllImport("avifil32.dll")]
        private static extern int AVIFileRelease(IntPtr pfile);

        /// <summary>The AVIFileExit function exits the AVIFile library and decrements the reference count for the library.</summary>
        [DllImport("avifil32.dll")]
        private static extern void AVIFileExit();

        /// <summary>AVI error codes</summary>
        private enum AviErrors : uint
        {
            Unsupported = 0x80044065,
            BadFormat = 0x80044066,
            Memory = 0x80044067,
            Internal = 0x80044068,
            BadFlags = 0x80044069,
            BadParam = 0x8004406A,
            BadSize = 0x8004406B,
            BadHandle = 0x8004406C,
            FileRead = 0x8004406D,
            FileWrite = 0x8004406E,
            FileOpen = 0x8004406F,
            Compressor = 0x80044070,
            NoCompressor = 0x80044071,
            ReadOnly = 0x80044072,
            NoData = 0x80044073,
            BufferTooSmall = 0x80044074,
            CanNotCompress = 0x80044075,
            UserAbort = 0x800440C6,
            Error = 0x800440C7
        }



        #endregion
    }
}