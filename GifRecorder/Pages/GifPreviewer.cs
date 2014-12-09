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
            webBrowser.DocumentCompleted += WebBrowserOnDocumentCompleted;
        }

        private void WebBrowserOnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs webBrowserDocumentCompletedEventArgs)
        {
            if (null != webBrowser.Document)
            {
                if (null != webBrowser.Document.Body)
                {
                    Size size = webBrowser.Document.Body.ScrollRectangle.Size;
                    this.Size = this.SizeFromClientSize(size);
                }
            }
        }

        /// <summary>
        /// Constructor that receives the image as parameter.
        /// </summary>
        public GifPreviewer(string imagePath):this()
        {
            if (File.Exists(imagePath))
            {
                webBrowser.Navigate(imagePath);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            webBrowser.Dispose();
            base.OnClosing(e);
        }
    }
}