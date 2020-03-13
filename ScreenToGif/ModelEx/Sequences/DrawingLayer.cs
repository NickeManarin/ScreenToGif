using System.Windows.Ink;

namespace ScreenToGif.ModelEx.Sequences
{
    public class DrawingLayer : Sequence
    {
        public StrokeCollection Strokes { get; set; }


        public DrawingLayer()
        {
            Type = Types.Drawing;
        }
    }
}