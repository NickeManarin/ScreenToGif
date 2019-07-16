using System.Windows;

namespace ScreenToGif.ModelEx.Layers
{
    internal class MouseClickLayer : LayerModel
    {
        public MouseClickLayer()
        {
            Type = LayerType.MouseClicks;
        }

        public Point Position { get; set; }

        public bool LeftClick { get; set; }
        public bool RightClick { get; set; }
        public bool MiddleClick { get; set; }
        public bool OtherButtonClick { get; set; }

        public bool MiddleScrollUp { get; set; }
        public bool MiddleScrollDown { get; set; }

        public bool MiddleScroll => MiddleScrollUp || MiddleScrollDown;
    }
}