using System.Collections.Generic;
using ScreenToGif.ModelEx.Sequences.SubSequences;

namespace ScreenToGif.ModelEx.Sequences
{
    public class CursorSequence : SizeableSequence
    {
        public List<CursorEvent> CursorEvents { get; set; }


        public CursorSequence()
        {
            Type = Types.Cursor;
        }
    }
}