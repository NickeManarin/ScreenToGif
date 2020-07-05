using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Util
{
    internal class NotificationManager
    {
        public static List<Notification> Notifications { get; set; } = new List<Notification>();


        internal static void AddNotification(string text, StatusType kind, string tag, ICommand command = null, object commandParameter = null)
        {
            AddNotification(text, kind, tag, () => { command?.Execute(commandParameter); });
        }

        internal static void AddNotification(string text, StatusType kind, string tag, Action action = null)
        {
            var rand = new Random(Notifications.Count);
            var id = rand.Next();

            while (Notifications.Any(a => a.Id == id))
                id = rand.Next();

            Notifications.Add(new Notification { Id = id, Text = text, Kind = kind, Tag = tag, Action = action });

            Refresh();
        }


        internal static void RemoveNotification(int id)
        {
            Notifications.RemoveAll(a => a.Id == id);

            Refresh();
        }

        internal static void RemoveNotification(Predicate<Notification> match)
        {
            Notifications.RemoveAll(match);

            Refresh();
        }

        internal static void RemoveAllNotifications()
        {
            Notifications.Clear();

            Refresh();
        }

        /// <summary>
        /// Warns all windows that implement the INotification interface that the notification data was updated.
        /// </summary>
        internal static void Refresh()
        {
            foreach (var notification in Application.Current.Windows.OfType<INotification>())
                notification.NotificationUpdated();
        }
    }


    interface INotification
    {
        void NotificationUpdated();
    }

    internal class Notification
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public StatusType Kind { get; set; }

        public string Tag { get; set; }

        public UIElement Image { get; set; }

        public Action Action { get; set; }
    }
}