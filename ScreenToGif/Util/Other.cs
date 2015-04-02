using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util
{
    public static class Other
    {
        public static List<FrameInfo> CopyList(this List<FrameInfo> target)
        {
            return target.Select(item => new FrameInfo(item.ImageLocation, item.Delay, item.CursorInfo)).ToList();
        }
    }
}
