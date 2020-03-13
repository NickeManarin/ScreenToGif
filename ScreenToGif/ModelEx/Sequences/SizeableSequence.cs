namespace ScreenToGif.ModelEx.Sequences
{
    public class SizeableSequence : Sequence
    {
        public double Left { get; set; }

        public double Top { get; set; }
        
        public double Width { get; set; }

        public double Height { get; set; }

        public double Angle { get; set; }

        public double HorizontalDpi { get; set; }

        public double VerticalDpi { get; set; }
    }
}