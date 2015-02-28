using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util.Enum
{
    /// <summary>
    /// Stage status of the Recording process.
    /// </summary>
    public enum Stage : int
    {
        /// <summary>
        /// Recording stopped.
        /// </summary>
        Stopped = 0,

        /// <summary>
        /// Recording active.
        /// </summary>
        Recording = 1,

        /// <summary>
        /// Recording paused.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// Pre start countdown active.
        /// </summary>
        PreStarting = 3,

        /// <summary>
        /// Single shot mode.
        /// </summary>
        Snapping = 4,
    };
}
