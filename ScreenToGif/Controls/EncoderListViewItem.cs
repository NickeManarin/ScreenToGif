using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// ListViewItem used by the Encoder window.
    /// </summary>
    public class EncoderListViewItem : ListViewItem
    {
        #region Variables

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(25.0));

        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register("Percentage", typeof(double), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty CurrentFrameProperty = DependencyProperty.Register("CurrentFrame", typeof(int), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(1));

        public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register("FrameCount", typeof(int), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty ReasonProperty = DependencyProperty.Register("Reason", typeof(string), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty IdProperty = DependencyProperty.Register("Id", typeof(int), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(-1));

        public static readonly DependencyProperty TokenProperty = DependencyProperty.Register("Token", typeof(CancellationTokenSource), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register("IsIndeterminate", typeof(bool), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(Status), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(Status.Encoding));

        public static readonly DependencyProperty SizeInBytesProperty = DependencyProperty.Register("SizeInBytes", typeof(long), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata(0L));

        public static readonly DependencyProperty OutputPathProperty = DependencyProperty.Register("OutputPath", typeof(string), typeof(EncoderListViewItem), 
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty OutputFilenameProperty = DependencyProperty.Register("OutputFilename", typeof(string), typeof(EncoderListViewItem),
                new FrameworkPropertyMetadata(OutputFilename_PropertyChanged));

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the ListViewItem.
        /// </summary>
        [Description("The Image of the ListViewItem.")]
        public UIElement Image
        {
            get { return (UIElement)GetValue(ImageProperty); }
            set { SetCurrentValue(ImageProperty, value); }
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image.")]
        public double MaxSize
        {
            get { return (double)GetValue(MaxSizeProperty); }
            set { SetCurrentValue(MaxSizeProperty, value); }
        }

        /// <summary>
        /// The encoding percentage.
        /// </summary>
        [Description("The encoding percentage.")]
        public double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetCurrentValue(PercentageProperty, value); }
        }

        /// <summary>
        /// The current frame being processed.
        /// </summary>
        [Description("The frame count.")]
        public int CurrentFrame
        {
            get { return (int)GetValue(CurrentFrameProperty); }
            set
            {
                SetCurrentValue(CurrentFrameProperty, value);

                if (CurrentFrame == 0)
                {
                    Percentage = 0;
                    return;
                }

                // 100% = FrameCount
                // 100% * CurrentFrame / FrameCount = Actual Percentage

                Percentage = Math.Round(CurrentFrame * 100.0 / FrameCount, 1, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// The frame count.
        /// </summary>
        [Description("The frame count.")]
        public int FrameCount
        {
            get { return (int)GetValue(FrameCountProperty); }
            set { SetCurrentValue(FrameCountProperty, value); }
        }

        /// <summary>
        /// The description of the item.
        /// </summary>
        [Description("The description of the item.")]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetCurrentValue(TextProperty, value); }
        }

        /// <summary>
        /// The reason of the error of the item.
        /// </summary>
        [Description("The reason of the error of the item.")]
        public string Reason
        {
            get { return (string)GetValue(ReasonProperty); }
            set { SetCurrentValue(ReasonProperty, value); }
        }

        /// <summary>
        /// The ID of the Task.
        /// </summary>
        [Description("The ID of the Task.")]
        public int Id
        {
            get { return (int)GetValue(IdProperty); }
            set { SetCurrentValue(IdProperty, value); }
        }

        /// <summary>
        /// The Cancellation Token Source.
        /// </summary>
        [Description("The Cancellation Token Source.")]
        public CancellationTokenSource TokenSource
        {
            get { return (CancellationTokenSource)GetValue(TokenProperty); }
            set { SetCurrentValue(TokenProperty, value); }
        }

        /// <summary>
        /// The state of the progress bar.
        /// </summary>
        [Description("The state of the progress bar.")]
        public bool IsIndeterminate
        {
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetCurrentValue(IsIndeterminateProperty, value); }
        }

        /// <summary>
        /// The status of the encoding.
        /// </summary>
        [Description("The status of the encoding.")]
        public Status Status
        {
            get { return (Status)GetValue(StatusProperty); }
            set { SetCurrentValue(StatusProperty, value); }
        }

        /// <summary>
        /// The size of the output file in bytes.
        /// </summary>
        [Description("The size of the output file in bytes.")]
        public long SizeInBytes
        {
            get { return (long)GetValue(SizeInBytesProperty); }
            set { SetCurrentValue(SizeInBytesProperty, value); }
        }

        /// <summary>
        /// The filename of the output file.
        /// </summary>
        [Description("The filename of the output file.")]
        public string OutputFilename
        {
            get { return (string)GetValue(OutputFilenameProperty); }
            set { SetCurrentValue(OutputFilenameProperty, value); }
        }

        /// <summary>
        /// The path of the output file.
        /// </summary>
        [Description("The path of the output file.")]
        public string OutputPath
        {
            get { return (string)GetValue(OutputPathProperty); }
            set { SetCurrentValue(OutputPathProperty, value); }
        }

        #endregion

        #region Events

        //public static readonly RoutedEvent DemoEvent = EventManager.RegisterRoutedEvent("Demo", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EncoderListViewItem));

        //public event RoutedEventHandler Demo
        //{
        //    add { AddHandler(DemoEvent, value); }
        //    remove { RemoveHandler(DemoEvent, value); }
        //}

        /// <summary>
        /// Close Button clicked event.
        /// </summary>
        public event Action<object> CloseButtonClickedEvent;
        public event Action<object> LabelLinkClickedEvent;
        public event Action<object> PathClickedEvent;

        private void ButtonOnPreviewMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            CloseButtonClickedEvent?.Invoke(this);
        }

        private void LinkOnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            LabelLinkClickedEvent?.Invoke(OutputFilename);
        }

        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            PathClickedEvent?.Invoke(OutputPath);
        }

        #endregion

        static EncoderListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EncoderListViewItem), new FrameworkPropertyMetadata(typeof(EncoderListViewItem)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var closeButton = Template.FindName("CancelButton", this) as Button;
            var labelLink = Template.FindName("LinkLabel", this) as Label;
            var pathButton = Template.FindName("PathButton", this) as ImageButton;

            if (closeButton != null)
                closeButton.PreviewMouseUp += ButtonOnPreviewMouseUp;

            if (labelLink != null)
                labelLink.PreviewMouseLeftButtonDown += LinkOnMouseLeftButtonDown;

            if (pathButton != null)
                pathButton.Click += PathButton_Click;
        }

        private static void OutputFilename_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as EncoderListViewItem;

            if (item == null)
                return;

            item.OutputPath = Path.GetDirectoryName(item.OutputFilename);
        }
    }
}
