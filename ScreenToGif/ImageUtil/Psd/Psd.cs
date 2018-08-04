using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Psd
{
    internal class Psd : IDisposable
    {
        #region Properties

        /// <summary>
        /// The stream which the psd is writen on.
        /// </summary>
        private Stream InternalStream { get; set; }

        /// <summary>
        /// The stream which the psd's layer data are writen on.
        /// </summary>
        private Stream InternalDataStream { get; set; }

        /// <summary>
        /// Repeat Count for the psd.
        /// </summary>
        internal int RepeatCount { get; set; } = 0;

        /// <summary>
        /// The height of the image.
        /// </summary>
        internal int Height { get; set; } = 0;

        /// <summary>
        /// The width of the image.
        /// </summary>
        internal int Width { get; set; } = 0;

        /// <summary>
        /// True if it's the first frame of the psd.
        /// </summary>
        private bool IsFirstFrame { get; set; } = true;

        /// <summary>
        /// List of image data bytes.
        /// </summary>
        private List<byte[]> ImageDataList { get; set; } = new List<byte[]>();

        #endregion

        internal Psd(Stream stream, int repeatCount, int height, int width)
        {
            InternalStream = stream;
            InternalDataStream = new MemoryStream();
            RepeatCount = repeatCount;
            Height = height;
            Width = width;
        }

        internal void AddFrame(string path, int delay = 66)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (IsFirstFrame)
                {
                    IsFirstFrame = false;

                    //Psd Header: XX bytes.
                    InternalStream.WriteBytes(Encoding.ASCII.GetBytes("8BPS")); //Chunk type, 4 bytes.
                    InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)1)); //File version, 1 - PSD, 2 - PSB, 2 bytes.
                    InternalStream.Position += 6; //Must be zero, 6 bytes.
                    InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)3)); //Number of channels, 2 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)Height)); //Height of the image, 4 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)Width)); //Width of the image, 4 bytes.
                    InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)8)); //Number of bits per channel, 2 bytes.
                    InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)3)); //The color mode of the file, 3 - RGB, 2 bytes.

                    //Color mode data. 4 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //The size of the color mode data block, 0 bytes for RGB mode, 4 bytes.

                    //TODO: Write the image resource block to another stream, then merge with the main one.
                    //TODO: Or save the position and update later.
                    //Image resources. XX bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //The size of the image resource block, 4 bytes. TODO

                    //InternalStream.WriteBytes(Encoding.ASCII.GetBytes("8BIM")); //Chunk type, 4 bytes.
                    //InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)0x0)); //Image Resource Id, 2 bytes.
                    //InternalStream.WriteBytes(new byte[] { 0, 0 });         

                    //Save the position

                    //Layers and Masks list. XX bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //The size of the layer and global mask block, 4 bytes. TODO

                    InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //The size of the layer block, 4 bytes. TODO
                    InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)0)); //The layer count, 2 bytes. TODO
                }

                InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //Top, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //Left, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)Height)); //Bottom, 4 bytes.
                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)Width)); //Right, 4 bytes.

                InternalStream.WriteInt16(BitHelper.ConvertEndian((short)4)); //Number of channels, 2 bytes.
                for (int i = -1; i < 4; i++)
                {
                    InternalStream.WriteInt16(BitHelper.ConvertEndian((short)i)); //Channel ID, 2 bytes.
                    InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //Data lenght of channel, 4 bytes. TODO         
                }


                InternalStream.WriteBytes(Encoding.ASCII.GetBytes("8BIM")); //Blend mode signature, 4 bytes.
                InternalStream.WriteInt32(BitHelper.ConvertEndian(0x6e6f726d)); //Blend mode value, Normal, 4 bytes.
                InternalStream.WriteByte(255); //Opacity, 1 byte.
                InternalStream.WriteByte(0); //Clipping, 1 byte.
                InternalStream.WriteByte(10); //Flags, Visible = true, 1 byte.
                InternalStream.WriteByte(0); //Filler, 1 byte.

                InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //Extra data lenght, 4 bytes. TODO
                InternalStream.WriteInt32(BitHelper.ConvertEndian(0)); //Layer mask size, 4 bytes.
                InternalStream.WriteInt32(BitHelper.ConvertEndian(0)); //Blending ranges size, 4 bytes. TODO: Check if it's possible o have this as zero.

                var name = $"Frame {0}".Truncate(255);
                InternalStream.WriteByte((byte)name.Length); //Layer name size, 1 byte.
                InternalStream.WriteString1252(name); //Layer name size, 1 byte.

                var padding = 4 - (name.Length + 1) % 4;
                if (padding != 4)  //There's zero padding if equals to 4.
                    InternalStream.Position += padding;

                //For each Aditional Layer Information:
                //InternalStream.WriteBytes(Encoding.ASCII.GetBytes("8BIM")); //Aditional Layer Information signature, 4 bytes.
                //InternalStream.WriteBytes(Encoding.ASCII.GetBytes("shmd")); //ALI ID, 4 bytes.

                var a = new TiffBitmapEncoder { Compression = TiffCompressOption.None };
                a.Frames.Add(BitmapFrame.Create(path.SourceFrom()));

                using (var ms = new MemoryStream())
                {
                    a.Save(ms);
                    ImageDataList.Add(ms.ToArray());
                }


            }
        }


        public void Dispose()
        {
            //Write down the image bytes.

            InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)0)); //Global mask information size, 4 bytes.

            //Aditional info.

            //Precomposed image data?
        }
    }
}