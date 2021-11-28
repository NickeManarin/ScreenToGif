using System.Windows.Shapes;

namespace ScreenToGif.Domain.Models.Project.Sequences;

public class ShapeSequence : SizeableSequence
{
    public Shape Shape { get; set; }


    public ShapeSequence()
    {
        Type = Types.Shape;
    }
}