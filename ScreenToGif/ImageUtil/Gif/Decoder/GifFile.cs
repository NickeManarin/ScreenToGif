using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenToGif.ImageUtil.Gif.Decoder
{
    public enum FrameDisposalMethod
    {
        None = 0,
        DoNotDispose = 1,
        RestoreBackground = 2,
        RestorePrevious = 3
    }

    public class FrameMetadata
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public TimeSpan Delay { get; set; }
        public FrameDisposalMethod DisposalMethod { get; set; }
    }

    public class GifFile
    {
        public GifHeader Header { get; private set; }
        public GifColor[] GlobalColorTable { get; set; }
        public IList<GifFrame> Frames { get; set; }
        public IList<GifExtension> Extensions { get; set; }
        public ushort RepeatCount { get; set; }

        private GifFile()
        {}

        internal static GifFile ReadGifFile(Stream stream, bool metadataOnly)
        {
            var file = new GifFile();
            file.Read(stream, metadataOnly);
            return file;
        }

        private void Read(Stream stream, bool metadataOnly)
        {
            Header = GifHeader.ReadHeader(stream);

            if (Header.LogicalScreenDescriptor.HasGlobalColorTable)
                GlobalColorTable =
                    GifHelpers.ReadColorTable(stream, Header.LogicalScreenDescriptor.GlobalColorTableSize);

            ReadFrames(stream, metadataOnly);

            var netscapeExtension = Extensions.OfType<GifApplicationExtension>().FirstOrDefault(GifHelpers.IsNetscapeExtension);

            if (netscapeExtension != null)
                RepeatCount = GifHelpers.GetRepeatCount(netscapeExtension);
            else
                RepeatCount = 1;
        }

        private void ReadFrames(Stream stream, bool metadataOnly)
        {
            var frames = new List<GifFrame>();
            var controlExtensions = new List<GifExtension>();
            var specialExtensions = new List<GifExtension>();

            while (true)
            {
                var block = GifBlock.ReadBlock(stream, controlExtensions, metadataOnly);

                if (block.Kind == GifBlockKind.GraphicRendering)
                    controlExtensions = new List<GifExtension>();

                if (block is GifFrame)
                {
                    frames.Add((GifFrame)block);
                }
                else if (block is GifExtension)
                {
                    var extension = (GifExtension)block;

                    switch (extension.Kind)
                    {
                        case GifBlockKind.Control:
                            controlExtensions.Add(extension);
                            break;
                        case GifBlockKind.SpecialPurpose:
                            specialExtensions.Add(extension);
                            break;
                    }
                }
                else if (block is GifTrailer)
                {
                    break;
                }
            }

            Frames = frames.AsReadOnly();
            Extensions = specialExtensions.AsReadOnly();
        }
    }
}