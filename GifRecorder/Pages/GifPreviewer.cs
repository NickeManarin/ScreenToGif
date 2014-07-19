#region

using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

#endregion

namespace ScreenToGif.Pages
{
    public partial class GifPreviewer : Form
    {
        public GifPreviewer()
        {
            InitializeComponent();
        }

        public GifPreviewer(string imagePath):this()
        {
            if (File.Exists(imagePath))
            {
                Bitmap image = new Bitmap(imagePath);
                pictureBox.Image = image;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Image image = pictureBox.Image;
            if (null != image)
            {
                image.Dispose();
            }

            base.OnClosing(e);
        }
    }
}