using System.Drawing;

namespace ScreenToGif_WPF.Capture
{
    public class CursorInfo
    {
        private Point position;
        private Icon icon;

        public Point Position
        {
            get { return position; }
            set
            {
                position = value;
            }
        }

        public Icon Icon
        {
            get { return icon; }
            set { icon = value; }
        }
    }
}
