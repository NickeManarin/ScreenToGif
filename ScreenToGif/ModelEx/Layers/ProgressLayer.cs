namespace ScreenToGif.ModelEx.Layers
{
    internal class ProgressLayer : LayerModel
    {
        /// <summary>
        /// Type of the progress indicator.
        /// </summary>
        public enum ProgressMode
        {
            Bar,
            Text,
        }

        public ProgressLayer()
        {
            Type = LayerType.Progress;
        }

        public ProgressMode ProgressType { get; set; }

        //Color.
        //Bar percentage.
        //How to decide the text?
    }
}