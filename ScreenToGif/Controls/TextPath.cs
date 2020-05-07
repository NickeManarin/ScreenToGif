using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// This class generates a Geometry from a block of text in a specific font, weight, etc. and renders it to WPF as a shape.
    /// </summary>
    public class TextPath : Shape
    {
        /// <summary>
        /// Data member that holds the generated geometry
        /// </summary>
        private Geometry _textGeometry;
        private Pen _pen;

        #region Dependency Properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TextPath), new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty OriginPointProperty = DependencyProperty.Register("Origin", typeof(Point), typeof(TextPath), new FrameworkPropertyMetadata(new Point(0.5, 0.5),
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(TextPath), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, 
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(TextPath), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, 
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(TextPath), new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue, 
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(TextPath), new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle, 
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(TextPath), new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight, 
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits));

        #endregion

        #region Property Accessors

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(PointConverter))]
        public Point Origin
        {
            get => (Point)GetValue(OriginPointProperty);
            set => SetValue(OriginPointProperty, value);
        }

        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.Font)]
        [TypeConverter(typeof(FontFamilyConverter))]
        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontStretchConverter))]
        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontStyleConverter))]
        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontWeightConverter))]
        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        [Bindable(true), Category("Appearance")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// This method is called to retrieve the geometry that defines the shape.
        /// </summary>
        protected override Geometry DefiningGeometry => _textGeometry ?? Geometry.Empty;

        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                _textGeometry.Transform = new TranslateTransform(-_textGeometry.Bounds.X, -_textGeometry.Bounds.Y + 1);
            }
            catch (Exception)
            {}

            //If the outline of the text should not be rendered outside, use the base OnRender method.
            if (!UserSettings.All.DrawOutlineOutside)
            {
                base.OnRender(drawingContext);
                return;
            }

            //This code will draw the outline outside the text.          
            drawingContext.DrawGeometry(null, _pen, _textGeometry);
            drawingContext.DrawGeometry(Fill, null, _textGeometry);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            try
            {
                _textGeometry = new FormattedText(Text ?? "", Thread.CurrentThread.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip).BuildGeometry(Origin);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to build text geometry.");

                try
                {
                    _textGeometry = new FormattedText(Text ?? "", Thread.CurrentThread.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Arial"), FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip).BuildGeometry(Origin);
                }
                catch (Exception ex2)
                {
                    LogWriter.Log(ex2, "Impossible to build text geometry with default font.");
                }
            }

            _pen = new Pen(Stroke, StrokeThickness)
            {
                DashCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                MiterLimit = StrokeMiterLimit
            };

            InvalidateVisual();

            base.OnPropertyChanged(e);
        }
        
        protected override Size MeasureOverride(Size constraint)
        {
            var definingGeometry = DefiningGeometry;
            var dashStyle = (DashStyle)null;

            if (_pen != null)
            {
                dashStyle = _pen.DashStyle;

                if (dashStyle != null)
                    _pen.DashStyle = null;
            }

            var renderBounds = definingGeometry.GetRenderBounds(_pen);

            if (dashStyle != null)
                _pen.DashStyle = dashStyle;

            return new Size(Math.Max(renderBounds.Right - renderBounds.X, 0.0), Math.Max(MinHeight, Math.Max(renderBounds.Bottom - renderBounds.Y + 1, 0.0)));
        }
    }
}