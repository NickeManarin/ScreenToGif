using System;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

///<summary>
///A panel that organizes it's inner elements in a circular fashion.
///</summary>
public class RadialPanel : Panel
{
    /// <summary>
    /// Measure each children and give as much room as they want.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (UIElement elem in Children)
        {
            //Give Infinite size as the available size for all the children.
            elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        return base.MeasureOverride(availableSize);
    }

    /// <summary>
    /// Arrange all children based on the geometric equations for the circle.
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
            return finalSize;

        var angle = 0d;

        //Degrees converted to Radian by multiplying with PI/180
        var incrementalAngularSpace = (360.0 / Children.Count) * (Math.PI / 180);

        //An approximate radii based on the available size , obviusly a better approach is needed here.
        var radiusX = finalSize.Width / 2.4;
        var radiusY = finalSize.Height / 2.4;

        foreach (UIElement elem in Children)
        {
            //Calculate the point on the circle for the element.
            var childPoint = new Point(Math.Cos(angle) * radiusX, -Math.Sin(angle) * radiusY);

            //Offsetting the point to the available rectangular area which is FinalSize.
            var actualChildPoint = new Point(finalSize.Width / 2 + childPoint.X - elem.DesiredSize.Width / 2, finalSize.Height / 2 + childPoint.Y - elem.DesiredSize.Height / 2);

            //Call Arrange method on the child element by giving the calculated point as the placementPoint.
            elem.Arrange(new Rect(actualChildPoint.X, actualChildPoint.Y, elem.DesiredSize.Width, elem.DesiredSize.Height));

            //Calculate the new _angle for the next element.
            angle += incrementalAngularSpace;
        }

        return finalSize;
    }
}