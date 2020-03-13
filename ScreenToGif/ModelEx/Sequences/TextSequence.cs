using System.Windows;
using System.Windows.Media;

namespace ScreenToGif.ModelEx.Sequences
{
    public class TextSequence : SizeableSequence
    {
        public string Text { get; set; }

        public FontFamily FontFamily { get; set; }
        
        public double FontSize { get; set; }
        
        public FontWeight FontWeight { get; set; }
        
        public FontStyle FontStyle { get; set; }
        
        public Brush Foreground { get; set; }
        
        public double OutlineThickness { get; set; }
        
        public Brush OutlineColor { get; set; }


        public TextSequence()
        {
            Type = Types.Text;
        }
    }
}