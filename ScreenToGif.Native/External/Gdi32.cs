using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.External
{
    public static class Gdi32
    {
        ///<summary>
        ///Creates a memory device context (DC) compatible with the specified device.
        ///</summary>
        ///<param name="hdc">A handle to an existing DC. If this handle is NULL,
        ///the function creates a memory DC compatible with the application's current screen.</param>
        ///<returns>
        ///If the function succeeds, the return value is the handle to a memory DC.
        ///If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.
        ///</returns>
        [DllImport(Constants.Gdi32, EntryPoint = "CreateCompatibleDC", SetLastError = true)]
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
        [DllImport(Constants.Gdi32, EntryPoint = "SelectObject")]
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
        ///<c>true</c> if the operation succeeds, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
        ///</returns>
        [DllImport(Constants.Gdi32, EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperations dwRop);

        [DllImport(Constants.Gdi32, EntryPoint = "StretchBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StretchBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidthDest, int nHeightDest, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, int nWidthSource, int nHeightSource, CopyPixelOperations dwRop);

        /// <summary>
        /// pbmi was BitmapInfo.
        /// </summary>
        /// <param name="hdc"></param>
        /// <param name="pbmi"></param>
        /// <param name="pila"></param>
        /// <param name="ppvBits"></param>
        /// <param name="hSection"></param>
        /// <param name="dwOffset"></param>
        /// <returns></returns>
        [DllImport(Constants.Gdi32)]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref IntPtr pbmi, uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        /// <summary>
        /// Retrieves the bits of the specified compatible bitmap and copies them into a buffer as a DIB using the specified format.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hbmp">A handle to the bitmap. This must be a compatible bitmap (DDB).</param>
        /// <param name="uStartScan">The first scan line to retrieve.</param>
        /// <param name="cScanLines">The number of scan lines to retrieve.</param>
        /// <param name="lpvBits">A pointer to a buffer to receive the bitmap data. If this parameter is <see cref="IntPtr.Zero"/>, the function passes the dimensions and format of the bitmap to the <see cref="BITMAPINFO"/> structure pointed to by the <paramref name="lpbi"/> parameter.</param>
        /// <param name="lpbi">A pointer to a <see cref="BITMAPINFO"/> structure that specifies the desired format for the DIB data.</param>
        /// <param name="uUsage">The format of the bmiColors member of the <see cref="BITMAPINFO"/> structure. It must be one of the following values.</param>
        /// <returns>If the lpvBits parameter is non-NULL and the function succeeds, the return value is the number of scan lines copied from the bitmap.
        /// If the lpvBits parameter is NULL and GetDIBits successfully fills the <see cref="BITMAPINFO"/> structure, the return value is nonzero.
        /// If the function fails, the return value is zero.
        /// This function can return the following value: ERROR_INVALID_PARAMETER (87 (0×57))</returns>
        [DllImport(Constants.Gdi32, EntryPoint = "GetDIBits")]
        public static extern int GetDIBits([In] IntPtr hdc, [In] IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BitmapInfoHeader lpbi, DibColorModes uUsage);
        
        ///<summary>Deletes the specified device context (DC).</summary>
        ///<param name="hdc">A handle to the device context.</param>
        ///<returns><para>If the function succeeds, the return value is nonzero.</para><para>If the function fails, the return value is zero.</para></returns>
        ///<remarks>An application must not delete a DC whose handle was obtained by calling the <c>GetDC</c> function. Instead, it must call the <c>ReleaseDC</c> function to free the DC.</remarks>
        [DllImport(Constants.Gdi32, EntryPoint = "DeleteDC")]
        public static extern bool DeleteDC([In] IntPtr hdc);

        [DllImport(Constants.Gdi32)]
        public static extern int GetObject(IntPtr hgdiobj, int cbBuffer, IntPtr lpvObject);

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
        [DllImport(Constants.Gdi32, EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        
        [DllImport(Constants.Gdi32)]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        
        [DllImport(Constants.Gdi32)]
        internal static extern int GetDeviceCaps(IntPtr hdc, int capindex);
        
        [DllImport(Constants.Gdi32)]
        internal static extern bool PatBlt(IntPtr hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, uint dwRop);

        [DllImport(Constants.Gdi32)]
        internal static extern bool GetCurrentPositionEx(IntPtr hdc, out PointW lpPoint);

        [DllImport(Constants.Gdi32)]
        internal static extern bool GetWindowOrgEx(IntPtr hdc, out PointW lpPoint);
    }
}