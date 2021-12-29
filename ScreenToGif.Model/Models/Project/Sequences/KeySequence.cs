using ScreenToGif.Domain.Models.Project.Sequences.SubSequences;

namespace ScreenToGif.Domain.Models.Project.Sequences;

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