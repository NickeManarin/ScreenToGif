using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls;

public class ExtendedUniformGrid : UniformGrid
{
    public static readonly DependencyProperty IsReversedProperty = DependencyProperty.Register(nameof(IsReversed), typeof(bool), typeof(ExtendedUniformGrid),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsMeasure));

    //private static void IsReversed_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    //{
    //    if (!(o is ExtendedUniformGrid grid))
    //        return;
    //}

    public bool IsReversed
    {
        get => (bool)GetValue(IsReversedProperty);
        set => SetValue(IsReversedProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        UpdateComputedValues();

        var availableSize = new Size(constraint.Width / Columns, constraint.Height / Rows);
        var num1 = 0.0;
        var num2 = 0.0;

        if (IsReversed)
        {
            for (var i = InternalChildren.Count - 1; i >= 0; i--)
            {
                var internalChild = InternalChildren[i];
                internalChild.Measure(availableSize);
                var desiredSize = internalChild.DesiredSize;

                if (num1 < desiredSize.Width)
                    num1 = desiredSize.Width;

                if (num2 < desiredSize.Height)
                    num2 = desiredSize.Height;
            }

            return new Size(num1 * Columns, num2 * Rows);
        }

        var index = 0;

        for (var count = InternalChildren.Count; index < count; ++index)
        {
            var internalChild = InternalChildren[index];
            internalChild.Measure(availableSize);
            var desiredSize = internalChild.DesiredSize;

            if (num1 < desiredSize.Width)
                num1 = desiredSize.Width;

            if (num2 < desiredSize.Height)
                num2 = desiredSize.Height;
        }

        return new Size(num1 * Columns, num2 * Rows);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var finalRect = new Rect(0.0, 0.0, arrangeSize.Width / Columns, arrangeSize.Height / Rows);
        var width = finalRect.Width;
        var num = arrangeSize.Width - 1.0;

        finalRect.X += finalRect.Width * FirstColumn;

        if (IsReversed)
        {
            for (var i = InternalChildren.Count - 1; i >= 0; i--)
            {
                InternalChildren[i].Arrange(finalRect);

                if (InternalChildren[i].Visibility != Visibility.Collapsed)
                {
                    finalRect.X += width;

                    if (finalRect.X >= num)
                    {
                        finalRect.Y += finalRect.Height;
                        finalRect.X = 0.0;
                    }
                }
            }

            return arrangeSize;
        }

        foreach (UIElement internalChild in InternalChildren)
        {
            internalChild.Arrange(finalRect);

            if (internalChild.Visibility != Visibility.Collapsed)
            {
                finalRect.X += width;

                if (finalRect.X >= num)
                {
                    finalRect.Y += finalRect.Height;
                    finalRect.X = 0.0;
                }
            }
        }

        return arrangeSize;
    }

    private void UpdateComputedValues()
    {
        if (FirstColumn >= Columns)
            FirstColumn = 0;

        if (Rows != 0 && Columns != 0)
            return;

        var num = 0;
        var index = 0;

        for (var count = InternalChildren.Count; index < count; ++index)
        {
            if (InternalChildren[index].Visibility != Visibility.Collapsed)
                ++num;
        }

        if (num == 0)
            num = 1;

        if (Rows == 0)
        {
            if (Columns > 0)
            {
                Rows = (num + FirstColumn + (Columns - 1)) / Columns;
            }
            else
            {
                Rows = (int)Math.Sqrt(num);

                if (Rows * Rows < num)
                    Rows = Rows + 1;

                Columns = Rows;
            }
        }
        else
        {
            if (Columns != 0)
                return;

            Columns = (num + (Rows - 1)) / Rows;
        }
    }
}