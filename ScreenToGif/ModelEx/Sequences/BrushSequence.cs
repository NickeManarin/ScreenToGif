using System.Windows.Media;

namespace ScreenToGif.ModelEx.Sequences
{
    public class BrushSequence : SizeableSequence
    {
        public Brush Brush { get; set; }


        public BrushSequence()
        {
            Type = Types.Brush;
        }
    }
}