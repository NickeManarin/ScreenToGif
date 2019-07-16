using System.Collections.Generic;

namespace ScreenToGif.ModelEx
{
    public class FrameModel
    {
        internal List<LayerModel> Layers { get; set; }

        internal byte[] Pixels { get; set; }

        /// <summary>
        /// Frame time. For how long a frame should be displayed before the next frame is rendered.
        /// </summary>
        internal int Delay { get; set; }

        internal void Render()
        {
            var layerPixels = new List<byte[]>();

            //Unites all frames in one pixel array.
            //Rendering of layers should be ordered.
            //Some of the types of layers need the pixel data of the previously rendered layer. 
            foreach (var layer in Layers)
            {
                layer.Render();

                //If the layer requires data from all previous layers, send it to them.
                //layerPixels.Add(layer.Pixels);
            }

            Pixels = null;
        }
    }
}