using System;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.Tasks;

namespace ScreenToGif.UserControls;

public partial class ShadowPanel : UserControl
{
    public ShadowPanel()
    {
        InitializeComponent();
    }

    private void Properties_ValueChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not ShadowViewModel model || PreviewViewBox.Width < 0)
                return;

            //Converts the direction in degrees to radians.
            var radians = Math.PI / 180.0 * model.Direction;
            var offsetX = model.Depth * Math.Cos(radians);
            var offsetY = model.Depth * Math.Sin(radians);

            //Each side can have a different offset based on the direction of the shadow.
            var offsetLeft = offsetX < 0 ? offsetX * -1 : 0;
            var offsetTop = offsetY > 0 ? offsetY : 0;
            var offsetRight = offsetX > 0 ? offsetX : 0;
            var offsetBottom = offsetY < 0 ? offsetY * -1 : 0;

            //Measure drop shadow space.
            var marginLeft = offsetLeft > 0 ? offsetLeft + model.BlurRadius / 2d : Math.Max(model.BlurRadius / 2d - offsetLeft, 0); //- offsetX
            var marginTop = offsetTop > 0 ? offsetTop + model.BlurRadius / 2d : Math.Max(model.BlurRadius / 2d - offsetTop, 0); //- offsetY
            var marginRight = offsetRight > 0 ? offsetRight + model.BlurRadius / 2d : Math.Max(model.BlurRadius / 2d + offsetRight, 0); //+ offsetX
            var marginBottom = offsetBottom > 0 ? offsetBottom + model.BlurRadius / 2d : Math.Max(model.BlurRadius / 2d + offsetBottom, 0); //+ offsetY

            PreviewGrid.Width = marginLeft + PreviewViewBox.Width + marginRight;
            PreviewGrid.Height = Math.Round(marginTop + PreviewViewBox.Height + marginBottom, 0);

            PreviewGrid.InvalidateVisual();
            PreviewViewBox.InvalidateProperty(EffectProperty);

            //PreviewGrid.Width = left + totalWidth;
            //PreviewGrid.Height = top + totalHeight;
            //PreviewGrid.Margin = new Thickness(left, top, 0, 0);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while trying to measure dropshadow size for the shadow task.");
        }
    }
}