using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    public class ProgressModel : DefaultTaskModel
    {
        #region Variables

        private ProgressType _type;
        private FontFamily _fontFamily;
        private FontStyle _fontStyle;
        private FontWeight _fontWeight;
        private double _fontSize;
        private Color _fontColor;
        private Color _color;
        private int _precision;
        private int _startNumber;
        private bool _showTotal;
        private string _format;
        private string _dateFormat;
        private double _thickness;
        private VerticalAlignment _verticalAlignment;
        private HorizontalAlignment _horizontalAlignment;
        private Orientation _orientation;

        #endregion

        public ProgressModel()
        {
            TaskType = TaskTypeEnum.Progress;
        }


        public ProgressType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public FontFamily FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        public FontStyle FontStyle
        {
            get => _fontStyle;
            set => SetProperty(ref _fontStyle, value);
        }

        public FontWeight FontWeight
        {
            get => _fontWeight;
            set => SetProperty(ref _fontWeight, value);
        }

        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public Color FontColor
        {
            get => _fontColor;
            set => SetProperty(ref _fontColor, value);
        }

        public Color Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public int Precision
        {
            get => _precision;
            set => SetProperty(ref _precision, value);
        }

        public int StartNumber
        {
            get => _startNumber;
            set => SetProperty(ref _startNumber, value);
        }

        public bool ShowTotal
        {
            get => _showTotal;
            set => SetProperty(ref _showTotal, value);
        }

        public string Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        public string DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set => SetProperty(ref _verticalAlignment, value);
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set => SetProperty(ref _horizontalAlignment, value);
        }

        public Orientation Orientation
        {
            get => _orientation;
            set => SetProperty(ref _orientation, value);
        }

        public override string ToString()
        {
            return $"{(Type == ProgressType.Bar ? LocalizationHelper.Get("S.Progress.Type.Bar") : LocalizationHelper.Get("S.Progress.Type.Text"))}, " +
                   $"{LocalizationHelper.Get("S.Color")} #{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}" +
                   $"{(Type == ProgressType.Text ? ", " + LocalizationHelper.GetWithIndex(Precision, "S.Progress.Precision.", "Minutes", "Seconds", "Milliseconds", "Percentage", "Count", "DateOfRecording", "Custom") : "")}";
        }

        public static ProgressModel Default()
        {
            return new ProgressModel
            {
                Type = ProgressType.Text,
                FontFamily = new FontFamily("Segoe UI"),
                FontStyle = FontStyles.Normal,
                FontWeight = FontWeights.Normal,
                FontSize = 14,
                FontColor = Color.FromArgb(255, 255, 255, 255),
                Color = Color.FromArgb(255, 0, 0, 0),
                Precision = 0,
                StartNumber = 0,
                ShowTotal = true,
                Format = " $s/@s s",
                DateFormat = "G",
                Thickness = 10,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Orientation = Orientation.Horizontal
            };
        }

        public static ProgressModel FromSettings()
        {
            return new ProgressModel
            {
                Type = UserSettings.All.ProgressType,
                FontFamily = UserSettings.All.ProgressFontFamily,
                FontStyle = UserSettings.All.ProgressFontStyle,
                FontWeight = UserSettings.All.ProgressFontWeight,
                FontSize = UserSettings.All.ProgressFontSize,
                FontColor = UserSettings.All.ProgressFontColor,
                Color = UserSettings.All.ProgressColor,
                Precision = UserSettings.All.ProgressPrecision,
                StartNumber = UserSettings.All.ProgressStartNumber,
                ShowTotal = UserSettings.All.ProgressShowTotal,
                Format = UserSettings.All.ProgressFormat,
                DateFormat = UserSettings.All.ProgressDateFormat,
                Thickness = UserSettings.All.ProgressThickness,
                VerticalAlignment = UserSettings.All.ProgressVerticalAligment,
                HorizontalAlignment = UserSettings.All.ProgressHorizontalAligment,
                Orientation = UserSettings.All.ProgressOrientation
            };
        }
    }
}