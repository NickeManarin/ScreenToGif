using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ScreenToGif.Util;

public class BrushAnimation : AnimationTimeline
{
    #region Properties

    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(nameof(From), typeof(Brush), typeof(BrushAnimation), new PropertyMetadata(new SolidColorBrush()));
    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(nameof(To), typeof(Brush), typeof(BrushAnimation), new PropertyMetadata(new SolidColorBrush()));

    public Brush From
    {
        get => (Brush)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public Brush To
    {
        get => (Brush)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    #endregion
        
    public override Type TargetPropertyType => typeof(Brush);

    protected override Freezable CreateInstanceCore()
    {
        return new BrushAnimation();
    }
        
    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        //To solid color.
        if (To is SolidColorBrush solidTo)
        {
            if (From is LinearGradientBrush linear)
            {
                var newLinear = new LinearGradientBrush();

                foreach (var stop in linear.GradientStops)
                {
                    var animation = new ColorAnimation(stop.Color, solidTo.Color, Duration);
                    var color = animation.GetCurrentValue(stop.Color, solidTo.Color, animationClock);

                    newLinear.GradientStops.Add(new GradientStop(color, stop.Offset));
                }

                return newLinear;
            }

            if (From is SolidColorBrush solid)
            {
                var newsolid = new SolidColorBrush();
                var solidAnimation = new ColorAnimation(solid.Color, solidTo.Color, Duration);
                    
                newsolid.Color = solidAnimation.GetCurrentValue(solid.Color, solidTo.Color, animationClock);

                return newsolid;
            }
        }

        //To linear color.
        if (To is LinearGradientBrush linearTo)
        {
                
        }

        return defaultDestinationValue;
    }
}