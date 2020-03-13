using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.ModelEx;

namespace ScreenToGif.ViewModel
{
    public class EditorViewModel : BaseViewModel
    {
        #region Variables

        private Project _project = null;
        private TimeSpan _currentTime = TimeSpan.Zero;

        #endregion

        #region Properties

        public Project Project
        {
            get => _project;
            set => SetProperty(ref _project, value);
        }

        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }



        #endregion

        //MainImageViewer = receives the RenderedImage.
        internal WriteableBitmap RenderedImage { get; set; }

        #region Methods

        internal void Init()
        {
            RenderedImage = new WriteableBitmap(Project.Width, Project.Heigth, Project.HorizontalDpi, Project.VerticalDpi, PixelFormats.Bgra32, null);
        }

        #endregion
    }
}