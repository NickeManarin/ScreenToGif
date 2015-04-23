using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Common Exception used as End of Line for certain actions. 
    /// </summary>
    public class UsageException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="instruction">A single phrase description of the Exception.</param>
        /// <param name="reason">The reason of the Exception.</param>
        public UsageException(string instruction, string reason)
        {
            Instruction = instruction;
            Reason = reason;
        }

        /// <summary>
        /// A single phrase description of the Exception.
        /// </summary>
        public string Instruction { get; set; }

        /// <summary>
        /// The reason of the Exception.
        /// </summary>
        public string Reason { get; set; }
    }
}
