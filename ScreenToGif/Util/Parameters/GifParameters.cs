using System.Windows.Media;

namespace ScreenToGif.Util.Parameters
{
    public class GifParameters : Parameters
    {
        public GifEncoderType EncoderType { get; set; }

        public int RepeatCount { get; set; }

        public bool DetectUnchangedPixels { get; set; }

        public Color? DummyColor { get; set; }

        public int Quality { get; set; }
    }
}
