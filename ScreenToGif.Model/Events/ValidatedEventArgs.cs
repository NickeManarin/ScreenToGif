using System.Windows;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Events
{
    public delegate void ValidatedEventHandler(object sender, ValidatedEventArgs e);

    public class ValidatedEventArgs : RoutedEventArgs
    {
        public string MessageKey { get; set; }

        public StatusReasons Reason { get; set; }

        public Action Action { get; set; }


        public ValidatedEventArgs(string messageKey, StatusReasons reason, Action action = null)
        {
            MessageKey = messageKey;
            Reason = reason;
            Action = action;
        }

        public ValidatedEventArgs(RoutedEvent routedEvent, string messageKey, StatusReasons reason, Action action = null) : base(routedEvent)
        {
            MessageKey = messageKey;
            Reason = reason;
            Action = action;
        }
    }
}