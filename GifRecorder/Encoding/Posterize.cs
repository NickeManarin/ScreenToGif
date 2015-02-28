using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenToGif.Encoding
{
    /// <summary>
    /// Posterize filter.
    /// </summary>
    public class Posterize
    {
        #region Variables

        /// <summary>
        /// Unfiltered image.
        /// </summary>
        public Image OriginalImage
        {
            get { return _originalImage; }
            set { _originalImage = value as Bitmap; }
        }

        Bitmap _originalImage = null;
        int nOffset, nWidth;
        Bitmap unsbmp;
        BitmapData _bmData;

        #endregion

        /// <summary>
        /// Bitmap Constructor.
        /// </summary>
        /// <param name="Image">The picture to be posterized.</param>
        public Posterize(Bitmap Image)
        {
            _originalImage = new Bitmap(Image);
        }

        /// <summary>
        /// Runs the process of Posterize.
        /// </summary>
        /// <returns>The posterized image.</returns>
        public Bitmap ExecuteFilter(int step)
        {
            if (step == 255) return _originalImage;

            var image = new PixelUtil(_originalImage);
            image.LockBits();

            int height = _originalImage.Height;
            int width = _originalImage.Width;

            #region Loop

            //x - width - sides
            for (int x = 0; x < width; x++)
            {
                //y - height - up/down
                for (int y = 0; y < height; y++)
                {
                    image.SetPixel(x, y, PosterizeCalculus(image.GetPixel(x, y), (step /100F)));
                }
            }

            #endregion

            image.UnlockBits();
            return _originalImage;
        }

        //Calculate the channel for the given color.
        private byte PosterizeCalculus(byte channel, double step)
        {
            if ((step >= 0) && (step <= 0.5)) 
                return channel < 127 ? (byte)0 : (byte)255;

            double adim = 255 / step;
            double ilk = channel - (channel % adim);
            double son = ilk + adim;
            return (channel - (ilk)) < ((son) - channel) ? (byte)Round(ilk) : (byte)Round(son);
        }

        private Color PosterizeCalculus(Color original, double step)
        {
            if ((step >= 0) && (step <= 0.5))
            {
                int red = original.R < 127 ? 0 : 255;
                int green = original.G < 127 ? 0 : 255;
                int blue = original.B < 127 ? 0 : 255;

                return Color.FromArgb(red, green, blue);
            }

            double auxRed = 255 / step;
            double auxGreen = 255 / step;
            double auxBlue = 255 / step;

            double aux2Red = original.R - (original.R % auxRed);
            double aux2Green = original.G - (original.G % auxGreen);
            double aux2Blue = original.B - (original.B % auxBlue);

            double sumRed = aux2Red + auxRed;
            double sumGreen = aux2Green + auxGreen;
            double sumBlue = aux2Blue + auxBlue;

            int redEnd = (original.R - (aux2Red)) < ((sumRed) - original.R) ? (byte)Round(aux2Red) : (byte)Round(sumRed);
            int greenEnd = (original.G - (aux2Green)) < ((sumGreen) - original.G) ? (byte)Round(aux2Green) : (byte)Round(sumGreen);
            int blueEnd = (original.B - (aux2Blue)) < ((sumBlue) - original.B) ? (byte)Round(aux2Blue) : (byte)Round(sumBlue);

            return Color.FromArgb(redEnd, greenEnd, blueEnd);
        }

        private int Round(double x)
        {
            if (x - Convert.ToInt32(x) < 0.5) return Convert.ToInt32(x); else return (Convert.ToInt32(x) + 1);
        }
    }
}
