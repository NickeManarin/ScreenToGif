using System.Windows.Input;

namespace ScreenToGif.Domain.Interfaces;

public interface IKeyGesture
{
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }
    public bool IsUppercase { get; set; }
    public bool IsInjected { get; set; }
    public string DisplayString { get; }
}