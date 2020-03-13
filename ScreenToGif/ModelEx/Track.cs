using System.Collections.Generic;

namespace ScreenToGif.ModelEx
{
    public class Track
    {
        public int Id { get; set; }

        public bool IsVisible { get; set; }

        public bool IsLocked { get; set; }
        
        public string Name { get; set; }

        /// <summary>
        /// A track can have multiple sequences of the same type.
        /// </summary>
        public List<Sequence> Sequences { get; set; }
    }
}