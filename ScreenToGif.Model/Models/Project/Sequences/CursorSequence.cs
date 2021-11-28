using ScreenToGif.Domain.Models.Project.Sequences.SubSequences;

namespace ScreenToGif.Domain.Models.Project.Sequences;

public class CursorSequence : SizeableSequence
{
    public List<CursorEvent> CursorEvents { get; set; }


    public CursorSequence()
    {
        Type = Types.Cursor;
    }
}