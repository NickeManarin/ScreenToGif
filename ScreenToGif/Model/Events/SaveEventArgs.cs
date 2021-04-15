using System.Windows;

namespace ScreenToGif.Model.Events
{
    public class SaveEventArgs : RoutedEventArgs
    {
        public SaveEventArgs(RoutedEvent routedEvent) : base(routedEvent)
        { }
    }
}