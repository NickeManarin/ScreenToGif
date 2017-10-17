using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Basic class of a Hideable TabControl.
    /// </summary>
    public class HideableTabControl : TabControl
    {
        #region Variables

        private Button _hideButton;
        private Button _optionsButton;
        private Button _feedbackButton;
        private Button _helpButton;
        private TabPanel _tabPanel;
        private Border _border;

        #endregion

        static HideableTabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HideableTabControl), new FrameworkPropertyMetadata(typeof(HideableTabControl)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _tabPanel = Template.FindName("TabPanel", this) as TabPanel;
            _border = Template.FindName("ContentBorder", this) as Border;

            _optionsButton = Template.FindName("OptionsButton", this) as ImageButton;
            _feedbackButton = Template.FindName("FeedbackButton", this) as ImageButton;
            _helpButton = Template.FindName("HelpButton", this) as ImageButton;
            _hideButton = Template.FindName("HideGridButton", this) as Button;

            //Options
            if (_optionsButton != null)
                _optionsButton.Click += (sender, args) =>
                {
                    var options = new Options { Owner = Window.GetWindow(this) };
                    options.ShowDialog();
                };

            //Feedback
            if (_feedbackButton != null)
                _feedbackButton.Click += async (sender, args) =>
                {
                    var feed = new Feedback { Owner = Window.GetWindow(this) };

                    if (feed.ShowDialog() != true)
                        return;

                    var window = feed.Owner as Editor;

                    if (window != null)
                        await window.SendFeedback();
                };

            //Help
            if (_helpButton != null)
                _helpButton.Click += (sender, args) =>
                {
                    try
                    {
                        Process.Start("https://github.com/NickeManarin/ScreenToGif/wiki/Help");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Openning the Help");
                    }
                };

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
        }

        #region Events

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
            var selected = sender as TabItem;

            if (selected != null)
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
            //Glass.GlassColor.GetBrightness() <= 137
            //var color = Glass.GlassColor;
            //var isdark2 = (2*Glass.GlassColor.R) + (5*Glass.GlassColor.G) + Glass.GlassColor.B;
            //477, 480, 484, 495, 499, 502, 505, 513, 572, 598, 601 = light back
            //0, 251, 263, 272, 276, 281, 299, 310, 334, 340, 345, 350, 370, 421, 428, 436, 441, 442, 449, 450, 470, 472, 473, 475, 476, 478 = dark back (482, 494) 

            var darkForeground = !SystemParameters.IsGlassEnabled || !Other.IsWin8OrHigher() || Glass.GlassColor.GetBrightness() > 137 || !isActivated;           
            var showBackground = !Other.IsWin8OrHigher();

            //Console.WriteLine("!IsGlassEnabled: " + !SystemParameters.IsGlassEnabled);
            //Console.WriteLine("!UsesColor: " + !Glass.UsesColor);
            //Console.WriteLine("GlassColorBrightness <= 137: " + (Glass.GlassColor.GetBrightness() <= 137));
            //Console.WriteLine("!IsWin8: " + !Other.IsWin8OrHigher());
            //Console.WriteLine("IsActivated: " + isActivated);
            //Console.WriteLine("IsDark: " + isDark);

            //Update each tab.
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
            if (_optionsButton != null)
                _optionsButton.Foreground = !darkForeground && UserSettings.All.EditorExtendChrome ? Brushes.GhostWhite : Brushes.Black;

            if (_feedbackButton != null)
                _feedbackButton.Foreground = !darkForeground && UserSettings.All.EditorExtendChrome ? Brushes.GhostWhite : Brushes.Black;

            if (_helpButton != null)
                _helpButton.Foreground = !darkForeground && UserSettings.All.EditorExtendChrome ? Brushes.GhostWhite : Brushes.Black;

            #region Tests

            //Console.WriteLine(SystemParameters.WindowGlassColor + " - " + SystemParameters.WindowGlassColor.GetBrightness2() + " • " + 
            //    SystemParameters.WindowGlassColor.GetBrightness() + " - " +
            //    SystemParameters.WindowGlassColor.ConvertRgbToHsv().V);

            //Console.WriteLine(Glass.GlassColor.GetBrightness() + " " + isDark);

            #endregion
        }
    }
}
