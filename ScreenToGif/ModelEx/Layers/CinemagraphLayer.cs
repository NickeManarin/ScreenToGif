using System.Windows.Ink;

namespace ScreenToGif.ModelEx.Layers
{
    internal class CinemagraphLayer : LayerModel
    {
        public CinemagraphLayer()
        {
            Type = LayerType.Cinemagraph;
            RequiresPreviousPixels = true;
        }

        public StrokeCollection StrokeCollection { get; set; }
    }
}