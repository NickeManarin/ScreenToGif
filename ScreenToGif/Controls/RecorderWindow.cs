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
        public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(Stage), typeof(RecorderWindow), new FrameworkPropertyMetadata(Stage.Stopped));

        /// <summary>
        /// The actual stage of the recorder.
        /// </summary>
        public Stage Stage
        {
            get => (Stage)GetValue(StageProperty);
            set => SetValue(StageProperty, value);
        }

        /// <summary>
        /// The project information about the current recording.
        /// </summary>
        internal ProjectInfo Project { get; set; }
    }
}