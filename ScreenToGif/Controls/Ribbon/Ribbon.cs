using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ScreenToGif.Util;

namespace ScreenToGif.Controls.Ribbon
{
    //Groups of elements, ordered
    //Groups can be hidden by type/tags
    public class Ribbon : TabControl
    {
        public enum Modes
        {
            Ribbon,
            Menu
        }

        #region Variables

        private Button _hideButton;
        private ImageMenuItem _extrasMenuItem;
        private TabPanel _tabPanel;
        private Border _border;
        private ImageToggleButton _notificationButton;
        private NotificationBox _notificationBox;

        #endregion

        #region Properties

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(Modes), typeof(Ribbon),
            new FrameworkPropertyMetadata(Modes.Ribbon, FrameworkPropertyMetadataOptions.AffectsRender, Mode_Changed));

        public static readonly DependencyProperty ExtrasMenuProperty = DependencyProperty.Register(nameof(ExtrasMenu), typeof(List<FrameworkElement>), typeof(Ribbon),
            new PropertyMetadata(new List<FrameworkElement>()));

        public Modes Mode
        {
            get => (Modes)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public List<FrameworkElement> ExtrasMenu
        {
            get => (List<FrameworkElement>)GetValue(ExtrasMenuProperty);
            set => SetValue(ExtrasMenuProperty, value);
        }

        #endregion

        static Ribbon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(typeof(Ribbon)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //Change the style of the inner controls based on the mode

            _tabPanel = Template.FindName("TabPanel", this) as TabPanel;
            _border = Template.FindName("ContentBorder", this) as Border;

            _notificationButton = Template.FindName("NotificationsButton", this) as ImageToggleButton;
            _notificationBox = Template.FindName("NotificationBox", this) as NotificationBox;
            _extrasMenuItem = Template.FindName("ExtrasMenuItem", this) as ImageMenuItem;

            _hideButton = Template.FindName("HideGridButton", this) as Button;

            //Hide tab
            if (_hideButton != null)
                _hideButton.Click += HideButton_Clicked;

            //Show tab (if hidden)
            if (_tabPanel != null)
            {
                foreach (TabItem tabItem in _tabPanel.Children)
                    tabItem.PreviewMouseDown += TabItem_PreviewMouseDown;

                _tabPanel.PreviewMouseWheel += TabControl_PreviewMouseWheel;
            }

            UpdateVisual();
            AnimateOrNot();
        }

        #region Events

        private static void Mode_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as Ribbon;
            element?.SwitchMode();
        }

        private void TabControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (SelectedIndex < Items.Count - 1)
                    SelectedIndex++;
                else
                    SelectedIndex = 0;
            }
            else
            {

                if (SelectedIndex > 0)
                    SelectedIndex--;
                else
                    SelectedIndex = Items.Count - 1;
            }

            if (!_tabPanel.Children[SelectedIndex].IsEnabled)
            {
                if (_tabPanel.Children.OfType<TabItem>().All(x => !x.IsEnabled))
                {
                    SelectedIndex = -1;
                    return;
                }

                TabControl_PreviewMouseWheel(sender, e);
            }

            TabItem_PreviewMouseDown(sender, null);
            ChangeVisibility();
        }

        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TabItem selected)
                selected.IsSelected = true;

            if (Math.Abs(_border.ActualHeight - 100) < 0)
                return;

            var animation = new DoubleAnimation(_border.ActualHeight, 100, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 8 }
            };
            _border.BeginAnimation(HeightProperty, animation);

            var opacityAnimation = new DoubleAnimation(_border.Opacity, 1, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 8 }
            };
            _border.BeginAnimation(OpacityProperty, opacityAnimation);

            var visibilityAnimation = new ObjectAnimationUsingKeyFrames();
            visibilityAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
            _hideButton.BeginAnimation(VisibilityProperty, visibilityAnimation);

            //Marging = 5,5,0,-1
            var marginAnimation = new ThicknessAnimation(_tabPanel.Margin, new Thickness(5, 5, 0, -1), new Duration(new TimeSpan(0, 0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 8 }
            };
            _tabPanel.BeginAnimation(MarginProperty, marginAnimation);
        }

        private void HideButton_Clicked(object sender, RoutedEventArgs routedEventArgs)
        {
            //ActualHeight = 0
            var animation = new DoubleAnimation(_border.ActualHeight, 0, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 8 }
            };
            _border.BeginAnimation(HeightProperty, animation);

            //Opacity = 0
            var opacityAnimation = new DoubleAnimation(_border.Opacity, 0, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 8 }
            };
            _border.BeginAnimation(OpacityProperty, opacityAnimation);

            //SelectedItem = null
            var objectAnimation = new ObjectAnimationUsingKeyFrames();
            objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(null, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            BeginAnimation(SelectedItemProperty, objectAnimation);

            //Visibility = Visibility.Collapsed
            var visibilityAnimation = new ObjectAnimationUsingKeyFrames();
            visibilityAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            _hideButton.BeginAnimation(VisibilityProperty, visibilityAnimation);

            //Marging = 5,5,0,5
            var marginAnimation = new ThicknessAnimation(_tabPanel.Margin, new Thickness(5, 5, 0, 5), new Duration(new TimeSpan(0, 0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 8 }
            };
            _tabPanel.BeginAnimation(MarginProperty, marginAnimation);
        }

        #endregion

        #region Methods

        internal void SwitchMode()
        {
            switch (Mode)
            {
                case Modes.Ribbon:
                {

                    break;
                }
                case Modes.Menu:
                {

                    break;
                }
            }
        }

        /// <summary>
        /// Changes the visibility of the Content.
        /// </summary>
        /// <param name="visible">True to show the Content.</param>
        public void ChangeVisibility(bool visible = true)
        {
            _border.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            _hideButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateVisual(bool isActivated = true)
        {
            //Shows only a white foreground when: 

            //var color = Glass.GlassColor;
            //var ness = Glass.GlassColor.GetBrightness();
            //var aa = color.ConvertRgbToHsv();

            var darkForeground = !SystemParameters.IsGlassEnabled || !Other.IsGlassSupported() || Glass.GlassColor.GetBrightness() > 973 || !isActivated;
            //var darkForeground = !SystemParameters.IsGlassEnabled || !Other.IsWin8OrHigher() || aa.V > 0.5 || !isActivated;           
            var showBackground = !Other.IsGlassSupported();

            //Console.WriteLine("!IsGlassEnabled: " + !SystemParameters.IsGlassEnabled);
            //Console.WriteLine("!UsesColor: " + !Glass.UsesColor);
            //Console.WriteLine("GlassColorBrightness <= 137: " + (Glass.GlassColor.GetBrightness() <= 137));
            //Console.WriteLine("!IsWin8: " + !Other.IsWin8OrHigher());
            //Console.WriteLine("IsActivated: " + isActivated);
            //Console.WriteLine("IsDark: " + isDark);

            //Update each tab.
            if (_tabPanel != null)
                foreach (var tab in _tabPanel.Children.OfType<AwareTabItem>())
                {
                    //To force the change.
                    if (tab.IsDark == !darkForeground)
                        tab.IsDark = !tab.IsDark;

                    if (tab.ShowBackground == showBackground)
                        tab.ShowBackground = !tab.ShowBackground;

                    tab.IsDark = !darkForeground;
                    tab.ShowBackground = showBackground;
                }

            //Update the buttons.
            if (_notificationButton != null)
            {
                _notificationButton.DarkMode = !darkForeground;
                _notificationButton.IsOverNonClientArea = UserSettings.All.EditorExtendChrome;
            }

            if (_extrasMenuItem != null)
            {
                _extrasMenuItem.DarkMode = !darkForeground;
                _extrasMenuItem.IsOverNonClientArea = UserSettings.All.EditorExtendChrome;
            }
        }


        public void UpdateNotifications(int? id = null)
        {
            _notificationBox?.UpdateNotification(id);

            AnimateOrNot();
        }

        public EncoderListViewItem AddEncoding(int id)
        {
            //Display the popup and animate the button.
            _notificationButton.IsChecked = true;

            AnimateOrNot(true);

            return _notificationBox.AddEncoding(id);
        }

        public void UpdateEncoding(int? id = null, bool onlyStatus = false)
        {
            if (!onlyStatus)
                _notificationBox?.UpdateEncoding(id);

            AnimateOrNot();
        }

        public EncoderListViewItem RemoveEncoding(int id)
        {
            try
            {
                return _notificationBox.RemoveEncoding(id);
            }
            finally
            {
                AnimateOrNot();
            }
        }

        private void AnimateOrNot(bool add = false)
        {
            //Blink the button when an encoding is added.
            if (add && _notificationButton.FindResource("NotificationStoryboard") is Storyboard story)
            {
                story.Stop();
                story.Begin();
            }

            var anyProcessing = EncodingManager.Encodings.Any(s => s.Status == Status.Processing);
            var anyCompleted = EncodingManager.Encodings.Any(s => s.Status == Status.Completed);
            var anyFaulty = EncodingManager.Encodings.Any(s => s.Status == Status.Error);

            _notificationButton.Content = anyProcessing ? FindResource("Vector.Encoder") as Canvas :
                anyCompleted ? FindResource("Vector.Ok") as Canvas :
                anyFaulty ? FindResource("Vector.Cancel.Round") as Canvas : _notificationButton.Content;
            _notificationButton.IsImportant = anyProcessing;
            _notificationButton.SetResourceReference(ImageToggleButton.TextProperty, anyProcessing ? "S.Encoder.Encoding" : anyCompleted ? "S.Encoder.Completed" : anyFaulty ? "S.Encoder.Error" : "S.Notifications");

            if (anyProcessing || anyCompleted || anyFaulty)
                return;

            //Animate the button for notifications, when there are no encodings.
            var most = NotificationManager.Notifications.Select(s => s.Kind).OrderByDescending(a => (int)a).FirstOrDefault();

            _notificationButton.Content = FindResource(StatusBand.KindToString(most)) as Canvas;
            _notificationButton.IsImportant = most != StatusType.None;
            _notificationButton.SetResourceReference(ImageToggleButton.TextProperty, "S.Notifications");

            if (most != StatusType.None)
                (_notificationButton.FindResource("NotificationStoryboard") as Storyboard)?.Begin();
        }

        #endregion
    }
}