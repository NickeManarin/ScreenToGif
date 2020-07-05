using System.Windows.Media;
using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    public class MouseClicksModel : DefaultTaskModel
    {
        private Color _foregroundColor;
        private double _width;
        private double _height;

        public MouseClicksModel()
        {
            TaskType = TaskTypeEnum.MouseClicks;
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set => SetProperty(ref _foregroundColor, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public override string ToString()
        {
            return $"{LocalizationHelper.Get("S.Caption.Color")} #{ForegroundColor.A:X2}{ForegroundColor.R:X2}{ForegroundColor.G:X2}{ForegroundColor.B:X2}, {LocalizationHelper.Get("S.FreeDrawing.Width")} {Width}, {LocalizationHelper.Get("S.FreeDrawing.Height")} {Height}";
        }

        public static MouseClicksModel Default()
        {
            return new MouseClicksModel
            {
                ForegroundColor = Color.FromArgb(120, 255, 255, 0),
                Height = 12,
                Width = 12
            };
        }

        public static MouseClicksModel FromSettings()
        {
            return new MouseClicksModel
            {
                ForegroundColor = UserSettings.All.MouseClicksColor,
                Height = UserSettings.All.MouseClicksHeight,
                Width = UserSettings.All.MouseClicksWidth
            };
        }
    }
}