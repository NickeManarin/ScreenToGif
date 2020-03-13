using System.Collections.Generic;
using ScreenToGif.ModelEx.Sequences.SubSequences;

namespace ScreenToGif.ModelEx.Sequences
{
    /// <summary>
    /// KeyEvents can happen out of sync with the recording. 
    /// </summary>
    public class KeySequence : SizeableSequence
    {
        public List<KeyEvent> KeyEvents { get; set; }


        public KeySequence()
        {
            Type = Types.Key;
        }
    }
}