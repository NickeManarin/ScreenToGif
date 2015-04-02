using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util.Enum;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// ListViewItem used by the Encoder window.
    /// </summary>
    public class EncoderListViewItem : ListViewItem
    {
        #region Variables

        public readonly static DependencyProperty ImageProperty;
        public readonly static DependencyProperty MaxSizeProperty;
        public readonly static DependencyProperty PercentageProperty;
        public readonly static DependencyProperty CurrentFrameProperty;
        public readonly static DependencyProperty FrameCountProperty;
        public readonly static DependencyProperty TextProperty;
        public readonly static DependencyProperty IdProperty;
        public readonly static DependencyProperty TokenProperty;
        public readonly static DependencyProperty StatusProperty;
        public readonly static DependencyProperty SizeInBytesProperty;
        public readonly static DependencyProperty OutputPathProperty;

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
        public event Action<Object> CloseButtonClickedEvent;
        public event Action<Object> LabelLinkClickedEvent;

        private void RaiseCancelButtonClick()
        {
            if (CloseButtonClickedEvent != null)
            {
                CloseButtonClickedEvent(this);
            }
        }

        private void RaiseLabelLinkClick()
        {
            if (LabelLinkClickedEvent != null)
            {
                LabelLinkClickedEvent(OutputPath);
            }
        }

        private void ButtonOnPreviewMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            RaiseCancelButtonClick();
        }

        private void LinkOnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            RaiseLabelLinkClick();
        }

        #endregion

        static EncoderListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EncoderListViewItem), new FrameworkPropertyMetadata(typeof(EncoderListViewItem)));

            ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(EncoderListViewItem), new FrameworkPropertyMetadata());
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(25.0));
            PercentageProperty = DependencyProperty.Register("Percentage", typeof(double), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(0.0));
            FrameCountProperty = DependencyProperty.Register("FrameCount", typeof(int), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(0));
            CurrentFrameProperty = DependencyProperty.Register("CurrentFrame", typeof(int), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(1));
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(EncoderListViewItem), new FrameworkPropertyMetadata());

            IdProperty = DependencyProperty.Register("Id", typeof(int), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(-1));
            TokenProperty = DependencyProperty.Register("Token", typeof(CancellationTokenSource), typeof(EncoderListViewItem), new FrameworkPropertyMetadata());

            StatusProperty = DependencyProperty.Register("Status", typeof(Status), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(Status.Encoding));
            SizeInBytesProperty = DependencyProperty.Register("SizeInBytes", typeof(long), typeof(EncoderListViewItem), new FrameworkPropertyMetadata(0L));
            OutputPathProperty = DependencyProperty.Register("OutputPath", typeof(string), typeof(EncoderListViewItem), new FrameworkPropertyMetadata());
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var button = Template.FindName("CancelButton", this) as Button;
            var labelLink = Template.FindName("LinkLabel", this) as Label;

            if (button != null)
                button.PreviewMouseUp += ButtonOnPreviewMouseUp;

            if (labelLink != null)
                labelLink.PreviewMouseLeftButtonDown += LinkOnMouseLeftButtonDown;
        }
    }
}
