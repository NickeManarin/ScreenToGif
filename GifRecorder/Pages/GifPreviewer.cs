using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// A form that plays the gif.
    /// </summary>
    public partial class GifPreviewer : Form
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        private GifPreviewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor that receives the image as parameter.
        /// </summary>
        public GifPreviewer(string imagePath):this()
        {
            if (File.Exists(imagePath))
            {
                pictureBox.Image = new Bitmap(imagePath);
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