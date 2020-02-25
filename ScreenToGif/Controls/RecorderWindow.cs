using System.ComponentModel;
using System.Windows;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// All recorders are derived from this class.
    /// </summary>
    public class RecorderWindow : Window
    {
        public static readonly DependencyProperty StageProperty = DependencyProperty.Register(nameof(Stage), typeof(Stage), typeof(RecorderWindow), new FrameworkPropertyMetadata(Stage.Stopped));
        public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register(nameof(FrameCount), typeof(int), typeof(RecorderWindow), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The actual stage of the recorder.
        /// </summary>
        public Stage Stage
        {
            get => (Stage)GetValue(StageProperty);
            set => SetValue(StageProperty, value);
        }

        /// <summary>
        /// The frame count of the current recording.
        /// </summary>
        [Bindable(true), Category("Common"), Description("The frame count of the current recording.")]
        public int FrameCount
        {
            get => (int)GetValue(FrameCountProperty);
            set => SetValue(FrameCountProperty, value);
        }

        /// <summary>
        /// The project information about the current recording.
        /// </summary>
        internal ProjectInfo Project { get; set; }
    }
}