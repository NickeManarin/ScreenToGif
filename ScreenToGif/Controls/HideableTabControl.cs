using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Basic class of a Hideable TabControl.
    /// </summary>
    public class HideableTabControl : TabControl
    {
        #region Variables

        private Button _button;
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

            _button = Template.FindName("HideGridButton", this) as Button;
            _tabPanel = Template.FindName("TabPanel", this) as TabPanel;
            _border = Template.FindName("ContentBorder", this) as Border;


            if (_button != null)
                _button.PreviewMouseUp += Button_Clicked;

            if (_tabPanel != null)
            {
                foreach (TabItem tabItem in _tabPanel.Children)
                {
                    tabItem.PreviewMouseDown += TabItem_PreviewMouseDown;
                }

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

            var animation = new DoubleAnimation(_border.ActualHeight, 100, new Duration(new TimeSpan(0, 0, 0, 1)));
            animation.EasingFunction = new PowerEase() { Power = 8 };
            _border.BeginAnimation(HeightProperty, animation);

            var opacityAnimation = new DoubleAnimation(_border.Opacity, 1, new Duration(new TimeSpan(0, 0, 0, 1)));
            opacityAnimation.EasingFunction = new PowerEase() { Power = 8 };
            _border.BeginAnimation(OpacityProperty, opacityAnimation);

            var visibilityAnimation = new ObjectAnimationUsingKeyFrames();
            visibilityAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
            _button.BeginAnimation(VisibilityProperty, visibilityAnimation);

            //Marging = 5,0,0,-1
            var marginAnimation = new ThicknessAnimation(_tabPanel.Margin, new Thickness(5, 0, 0, -1), new Duration(new TimeSpan(0, 0, 0, 0, 1)));
            marginAnimation.EasingFunction = new PowerEase() { Power = 8 };
            _tabPanel.BeginAnimation(MarginProperty, marginAnimation);
        }

        private void Button_Clicked(object sender, MouseButtonEventArgs e)
        {
            //ActualHeight = 0
            var animation = new DoubleAnimation(_border.ActualHeight, 0, new Duration(new TimeSpan(0, 0, 0, 1)));
            animation.EasingFunction = new PowerEase() { Power = 8 };
            _border.BeginAnimation(HeightProperty, animation);

            //Opacity = 0
            var opacityAnimation = new DoubleAnimation(_border.Opacity, 0, new Duration(new TimeSpan(0, 0, 0, 1)));
            opacityAnimation.EasingFunction = new PowerEase() { Power = 8 };
            _border.BeginAnimation(OpacityProperty, opacityAnimation);

            //SelectedItem = null
            var objectAnimation = new ObjectAnimationUsingKeyFrames();
            objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(null, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            this.BeginAnimation(SelectedItemProperty, objectAnimation);

            //Visibility = Visibility.Collapsed
            var visibilityAnimation = new ObjectAnimationUsingKeyFrames();
            visibilityAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            _button.BeginAnimation(VisibilityProperty, visibilityAnimation);

            //Marging = 5,0,0,5
            var marginAnimation = new ThicknessAnimation(_tabPanel.Margin, new Thickness(5, 0, 0, 5), new Duration(new TimeSpan(0, 0, 0, 0, 1)));
            marginAnimation.EasingFunction = new PowerEase() { Power = 8 };
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
            _button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateVisual(bool isActivated = true)
        {
            var isDark = (!SystemParameters.IsGlassEnabled || !Glass.UsesColor || Glass.GlassColor.GetBrightness() <= 137) && isActivated || !Other.IsWin8OrHigher();

            //Console.WriteLine(SystemParameters.WindowGlassColor + " - " + SystemParameters.WindowGlassColor.GetBrightness2() + " • " + 
            //    SystemParameters.WindowGlassColor.GetBrightness() + " - " +
            //    SystemParameters.WindowGlassColor.ConvertRgbToHsv().V);

            //Console.WriteLine(Glass.GlassColor.GetBrightness() + " " + isDark);

            foreach (var tab in _tabPanel.Children.OfType<AwareTabItem>())
            {
                //To force the change.
                if (tab.IsDark == isDark)
                    tab.IsDark = !tab.IsDark;

                tab.IsDark = isDark;
            }
        }
    }
}
