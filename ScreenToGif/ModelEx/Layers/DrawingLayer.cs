using System.Windows.Ink;

namespace ScreenToGif.ModelEx.Layers
{
    internal class DrawingLayer : LayerModel
    {
        public DrawingLayer()
        {
            Type = LayerType.Drawing;
        }

        public StrokeCollection StrokeCollection { get; set; }
    }
}