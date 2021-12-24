using System.Windows.Media;

namespace ScreenToGif.Domain.Models.Project.Sequences.SubSequences;

public class Shadow
{
    public Color Color { get;set; } 
        
    public double Direction { get; set; }
        
    public double BlurRadius { get; set; }

    public double Opacity { get; set; }

    public double Depth { get; set; }
}