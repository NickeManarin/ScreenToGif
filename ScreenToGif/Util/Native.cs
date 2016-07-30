using System;
//using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ScreenToGif.Util.Writers;
//using Size = System.Drawing.Size;

namespace ScreenToGif.Util
{
    public static class Native
    {
        #region Variables/Const

        internal const int MonitorDefaultToNull = 0;
        internal const int MonitorDefaultToPrimary = 1;
        internal const int MonitorDefaultToNearest = 2;

        internal const int CursorShowing = 0x00000001;
        internal const int DstInvert = 0x00550009;

        [StructLayout(LayoutKind.Sequential)]
        public struct ICONINFO
        {
            public bool fIcon;      // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies 
            public int xHotspot;    // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot 
            public int yHotspot;    // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot 
            public IntPtr hbmMask;  // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, 
            public IntPtr hbmColor; // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this 
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            /// <summary>
            /// Specifies the size, in bytes, of the structure. 
            /// </summary>
            public int cbSize;

            /// <summary>
            /// Specifies the cursor state. This parameter can be one of the following values:
            /// </summary>
            public int flags;

            ///<summary>
            ///Handle to the cursor. 
            ///</summary>
            public IntPtr hCursor;

            /// <summary>
            /// A POINT structure that receives the screen coordinates of the cursor. 
            /// </summary>
            public POINT ptScreenPos;
        }

        ///<summary>
        ///Specifies a raster-operation code. These codes define how the color data for the
        ///source rectangle is to be combined with the color data for the destination
        ///rectangle to achieve the final color.
        ///</summary>
        public enum CopyPixelOperation
        {
            NoMirrorBitmap = -2147483648,

            /// <summary>dest = BLACK, 0x00000042</summary>
            Blackness = 66,

            ///<summary>dest = (NOT src) AND (NOT dest), 0x001100A6</summary>
            NotSourceErase = 1114278,

            ///<summary>dest = (NOT source), 0x00330008</summary>
            NotSourceCopy = 3342344,

            ///<summary>dest = source AND (NOT dest), 0x00440328</summary>
            SourceErase = 4457256,

            /// <summary>dest = (NOT dest), 0x00550009</summary>
            DestinationInvert = 5570569,

            /// <summary>dest = pattern XOR dest, 0x005A0049</summary>
            PatInvert = 5898313,

            ///<summary>dest = source XOR dest, 0x00660046</summary>
            SourceInvert = 6684742,

            ///<summary>dest = source AND dest, 0x008800C6</summary>
            SourceAnd = 8913094,

            /// <summary>dest = (NOT source) OR dest, 0x00BB0226</summary>
            MergePaint = 12255782,

            ///<summary>dest = (source AND pattern), 0x00C000CA</summary>
            MergeCopy = 12583114,

            ///<summary>dest = source, 0x00CC0020</summary>
            SourceCopy = 13369376,

            /// <summary>dest = source OR dest, 0x00EE0086</summary>
            SourcePaint = 15597702,

            /// <summary>dest = pattern, 0x00F00021</summary>
            PatCopy = 15728673,

            /// <summary>dest = DPSnoo, 0x00FB0A09</summary>
            PatPaint = 16452105,

            /// <summary>dest = WHITE, 0x00FF0062</summary>
            Whiteness = 16711778,

            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows 
            /// such as WPF windows with AllowsTransparency="true", 0x40000000
            /// </summary>
            CaptureBlt = 1073741824,
        }

        public enum DeviceCaps : int
        {
            LogPixelsX = 88,
            LogPixelsY = 90,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner

            public Int32Rect ToRectangle()
            {
                return new Int32Rect(Left, Top, Right - Left, Bottom - Top);
            }
        }

        public struct Margins
        {
            public Margins(Thickness t)
            {
                Left = (int)t.Left;
                Right = (int)t.Right;
                Top = (int)t.Top;
                Bottom = (int)t.Bottom;
            }

            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        public enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }

        enum DwmWindowAttribute
        {
            DwmwaNcrenderingEnabled = 1,
            DwmwaNcrenderingPolicy,
            DwmwaTransitionsForcedisabled,
            DwmwaAllowNcpaint,
            DwmwaCaptionButtonBounds,
            DwmwaNonclientRtlLayout,
            DwmwaForceIconicRepresentation,
            DwmwaFlip3DPolicy,
            DwmwaExtendedFrameBounds,
            DwmwaHasIconicBitmap,
            DwmwaDisallowPeek,
            DwmwaExcludedFromPeek,
            DwmwaCloak,
            DwmwaCloaked,
            DwmwaFreezeRepresentation,
            DwmwaLast
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MonitorInfoEx
        {
            public int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
            public Rect rcMonitor = new Rect();
            public Rect rcWork = new Rect();
            public int dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }

        #endregion

        #region Functions

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        public static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll", EntryPoint = "CopyIcon")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", EntryPoint = "GetIconInfo")]
        public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        ///<summary>
        ///Creates a memory device context (DC) compatible with the specified device.
        ///</summary>
        ///<param name="hdc">A handle to an existing DC. If this handle is NULL,
        ///the function creates a memory DC compatible with the application's current screen.</param>
        ///<returns>
        ///If the function succeeds, the return value is the handle to a memory DC.
        ///If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.
        ///</returns>
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        ///<summary>Selects an object into the specified device context (DC). The new object replaces the previous object of the same type.</summary>
        ///<param name="hdc">A handle to the DC.</param>
        ///<param name="hgdiobj">A handle to the object to be selected.</param>
        ///<returns>
        ///<para>If the selected object is not a region and the function succeeds, the return value is a handle to the object being replaced. If the selected object is a region and the function succeeds, the return value is one of the following values.</para>
        ///<para>SIMPLEREGION - Region consists of a single rectangle.</para>
        ///<para>COMPLEXREGION - Region consists of more than one rectangle.</para>
        ///<para>NULLREGION - Region is empty.</para>
        ///<para>If an error occurs and the selected object is not a region, the return value is <c>NULL</c>. Otherwise, it is <c>HGDI_ERROR</c>.</para>
        ///</returns>
        ///<remarks>
        ///<para>This function returns the previously selected object of the specified type. An application should always replace a new object with the original, default object after it has finished drawing with the new object.</para>
        ///<para>An application cannot select a single bitmap into more than one DC at a time.</para>
        ///<para>ICM: If the object being selected is a brush or a pen, color management is performed.</para>
        ///</remarks>
        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        ///<summary>
        ///Performs a bit-block transfer of the color data corresponding to a
        ///rectangle of pixels from the specified source device context into
        ///a destination device context.
        ///</summary>
        ///<param name="hdc">Handle to the destination device context.</param>
        ///<param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
        ///<param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
        ///<param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
        ///<param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
        ///<param name="hdcSrc">Handle to the source device context.</param>
        ///<param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
        ///<param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
        ///<param name="dwRop">A raster-operation code.</param>
        ///<returns>
        ///<c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
        ///</returns>
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop); 
        //TernaryRasterOperations

        ///<summary>Deletes the specified device context (DC).</summary>
        ///<param name="hdc">A handle to the device context.</param>
        ///<returns><para>If the function succeeds, the return value is nonzero.</para><para>If the function fails, the return value is zero.</para></returns>
        ///<remarks>An application must not delete a DC whose handle was obtained by calling the <c>GetDC</c> function. Instead, it must call the <c>ReleaseDC</c> function to free the DC.</remarks>
        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public static extern bool DeleteDC([In] IntPtr hdc);

        ///<summary>Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object. After the object is deleted, the specified handle is no longer valid.</summary>
        ///<param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
        ///<returns>
        ///<para>If the function succeeds, the return value is nonzero.</para>
        ///<para>If the specified handle is not valid or is currently selected into a DC, the return value is zero.</para>
        ///</returns>
        ///<remarks>
        ///<para>Do not delete a drawing object (pen or brush) while it is still selected into a DC.</para>
        ///<para>When a pattern brush is deleted, the bitmap associated with the brush is not deleted. The bitmap must be deleted independently.</para>
        ///</remarks>
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int capindex);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr handle);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        [DllImport("gdi32.dll")]
        public static extern bool PatBlt(IntPtr hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, uint dwRop);

        [DllImport("user32.dll")]
        static extern bool OffsetRect(ref Rect lprc, int dx, int dy);

        [DllImport("gdi32.dll")]
        public static extern bool GetCurrentPositionEx(IntPtr hdc, out POINT lpPoint);

        [DllImport("gdi32.dll")]
        public static extern bool GetWindowOrgEx(IntPtr hdc, out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        //[DllImport("SHCore.dll", SetLastError = true)]
        //public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        //[DllImport("SHCore.dll", SetLastError = true)]
        //public static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);

        #endregion

        #region Methods

        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="size">The size of the final image.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <returns>A bitmap withe the capture rectangle.</returns>
        public static BitmapSource Capture2(Size size, int positionX, int positionY)
        {
            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);
            var hBmp = CreateCompatibleBitmap(hSrce, (int)size.Width, (int)size.Height);
            var hOldBmp = SelectObject(hDest, hBmp);

            var b = BitBlt(hDest, 0, 0, (int)size.Width, (int)size.Height, hSrce, positionX, positionY, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                //return Image.FromHbitmap(hBmp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static System.Drawing.Image Capture(Size size, int positionX, int positionY)
        {
            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);
            var hBmp = CreateCompatibleBitmap(hSrce, (int)size.Width, (int)size.Height);
            var hOldBmp = SelectObject(hDest, hBmp);

            var b = BitBlt(hDest, 0, 0, (int)size.Width, (int)size.Height, hSrce, positionX, positionY, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            try
            {
                return System.Drawing.Image.FromHbitmap(hBmp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        /// <summary>
        /// Draws a rectangle over a Window.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        public static void DrawFrame(IntPtr hWnd, double scale)
        {
            if (hWnd == IntPtr.Zero)
                return;

            var hdc = GetWindowDC(hWnd);

            //TODO: Adjust for high DPI.
            Rect rect;
            GetWindowRect(hWnd, out rect);
            //DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(Rect)));
            OffsetRect(ref rect, -rect.Left, -rect.Top);

            const int frameWidth = 3;

            PatBlt(hdc, rect.Left, rect.Top, rect.Right - rect.Left, frameWidth, DstInvert);

            PatBlt(hdc, rect.Left, rect.Bottom - frameWidth, frameWidth,
                -(rect.Bottom - rect.Top - 2 * frameWidth), DstInvert);

            PatBlt(hdc, rect.Right - frameWidth, rect.Top + frameWidth, frameWidth,
                rect.Bottom - rect.Top - 2 * frameWidth, DstInvert);

            PatBlt(hdc, rect.Right, rect.Bottom - frameWidth, -(rect.Right - rect.Left),
                frameWidth, DstInvert);
        }

        private static bool ExtendedFrameBounds(IntPtr handle, out Int32Rect rectangle)
        {
            Rect rect;
            var result = DwmGetWindowAttribute(handle, (int)DwmWindowAttribute.DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(Rect)));

            rectangle = rect.ToRectangle();

            return result >= 0;
        }

        private static Int32Rect GetWindowRect(IntPtr handle)
        {
            Rect rect;
            GetWindowRect(handle, out rect);
            return rect.ToRectangle();
        }

        public static Int32Rect TrueWindowRectangle(IntPtr handle)
        {
            Int32Rect rectangle;
            return ExtendedFrameBounds(handle, out rectangle) ? rectangle : GetWindowRect(handle);

        }

        internal static Size ScreenSizeFromWindow(Window window)
        {
            var pointer = MonitorFromWindow(new WindowInteropHelper(window).Handle, MonitorDefaultToNearest);

            var info = new MonitorInfoEx();
            GetMonitorInfo(pointer, ref info);

            var rect = info.rcWork.ToRectangle();
            
            return new Size(rect.Width, rect.Height);
        }

        #endregion
    }
}
