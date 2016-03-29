using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenToGif.ImageUtil.GifEncoder2
{
    public class GifFile : IDisposable
    {
        #region Header Constants

        private const string FileTypeVersion = "GIF89a";

        private const byte FileTrailer = 0x3b;

        private const int ApplicationExtensionBlockIdentifier = 0xff21;

        private const byte ApplicationBlockSize = 0x0b;

        private const string ApplicationIdentification = "NETSCAPE2.0";

        private const int GraphicControlExtensionBlockIdentifier = 0xf921;

        private const byte GraphicControlExtensionBlockSize = 0x04;

        private const long SourceGlobalColorInfoPosition = 10;

        private const long SourceGraphicControlExtensionPosition = 781;

        private const long SourceGraphicControlExtensionLength = 8;

        private const long SourceImageBlockPosition = 789;

        private const long SourceImageBlockHeaderLength = 11;

        private const long SourceColorBlockPosition = 13;

        private const long SourceColorBlockLength = 768;

        #endregion

        #region Properties

        /// <summary>
        /// Repeat Count for the gif.
        /// </summary>
        public int RepeatCount { get; set; }

        public Size Size { get; set; }

        public Color? TransparentColor { get; set; }
        
        private Stream InternalStream { get; set; }

        private bool IsFirstFrame { get; set; } = true;

        #endregion

        public GifFile(Stream stream, Size size, Color? transparent, int repeatCount = 1)
        {
            InternalStream = stream;
            Size = size;
            TransparentColor = transparent;
            RepeatCount = repeatCount;
        }

        #region Public Methods

        public void AddFrame(string path, int delay = 66, int x = 0, int y = 0)
        {
            using (var imageStream = new FileStream(path, FileMode.Open))
            {
                var palette = GetPalette(imageStream);

                if (IsFirstFrame)
                {
                    IsFirstFrame = false;

                    WriteLogicalScreenDescriptor();
                    WritePalette();
                }
            }
        }

        #endregion

        #region Private Methods

        private void WriteLogicalScreenDescriptor()
        {
            //File Header
            WriteString(FileTypeVersion);

            //Initial Logical Size (Width, Height)
            WriteShort((int)Size.Width);
            WriteShort((int)Size.Height);

            //Packed fields
            WriteByte(Convert.ToByte(
                    0x80 |   //#1   : Global Color Table Flag (1 = GCT Used)
                    0x70 |   //#2-4 : Color Resolution = 7
                    0x00 |   //#5   : GCT Sort Flag = (0 = Not Sorted)
                    7        //#6-8 : Global Color Table size (bits-1)
                    ));

            WriteByte(0); //Background color index
            WriteByte(0); //Pixel aspect ratio - Assume 1:1
        }

        private List<Color> GetPalette(Stream imageStream)
        {
            var bmp = new WriteableBitmap(imageStream.SourceFrom());
            bmp.Lock();

            int stride = bmp.PixelWidth * 4;
            int size = bmp.PixelHeight * stride;
            var imgData = new byte[size];
            //int index = y * stride + 4 * x; //To acess a specific pixel.

            Marshal.Copy(bmp.BackBuffer, imgData, 0, imgData.Length);

            bmp.Unlock();

            var colorList = new List<Color>();

            for (int index = 0; index < imgData.Length - 1; index += 4)
            {
                colorList.Add(
                    Color.FromArgb(imgData[index], imgData[index + 1], 
                    imgData[index + 2], imgData[index + 3]));
            }

            var result = colorList.GroupBy<Color, Color>(x => x) //grouping based on its value
                .OrderByDescending(g => g.Count()) //order by most frequent values
                .Select(g => g.FirstOrDefault()) //take the first among the group
                .Take(255);

            return result.ToList();
        }

        private void WritePalette()
        {
            
        }

        private void WriteByte(int value)
        {
            InternalStream.WriteByte(Convert.ToByte(value));
        }

        private void WriteShort(int value)
        {
            InternalStream.WriteByte(Convert.ToByte(value & 0xff));
            InternalStream.WriteByte(Convert.ToByte((value >> 8) & 0xff));
        }

        private void WriteString(string value)
        {
            InternalStream.Write(value.ToArray().Select(c => (byte)c).ToArray(), 0, value.Length);
        }

        #endregion

        public void Dispose()
        {
            // Complete File
            WriteByte(FileTrailer);
            // Pushing data
            InternalStream.Flush();
            //Resets the stream position to save afterwards.
            InternalStream.Position = 0;
        }
    }
}
