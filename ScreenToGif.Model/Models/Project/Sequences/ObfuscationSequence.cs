namespace ScreenToGif.Domain.Models.Project.Sequences;

public class ObfuscationSequence : SizeableSequence
{
    public enum Modes : int
    {
        Pixelate,
        Blur
    }

    public Modes ObfuscationMode { get; set; }

    //ObfuscationSize, other properties.


    public ObfuscationSequence()
    {
        Type = Types.Obfuscation;
    }
}