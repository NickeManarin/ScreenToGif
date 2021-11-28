using System.Windows;

namespace ScreenToGif.Domain.Events;

public delegate void ManipulatedEventHandler(object sender, ManipulatedEventArgs args);

public class ManipulatedEventArgs : RoutedEventArgs
{
    public double AngleDifference { get; private set; }

    public double WidthDifference { get; private set; }

    public double HeightDifference { get; private set; }

    public double TopDifference { get; private set; }

    public double LeftDifference { get; private set; }

    public ManipulatedEventArgs(RoutedEvent routedEvent, double angleDifference, double widthDifference, double heightDifference, double topDifference, double leftDifference) : base(routedEvent)
    {
        AngleDifference = angleDifference;
        WidthDifference = widthDifference;
        HeightDifference = heightDifference;
        TopDifference = topDifference;
        LeftDifference = leftDifference;
    }

    public ManipulatedEventArgs(RoutedEvent routedEvent, double angleDifference) : base(routedEvent)
    {
        AngleDifference = angleDifference;
    }

    public ManipulatedEventArgs(RoutedEvent routedEvent, double widthDifference, double heightDifference, double topDifference, double leftDifference) : base(routedEvent)
    {
        WidthDifference = widthDifference;
        HeightDifference = heightDifference;
        TopDifference = topDifference;
        LeftDifference = leftDifference;
    }
}