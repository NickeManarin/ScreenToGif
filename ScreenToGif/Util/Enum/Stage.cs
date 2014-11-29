using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util.Enum
{
    public enum Stage : int
    {
        Stopped = 0,
        Recording = 1,
        Paused = 2,
        PreStarting = 3,
        Editing = 4,
        Encoding = 5,
        Snapping = 6,
    };
}
