using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ScreenToGif.Util;

namespace ScreenToGif.Controls.Ribbon
{
    public class Ribbon : TabControl
    {
        public enum Modes
        {
            Ribbon,
            Menu
        }

        #region Variables

        private Button _hideButton;
        private SideScrollViewer _tabScrollViewer;
        private StackPanel _tabPanel;
        private Border _contentBorder;
        private ContentPresenter _contentPresenter;
        private ExtendedToggleButton _notificationButton;
        private NotificationBox _notificationBox;
        private int _accumulatedDelta = 0;

        #endregion

        #region Properties

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(Modes), typeof(Ribbon), 
            new FrameworkPropertyMetadata(Modes.Ribbon, FrameworkPropertyMetadataOptions.AffectsRender, Mode_Changed));

        public static DependencyProperty IsDisplayingContentProperty = DependencyProperty.Register(nameof(IsDisplayingContent), typeof(bool), typeof(Ribbon), new PropertyMetadata(true));
        
        public static DependencyProperty OptionsCommandProperty = DependencyProperty.Register(nameof(OptionsCommand), typeof(ICommand), typeof(Ribbon), new PropertyMetadata(null));

        public static DependencyProperty FeedbackCommandProperty = DependencyProperty.Register(nameof(FeedbackCommand), typeof(ICommand), typeof(Ribbon), new PropertyMetadata(null));

        public static DependencyProperty TroubleshootCommandProperty = DependencyProperty.Register(nameof(TroubleshootCommand), typeof(ICommand), typeof(Ribbon), new PropertyMetadata(null));

        public static DependencyProperty HelpCommandProperty = DependencyProperty.Register(nameof(HelpCommand), typeof(ICommand), typeof(Ribbon), new PropertyMetadata(null));


        public Modes Mode
        {
            get => (Modes)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public bool IsDisplayingContent
        {
            get => (bool)GetValue(IsDisplayingContentProperty);
            set => SetValue(IsDisplayingContentProperty, value);
        }

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


        static Ribbon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(typeof(Ribbon)));
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _tabScrollViewer = Template.FindName("TabPanelScrollViewer", this) as SideScrollViewer;
            _tabPanel = Template.FindName("TabPanel", this) as StackPanel;
            _contentBorder = Template.FindName("ContentBorder", this) as Border;
            _contentPresenter = Template.FindName("ContentPresenter", this) as ContentPresenter;

            _notificationButton = Template.FindName("NotificationsButton", this) as ExtendedToggleButton;
            _notificationBox = Template.FindName("NotificationBox", this) as NotificationBox;
            _hideButton = Template.FindName("HideGridButton", this) as Button;

            //Hide button.
            if (_hideButton != null)
                _hideButton.Click += HideButton_Clicked;

            if (_tabPanel != null)
            {
                _tabPanel.PreviewMouseWheel += TabControl_PreviewMouseWheel;
                _tabPanel.PreviewMouseLeftButtonDown += TabPanel_MouseLeftButtonDown;
            }
            
            SelectionChanged += Ribbon_SelectionChanged;
            
            if (_notificationButton != null)
                _notificationButton.Checked += NotificationButton_Checked;

            AnimateOrNot();
        }

        #region Events

        private static void Mode_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as Ribbon;
            element?.SwitchMode();
        }

        private void Ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedIndex < 0)
                return;

            //If the ribbon content is already being displayed.
            if (IsDisplayingContent)
            {
                //Increase the size and opacity of the content border.
                _contentPresenter.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(-20, 0, 0, 0), new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(0, 0, 0, 1)))
                {
                    EasingFunction = new PowerEase { Power = 9 }
                });

                _contentPresenter.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(new TimeSpan(0, 0, 0, 1)))
                {
                    EasingFunction = new PowerEase { Power = 9 }
                });
                return;
            }

            if (_tabPanel.Children[SelectedIndex] is RibbonTab tab)
                tab.DisplayAccent = false;

            //Increase the size and opacity of the content border.
            _contentBorder.BeginAnimation(HeightProperty, new DoubleAnimation(_contentBorder.ActualHeight, 98, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 9 }
            });

            _contentBorder.BeginAnimation(OpacityProperty, new DoubleAnimation(_contentBorder.Opacity, 1, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 9 }
            });

            //Show the "Hide panel" button.
            _hideButton.BeginAnimation(VisibilityProperty, new ObjectAnimationUsingKeyFrames
            {
                KeyFrames = { new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))) }
            });

            //Decrease the size of the TabItem list. 
            _tabScrollViewer.BeginAnimation(MarginProperty, new ThicknessAnimation(_tabPanel.Margin, new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 9 }
            });

            _tabPanel.Children[SelectedIndex].BeginAnimation(RibbonTab.DisplayAccentProperty, new BooleanAnimationUsingKeyFrames
            {
                KeyFrames = { new DiscreteBooleanKeyFrame(true, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))) }
            });

            IsDisplayingContent = true;
        }

        private void TabPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                HidePanel();
        }

        private void TabControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                //Tone down the delta. TODO: Test with a mouse.
                _accumulatedDelta += Math.Min(e.Delta, 100);

                if (_accumulatedDelta > 120)
                {
                    #region Advances to the next tab item

                    if (SelectedIndex > 0)
                        SelectedIndex--;
                    else
                        SelectedIndex = Items.OfType<TabItem>().Count(w => w.IsEnabled) - 1;

                    _accumulatedDelta = 0;

                    SkipDisabledTab();

                    #endregion
                }
                else if (_accumulatedDelta < -120)
                {
                    #region Backs to the previous tab item

                    if (SelectedIndex < Items.OfType<TabItem>().Count(w => w.IsEnabled) - 1)
                        SelectedIndex++;
                    else
                        SelectedIndex = 0;

                    _accumulatedDelta = 0;

                    SkipDisabledTab(false);

                    #endregion
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void HideButton_Clicked(object sender, RoutedEventArgs routedEventArgs)
        {
            HidePanel();
        }

        private void NotificationButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (_notificationButton.FindResource("NotificationStoryboard") is Storyboard story)
                story.Stop();
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

        private void HidePanel()
        {
            //ActualHeight = 0
            _contentBorder.BeginAnimation(HeightProperty, new DoubleAnimation(_contentBorder.ActualHeight, 0, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 9 }
            });

            //Opacity = 0
            _contentBorder.BeginAnimation(OpacityProperty, new DoubleAnimation(_contentBorder.Opacity, 0, new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 9 }
            });

            //SelectedItem = null
            BeginAnimation(SelectedItemProperty, new ObjectAnimationUsingKeyFrames
            {
                KeyFrames = { new DiscreteObjectKeyFrame(null, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))) }
            });

            //Hide the "Hide panel" button.
            _hideButton.BeginAnimation(VisibilityProperty, new ObjectAnimationUsingKeyFrames
            {
                KeyFrames = { new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))) }
            });

            //Marging = 0,0,0,5
            _tabScrollViewer.BeginAnimation(MarginProperty, new ThicknessAnimation(_tabPanel.Margin, new Thickness(0, 0, 0, 2), new Duration(new TimeSpan(0, 0, 0, 1)))
            {
                EasingFunction = new PowerEase { Power = 9 }
            });

            SelectedIndex = -1;
            IsDisplayingContent = false;
        }

        /// <summary>
        /// Checks if the tab selection is valid.
        /// If not, it tries to selected another tab.
        /// </summary>
        /// <param name="next">True if the navigation should go forwards.</param>
        private void SkipDisabledTab(bool next = true)
        {
            //If no tab was selected, ignore.
            if (SelectedIndex < 0)
                return;

            //If the current tab is disabled.
            if (!_tabPanel.Children[SelectedIndex].IsEnabled || _tabPanel.Children[SelectedIndex].Visibility != Visibility.Visible)
            {
                //If all tabs are disabled, remove selection.
                if (_tabPanel.Children.OfType<TabItem>().All(x => !x.IsEnabled))
                {
                    SelectedIndex = -1;
                    return;
                }

                if (next)
                {
                    //Tries to go forward.
                    if (SelectedIndex > 0)
                        SelectedIndex--;
                    else
                        SelectedIndex = Items.Count - 1;
                    return;
                }

                //Tries to go back.
                if (SelectedIndex < Items.Count - 1)
                    SelectedIndex++;
                else
                    SelectedIndex = 0;
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

            var anyProcessing = EncodingManager.Encodings.Any(s => s.Status == Status.Processing);
            var anyCompleted = EncodingManager.Encodings.Any(s => s.Status == Status.Completed);
            var anyFaulty = EncodingManager.Encodings.Any(s => s.Status == Status.Error);

            _notificationButton.Icon = anyProcessing ? FindResource("Vector.Progress") as Brush :
                anyCompleted ? FindResource("Vector.Ok.Round") as Brush :
                anyFaulty ? FindResource("Vector.Cancel.Round") as Brush : _notificationButton.Icon;
            _notificationButton.IsImportant = anyProcessing;
            _notificationButton.SetResourceReference(ExtendedToggleButton.TextProperty, anyProcessing ? "S.Encoder.Encoding" : anyCompleted ? "S.Encoder.Completed" : anyFaulty ? "S.Encoder.Error" : "S.Notifications");

            if (anyProcessing || anyCompleted || anyFaulty)
                return;

            //Animate the button for notifications, when there are no encodings.
            var most = NotificationManager.Notifications.Select(s => s.Kind).OrderByDescending(a => (int)a).FirstOrDefault();

            _notificationButton.Icon = FindResource(StatusBand.KindToString(most)) as Brush;
            _notificationButton.IsImportant = most != StatusType.None;
            _notificationButton.SetResourceReference(ExtendedToggleButton.TextProperty, "S.Notifications");

            if (story != null)
            {
                story.Stop();

                if (most != StatusType.None)
                    story.Begin();
            }
        }

        #endregion
    }
}