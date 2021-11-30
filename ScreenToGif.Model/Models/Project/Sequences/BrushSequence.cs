using System.Windows.Media;

namespace ScreenToGif.Domain.Models.Project.Sequences;

public class BrushSequence : SizeableSequence
{
    public Brush Brush { get; set; }


    public BrushSequence()
    {
        Type = Types.Brush;
    }
}