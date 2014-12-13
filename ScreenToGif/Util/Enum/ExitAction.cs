using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util.Enum
{
    /// <summary>
    /// Exit actions after closing the Recording Window
    /// </summary>
    public enum ExitAction : int
    {
        /// <summary>
        /// Return to the StartUp Window.
        /// </summary>
        Return = 0,

        /// <summary>
        /// Go to the Editor.
        /// </summary>
        Edit = 1,

        /// <summary>
        /// Exit the application.
        /// </summary>
        Exit = 2,
    };
}
