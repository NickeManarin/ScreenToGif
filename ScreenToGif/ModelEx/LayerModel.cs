using System.Windows;
using System.Windows.Media;

namespace ScreenToGif.ModelEx
{
    public class LayerModel
    {
        internal enum LayerType : int
        {
            /// <summary>
            /// A layer that contains a single brush data.
            /// </summary>
            Color,
            /// <summary>
            /// A layer that holds image data.
            /// It can be the actual frame image from a recording or an overlay.
            /// </summary>
            Image,

            Text,
            KeyStrokes,
            TitleFrame, //? Maybe it should be a layer type of Frame.
            Progress,
            MouseClicks,
            Shape,
            Drawing,
            Obfuscation,
            Cinemagraph
        }

        /// <summary>
        /// Each layer can have simblings distributed among other frames.
        /// So, if one layer is manipulated, but multiples frames are selected, all layers of the same Id should be manipulated together.
        /// </summary>
        internal int Id { get; set; }

        /// <summary>
        /// The layer type. Each different layer type may act differently.
        /// </summary>
        internal LayerType Type { get; set; }

        internal int Top { get; set; }

        internal int Left { get; set; }

        internal int Width { get; set; }

        internal int Height { get; set; }

        internal bool IsVisible { get; set; }

        /// <summary>
        /// True if this layer requires the pixel data from the previous layers in order to be rendered.
        /// </summary>
        internal bool RequiresPreviousPixels { get; set; }

        /// <summary>
        /// From 100 to 0.
        /// </summary>
        internal double Opacity { get; set; }

        /// <summary>
        /// After rendering, this is where the pixel data is stored.
        /// </summary>
        internal byte[] Pixels { get; set; }

        //?
        internal Thickness BorderThickness { get; set; }

        internal Brush BorderColor { get; set; }

        internal Brush Background { get; set; }

        /// <summary>
        ///Use the current position/sizing details to contruct the pixel data.
        ///Each type of layer should implement this method.
        /// </summary>
        internal virtual void Render()
        {}
    }
}