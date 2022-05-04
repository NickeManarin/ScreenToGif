using System.Globalization;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls.Shapes;

internal class Triangle : Shape
{
    protected override Geometry DefiningGeometry => Geometry.Parse($"M {(Width/2d).ToString(CultureInfo.InvariantCulture)},{(StrokeThickness / 2d).ToString(CultureInfo.InvariantCulture)} " +
                                                                   $"L{(Width - (StrokeThickness / 2d)).ToString(CultureInfo.InvariantCulture)},{(Height - (StrokeThickness / 2d)).ToString(CultureInfo.InvariantCulture)} " +
                                                                   $"L {(StrokeThickness / 2d).ToString(CultureInfo.InvariantCulture)},{(Height - (StrokeThickness / 2d)).ToString(CultureInfo.InvariantCulture)} z");
}