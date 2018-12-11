using System;
using System.IO;
using System.Text;
using ScreenToGif.ImageUtil.Gif.Encoder;
using ScreenToGif.ImageUtil.Psd.ImageResourceBlocks;
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
        /// Compress the image data?
        /// </summary>
        internal bool Compress { get; set; }

        /// <summary>
        /// Save the timeline data of the recording?
        /// </summary>
        internal bool SaveTimeline { get; set; }

        #endregion

        private ImageResources ImageResources { get; set; } = new ImageResources();

        private LayerAndMask LayerAndMask { get; set; } = new LayerAndMask();


        internal Psd(Stream stream, int repeatCount, int height, int width, bool compress = true, bool saveTimeline = true)
        {
            InternalStream = stream;
            RepeatCount = repeatCount;
            Height = height;
            Width = width;
            Compress = compress;
            SaveTimeline = saveTimeline;
        }

        internal void AddFrame(int index, string path, int delay = 66)
        {
            var reader = new PixelUtil(path.SourceFrom());
            reader.LockBits();

            var channelData = new ImageChannelData(reader.Depth, reader.Pixels, reader.Height, reader.Width, Compress); //TODO: Support for layers with multiple sizes.
            var layerData = new LayerRecord
            {
                Top = 0,
                Left = 0,
                Bottom = (uint)Height, // + top,
                Right = (uint)Width, // + left,
                Name = index.ToString()
            };

            reader.UnlockBitsWithoutCommit();

            //Add the lengths of the channels.
            for (var i = 0; i < channelData.ChannelList.Count; i++)
                layerData.Channels.Add((short)(i - 1), (int)channelData.ChannelList[i].Length + 2); //+ 2 bytes for the compression type.

            LayerAndMask.LayerInfo.ImageChannelDataList.Add(channelData);
            LayerAndMask.LayerInfo.LayerList.Add(layerData);

            //TODO: Add ImageResource info (timeline)
            //ImageResources.ImageResourceList.Add(new ImageResourceBlock(2, "shmd", null));
        }

        internal void Encode()
        {
            //Psd Header: 26 bytes.
            InternalStream.WriteBytes(Encoding.ASCII.GetBytes("8BPS")); //Chunk type, 4 bytes.
            InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)1)); //File version, 1 - PSD, 2 - PSB, 2 bytes.
            InternalStream.Position += 6; //Must be zero, 6 bytes.
            InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)4)); //Number of channels, ARGB, 2 bytes.
            InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)Height)); //Height of the image, 4 bytes.
            InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)Width)); //Width of the image, 4 bytes.
            InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)8)); //Number of bits per channel, 2 bytes.
            InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)3)); //The color mode of the file, 3 - RGB, 2 bytes.

            //Color mode data. 4 bytes.
            InternalStream.WriteUInt32(BitHelper.ConvertEndian(0u)); //The size of the color mode data block, 0 bytes for RGB mode, 4 bytes.

            //Image resources. XX bytes.
            InternalStream.WriteBytes(ImageResources.Content);

            //LayerAndMaskInformation. 4 + XX bytes.
            var layerAndMask = LayerAndMask.Content;
            InternalStream.WriteUInt32(BitHelper.ConvertEndian((uint)layerAndMask.Length)); //Length of the LayerAndMask block, 4 bytes.
            InternalStream.WriteBytes(layerAndMask); //Content of the LayerAndMask block.

            //ImageData. XX bytes.
            if (Compress)
            {
                InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)1)); //The type of encoding, PackBit/RLE, 2 bytes.
                foreach (var layer in LayerAndMask.LayerInfo.ImageChannelDataList)
                {
                    //Writes all byte counts for all the scan lines (rows * channels), with each count stored as a two-byte value.
                    foreach (var channel in layer.ChannelList)
                        foreach (var b in channel.RleCompressedContent)
                            InternalStream.WriteInt16(BitHelper.ConvertEndian((short)b.Length));

                    //Writes down each layer, in planar order: AAA RRR GGG BBB.
                    foreach (var channel in layer.ChannelList)
                        foreach (var b in channel.RleCompressedContent)
                            InternalStream.WriteBytes(b);

                    break;
                }
            }
            else
            {
                InternalStream.WriteUInt16(BitHelper.ConvertEndian((ushort)0)); //The type of encoding, Raw data, 2 bytes.
                foreach (var layer in LayerAndMask.LayerInfo.ImageChannelDataList)
                {
                    //Writes down each layer, in planar order: AAA RRR GGG BBB.
                    foreach (var channel in layer.ChannelList)
                        InternalStream.WriteBytes(channel.RawContent);

                    break;
                }
            }
        }

        public void Dispose()
        {
            //Writes down all data to the stream.
            Encode();
        }
    }
}