using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    internal class NotificationListControl : Control
    {
        #region Variables

        StackPanel _mainStackPanel;
        Hyperlink _mainHyperlink;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty HasNotificationsProperty = DependencyProperty.Register("HasNotifications", typeof(bool), typeof(NotificationListControl), new PropertyMetadata(false));

        public bool HasNotifications
        {
            get => (bool)GetValue(HasNotificationsProperty);
            set => SetValue(HasNotificationsProperty, value);
        }

        #endregion

        static NotificationListControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationListControl), new FrameworkPropertyMetadata(typeof(NotificationListControl)));
        }

        public override void OnApplyTemplate()
        {
            _mainStackPanel = GetTemplateChild("MainStackPanel") as StackPanel;
            _mainHyperlink = GetTemplateChild("MainHyperlink") as Hyperlink;

            if (_mainHyperlink != null)
                _mainHyperlink.Click += MainHyperlink_Click;

            base.OnApplyTemplate();

            UpdateList();
        }

        private void MainHyperlink_Click(object sender, RoutedEventArgs args)
        {
            //Remove all notifications.
            NotificationManager.RemoveAllNotifications();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs args)
        {
            var band = sender as StatusBand;

            if (band == null)
                return;

            NotificationManager.RemoveNotification(band.Id);
        }

        public void UpdateList()
        {
            if (_mainStackPanel == null)
            {
                HasNotifications = false;
                return;
            }

            var current = _mainStackPanel.Children.OfType<StatusBand>();

            foreach (var band in current)
                band.Dismissed -= RemoveItem_Click;

            _mainStackPanel.Children.Clear();

            foreach (var notification in NotificationManager.Notifications)
            {
                var item = new StatusBand
                {
                    Id = notification.Id,
                    Text = notification.Text,
                    Type = notification.Kind,
                    Image = TryFindResource(StatusBand.KindToString(notification.Kind)) as Canvas,
                    Action = notification.Action,
                    IsLink = notification.Action != null,
                    Visibility = Visibility.Visible
                };

                item.Dismissed += RemoveItem_Click;

                _mainStackPanel.Children.Add(item);
            }

            HasNotifications = _mainStackPanel.Children.Count > 0;
        }
    }
}