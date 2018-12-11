using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls.Shapes
{
    internal class Triangle : Shape
    {
        protected override Geometry DefiningGeometry => Geometry.Parse($"M {Width/2d},{StrokeThickness / 2d} L{Width - (StrokeThickness / 2d)},{Height - (StrokeThickness / 2d)} L {StrokeThickness / 2d},{Height - (StrokeThickness / 2d)} z");
    }
}