using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.ModelEx.Layers
{
    internal class ShapeLayer : LayerModel
    {
        public ShapeLayer()
        {
            Type = LayerType.Shape;
        }

        public Shape Shape { get; set; }

        //Is this necessary?
        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; }
    }
}