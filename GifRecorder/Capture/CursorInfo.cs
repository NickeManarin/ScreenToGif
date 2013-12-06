using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Capture
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
