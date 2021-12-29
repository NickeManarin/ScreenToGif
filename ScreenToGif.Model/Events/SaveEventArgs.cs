using System.Windows;

namespace ScreenToGif.Domain.Events
{
    public class SaveEventArgs : RoutedEventArgs
    {
        public SaveEventArgs(RoutedEvent routedEvent) : base(routedEvent)
        { }
    }
}