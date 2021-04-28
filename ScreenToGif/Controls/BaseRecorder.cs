using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// All recorders are derived from this class.
    /// </summary>
    public class BaseRecorder : Window
    {
        public static readonly DependencyProperty StageProperty = DependencyProperty.Register(nameof(Stage), typeof(Stage), typeof(BaseRecorder), new FrameworkPropertyMetadata(Stage.Stopped));
        public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register(nameof(FrameCount), typeof(int), typeof(BaseRecorder), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty HasImpreciseCaptureProperty = DependencyProperty.Register(nameof(HasImpreciseCapture), typeof(bool), typeof(BaseRecorder), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The actual stage of the recorder.
        /// </summary>
        public Stage Stage
        {
            get => (Stage)GetValue(StageProperty);
            set
            {
                SetValue(StageProperty, value);
                CommandManager.InvalidateRequerySuggested();
            }
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
        /// The frame count of the current recording.
        /// </summary>
        [Bindable(true), Category("Common"), Description("True if the recorder is unable to capture with precision.")]
        public bool HasImpreciseCapture
        {
            get => (bool)GetValue(HasImpreciseCaptureProperty);
            set => SetValue(HasImpreciseCaptureProperty, value);
        }

        /// <summary>
        /// The project information about the current recording.
        /// </summary>
        internal ProjectInfo Project { get; set; }
    }
}