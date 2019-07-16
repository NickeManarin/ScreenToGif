using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.ModelEx;

namespace ScreenToGif.ViewModel
{
    public class EditorViewModel : BaseViewModel
    {
        //MainImageViewer = receives the RenderedImage.
        //The project viewer has an index
        //Each 

        internal WriteableBitmap RenderedImage { get; set; }

        internal ProjectModel Project { get; set; }



        internal void Init()
        {
            RenderedImage = new WriteableBitmap(Project.Width, Project.Heigth, Project.Dpi, Project.Dpi, PixelFormats.Bgra32, null);
        }
    }
}