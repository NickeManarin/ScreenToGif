using System.Windows.Input;

namespace ScreenToGif.Domain.Models.Project.Sequences.SubSequences;

public class KeyEvent
{
    public Key Key { get; set; }

    public ModifierKeys Modifiers { get; set; }

    public TimeSpan TimeStamp { get; set; }
}