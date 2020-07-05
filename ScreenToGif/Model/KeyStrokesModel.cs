using System.Windows;
using System.Windows.Media;
using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    public class KeyStrokesModel : DefaultTaskModel
    {
        #region Variables

        private bool _ignoreNonModifiers;
        private bool _earlier;
        private double _earlierBy;
        private string _separator;
        private bool _extended;
        private double _delay;
        private FontFamily _fontFamily;
        private FontStyle _fontStyle;
        private FontWeight _fontWeight;
        private double _fontSize;
        private Color _fontColor;
        private double _outlineThickness;
        private Color _outlineColor;
        private Color _backgroundColor;
        private VerticalAlignment _verticalAlignment;
        private HorizontalAlignment _horizontalAlignment;
        private double _margin;
        private double _padding;
        private double _minHeight;

        #endregion

        public KeyStrokesModel()
        {
            TaskType = TaskTypeEnum.KeyStrokes;
        }

        public bool KeyStrokesIgnoreNonModifiers
        {
            get => _ignoreNonModifiers;
            set => SetProperty(ref _ignoreNonModifiers, value);
        }

        public bool KeyStrokesEarlier
        {
            get => _earlier;
            set => SetProperty(ref _earlier, value);
        }

        public double KeyStrokesEarlierBy
        {
            get => _earlierBy;
            set => SetProperty(ref _earlierBy, value);
        }

        public string KeyStrokesSeparator
        {
            get => _separator;
            set => SetProperty(ref _separator, value);
        }

        public bool KeyStrokesExtended
        {
            get => _extended;
            set => SetProperty(ref _extended, value);
        }

        public double KeyStrokesDelay
        {
            get => _delay;
            set => SetProperty(ref _delay, value);
        }

        public FontFamily KeyStrokesFontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        public FontStyle KeyStrokesFontStyle
        {
            get => _fontStyle;
            set => SetProperty(ref _fontStyle, value);
        }

        public FontWeight KeyStrokesFontWeight
        {
            get => _fontWeight;
            set => SetProperty(ref _fontWeight, value);
        }

        public double KeyStrokesFontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public Color KeyStrokesFontColor
        {
            get => _fontColor;
            set => SetProperty(ref _fontColor, value);
        }

        public double KeyStrokesOutlineThickness
        {
            get => _outlineThickness;
            set => SetProperty(ref _outlineThickness, value);
        }

        public Color KeyStrokesOutlineColor
        {
            get => _outlineColor;
            set => SetProperty(ref _outlineColor, value);
        }

        public Color KeyStrokesBackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        public VerticalAlignment KeyStrokesVerticalAlignment
        {
            get => _verticalAlignment;
            set => SetProperty(ref _verticalAlignment, value);
        }

        public HorizontalAlignment KeyStrokesHorizontalAlignment
        {
            get => _horizontalAlignment;
            set => SetProperty(ref _horizontalAlignment, value);
        }

        public double KeyStrokesMargin
        {
            get => _margin;
            set => SetProperty(ref _margin, value);
        }

        public double KeyStrokesPadding
        {
            get => _padding;
            set => SetProperty(ref _padding, value);
        }

        public double KeyStrokesMinHeight
        {
            get => _minHeight;
            set => SetProperty(ref _minHeight, value);
        }

        #region Obsolete (Typo in name)

        public VerticalAlignment KeyStrokesVerticalAligment
        {
            get => _verticalAlignment;
            set => SetProperty(ref _verticalAlignment, value);
        }

        public HorizontalAlignment KeyStrokesHorizontalAligment
        {
            get => _horizontalAlignment;
            set => SetProperty(ref _horizontalAlignment, value);
        }

        #endregion

        public override string ToString()
        {
            return $"{LocalizationHelper.Get("S.Caption.Size")} {KeyStrokesFontSize}, " +
                   $"{LocalizationHelper.Get("S.Caption.Color")} #{KeyStrokesFontColor.A:X2}{KeyStrokesFontColor.R:X2}{KeyStrokesFontColor.G:X2}{KeyStrokesFontColor.B:X2}, " +
                   $"{LocalizationHelper.Get("S.Background")} #{KeyStrokesBackgroundColor.A:X2}{KeyStrokesBackgroundColor.R:X2}{KeyStrokesBackgroundColor.G:X2}{KeyStrokesBackgroundColor.B:X2}, " +
                   $"{LocalizationHelper.Get("S.Margin")} {KeyStrokesMargin}, " +
                   $"{LocalizationHelper.Get("S.Padding")} {KeyStrokesPadding}";
        }

        public static KeyStrokesModel Default()
        {
            return new KeyStrokesModel
            {
                KeyStrokesIgnoreNonModifiers = true,
                KeyStrokesEarlier = false,
                KeyStrokesEarlierBy = 500,
                KeyStrokesExtended = true,
                KeyStrokesDelay = 800,
                KeyStrokesSeparator = "  ",
                KeyStrokesFontFamily = new FontFamily("Segoe UI"),
                KeyStrokesFontSize = 30,
                KeyStrokesFontColor = Color.FromArgb(255,255,255,255),
                KeyStrokesFontStyle = FontStyles.Normal,
                KeyStrokesFontWeight = FontWeights.Bold,
                KeyStrokesOutlineThickness = 0,
                KeyStrokesOutlineColor = Color.FromArgb(255, 255, 255, 255),
                KeyStrokesBackgroundColor = Color.FromArgb(255, 0, 0, 0),
                KeyStrokesHorizontalAlignment = HorizontalAlignment.Center,
                KeyStrokesVerticalAlignment = VerticalAlignment.Bottom,
                KeyStrokesMargin = 0,
                KeyStrokesPadding = 5,
                KeyStrokesMinHeight = 42,
            };
        }

        public static KeyStrokesModel FromSettings()
        {
            return new KeyStrokesModel
            {
                KeyStrokesIgnoreNonModifiers = UserSettings.All.KeyStrokesIgnoreNonModifiers,
                KeyStrokesEarlier = UserSettings.All.KeyStrokesEarlier,
                KeyStrokesEarlierBy = UserSettings.All.KeyStrokesEarlierBy,
                KeyStrokesExtended = UserSettings.All.KeyStrokesExtended,
                KeyStrokesDelay = UserSettings.All.KeyStrokesDelay,
                KeyStrokesSeparator = UserSettings.All.KeyStrokesSeparator,
                KeyStrokesFontFamily = UserSettings.All.KeyStrokesFontFamily,
                KeyStrokesFontSize = UserSettings.All.KeyStrokesFontSize,
                KeyStrokesFontColor = UserSettings.All.KeyStrokesFontColor,
                KeyStrokesFontStyle = UserSettings.All.KeyStrokesFontStyle,
                KeyStrokesFontWeight = UserSettings.All.KeyStrokesFontWeight,
                KeyStrokesOutlineThickness = UserSettings.All.KeyStrokesOutlineThickness,
                KeyStrokesOutlineColor = UserSettings.All.KeyStrokesOutlineColor,
                KeyStrokesBackgroundColor = UserSettings.All.KeyStrokesBackgroundColor,
                KeyStrokesHorizontalAlignment = UserSettings.All.KeyStrokesHorizontalAligment,
                KeyStrokesVerticalAlignment = UserSettings.All.KeyStrokesVerticalAligment,
                KeyStrokesMargin = UserSettings.All.KeyStrokesMargin,
                KeyStrokesPadding = UserSettings.All.KeyStrokesPadding,
                KeyStrokesMinHeight = UserSettings.All.KeyStrokesMinHeight,
            };
        }
    }
}