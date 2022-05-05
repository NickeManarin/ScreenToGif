using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Controls;

/// <summary>
/// Basic class of a Hideable TabControl.
/// </summary>
public class HideableTabControl : TabControl
{
    #region Variables

    private Button _hideButton;
    private ExtendedMenuItem _extrasMenuItem;
    private TabPanel _tabPanel;
    private Border _border;
    private ExtendedToggleButton _notificationButton;
    private NotificationBox _notificationBox;

    #endregion

    #region Dependency Properties

    public static DependencyProperty OptionsCommandProperty = DependencyProperty.Register("OptionsCommand", typeof(ICommand), typeof(HideableTabControl), new PropertyMetadata(null));

    public static DependencyProperty FeedbackCommandProperty = DependencyProperty.Register("FeedbackCommand", typeof(ICommand), typeof(HideableTabControl), new PropertyMetadata(null));

    public static DependencyProperty TroubleshootCommandProperty = DependencyProperty.Register("TroubleshootCommand", typeof(ICommand), typeof(HideableTabControl), new PropertyMetadata(null));

    public static DependencyProperty HelpCommandProperty = DependencyProperty.Register("HelpCommand", typeof(ICommand), typeof(HideableTabControl), new PropertyMetadata(null));

    #endregion

    #region Properties

    public ICommand OptionsCommand
    {
        get => (ICommand)GetValue(OptionsCommandProperty);
        set => SetValue(OptionsCommandProperty, value);
    }

    public ICommand FeedbackCommand
    {
        get => (ICommand)GetValue(FeedbackCommandProperty);
        set => SetValue(FeedbackCommandProperty, value);
    }

    public ICommand TroubleshootCommand
    {
        get => (ICommand)GetValue(TroubleshootCommandProperty);
        set => SetValue(TroubleshootCommandProperty, value);
    }

    public ICommand HelpCommand
    {
        get => (ICommand)GetValue(HelpCommandProperty);
        set => SetValue(HelpCommandProperty, value);
    }

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

        _notificationButton = Template.FindName("NotificationsButton", this) as ExtendedToggleButton;
        _notificationBox = Template.FindName("NotificationBox", this) as NotificationBox;
        _extrasMenuItem = Template.FindName("ExtrasMenuItem", this) as ExtendedMenuItem;

        _hideButton = Template.FindName("HideGridButton", this) as Button;

        //Hide button.
        if (_hideButton != null)
            _hideButton.Click += HideButton_Clicked;

        //Show tab (if hidden).
        if (_tabPanel != null)
        {
            foreach (TabItem tabItem in _tabPanel.Children)
                tabItem.PreviewMouseDown += TabItem_PreviewMouseDown;

            _tabPanel.PreviewMouseWheel += TabControl_PreviewMouseWheel;
        }

        if (_notificationButton != null)
            _notificationButton.Checked += NotificationButton_Checked;

        UpdateVisual();
        AnimateOrNot();
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

        //Margin = 5,5,0,-1
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

        //Margin = 5,5,0,5
        var marginAnimation = new ThicknessAnimation(_tabPanel.Margin, new Thickness(5, 5, 0, 5), new Duration(new TimeSpan(0, 0, 0, 0, 1)))
        {
            EasingFunction = new PowerEase { Power = 8 }
        };
        _tabPanel.BeginAnimation(MarginProperty, marginAnimation);
    }

    private void NotificationButton_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (_notificationButton.FindResource("NotificationStoryboard") is Storyboard story)
            story.Stop();
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

        //var color = Glass.GlassColor;
        //var ness = Glass.GlassColor.GetBrightness();
        //var aa = color.ConvertRgbToHsv();

        //var darkForeground = !SystemParameters.IsGlassEnabled || !Other.IsGlassSupported() || Glass.GlassColor.GetBrightness() > 973 || !isActivated;
        var darkForeground = !SystemParameters.IsGlassEnabled || !isActivated;
        //var darkForeground = !SystemParameters.IsGlassEnabled || !Other.IsWin8OrHigher() || aa.V > 0.5 || !isActivated;
        var showBackground = true;// !Other.IsGlassSupported();

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

    public EncoderListViewItem AddEncoding(int id, bool isActive = false)
    {
        //Display the popup (if the editor is active) and animate the button.
        if (isActive)
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
        var story = _notificationButton.FindResource("NotificationStoryboard") as Storyboard;

        if (story != null)
        {
            story.Stop();

            //Blink the button when an encoding is added.
            if (add)
                story.Begin();
        }

        var anyProcessing = EncodingManager.Encodings.Any(s => s.Status == EncodingStatus.Processing);
        var anyCompleted = EncodingManager.Encodings.Any(s => s.Status == EncodingStatus.Completed);
        var anyFaulty = EncodingManager.Encodings.Any(s => s.Status == EncodingStatus.Error);

        _notificationButton.Icon = anyProcessing ? FindResource("Vector.Progress") as Brush :
            anyCompleted ? FindResource("Vector.Ok.Round") as Brush :
            anyFaulty ? FindResource("Vector.Cancel.Round") as Brush : _notificationButton.Icon;
        _notificationButton.IsImportant = anyProcessing;
        _notificationButton.SetResourceReference(ExtendedToggleButton.TextProperty, anyProcessing ? "S.Encoder.Encoding" : anyCompleted ? "S.Encoder.Completed" : anyFaulty? "S.Encoder.Error" : "S.Notifications");

        if (anyProcessing || anyCompleted || anyFaulty)
            return;

        //Animate the button for notifications, when there are no encodings.
        var most = NotificationManager.Notifications.Select(s => s.Kind).OrderByDescending(a => (int)a).FirstOrDefault();

        _notificationButton.Icon = TryFindResource(StatusBand.KindToString(most)) as Brush;
        _notificationButton.IsImportant = most != StatusType.None;
        _notificationButton.SetResourceReference(ExtendedToggleButton.TextProperty, "S.Notifications");

        if(story != null)
        {
            story.Stop();

            if (most != StatusType.None)
                story.Begin();
        }
    }
}