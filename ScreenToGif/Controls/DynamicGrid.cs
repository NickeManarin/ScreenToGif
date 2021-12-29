using System;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

public class DynamicGrid : Grid
{
    #region Dependency Properties

    public static readonly DependencyProperty FirstColumnProperty = DependencyProperty.Register("FirstColumn", typeof(int), typeof(DynamicGrid),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure), ValidateFirstColumn);

    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(int), typeof(DynamicGrid), new
        FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure), ValidateColumns);

    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(int), typeof(DynamicGrid),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure), ValidateRows);

    public static readonly DependencyProperty IsReversedProperty = DependencyProperty.Register("IsReversed", typeof(bool), typeof(DynamicGrid),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsMeasure));

    #endregion

    #region Properties

    ///<summary>Gets or sets the number of leading blank cells in the first row of the grid.  </summary>
    ///<returns>The number of empty cells that are in the first row of the grid. The default is 0.</returns>
    public int FirstColumn
    {
        get => (int)GetValue(FirstColumnProperty);
        set => SetValue(FirstColumnProperty, value);
    }

    /// <summary>Gets or sets the number of columns that are in the grid.  </summary>
    /// <returns>The number of columns that are in the grid. The default is 0. </returns>
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>Gets or sets the number of rows that are in the grid.  </summary>
    /// <returns>The number of rows that are in the grid. The default is 0.</returns>
    public int Rows
    {
        get => (int)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public bool IsReversed
    {
        get => (bool)GetValue(IsReversedProperty);
        set => SetValue(IsReversedProperty, value);
    }

    #endregion

    #region Coerce

    private static bool ValidateFirstColumn(object o)
    {
        return (int)o >= 0;
    }

    private static bool ValidateRows(object o)
    {
        return (int)o >= 0;
    }

    private static bool ValidateColumns(object o)
    {
        return (int)o >= 0;
    }

    #endregion

    protected override Size MeasureOverride(Size constraint)
    {
        UpdateComputedValues();

        RowDefinitions.Clear();
        ColumnDefinitions.Clear();

        for (var i = 0; i < Rows; i++)
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < Columns; i++)
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var count = 0;
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Columns; c++)
        {
            if (count > Children.Count - 1)
                continue;

            Children[count].Measure(constraint);

            if (RowDefinitions[r].MinHeight < Children[count].DesiredSize.Height)
                RowDefinitions[r].MinHeight = Children[count].DesiredSize.Height;

            if (ColumnDefinitions[c].MinWidth < Children[count].DesiredSize.Width)
                ColumnDefinitions[c].MinWidth = Children[count].DesiredSize.Width + 6;

            SetColumn(Children[count], c);
            SetRow(Children[count], r);
            count++;
        }
            
        return base.MeasureOverride(constraint);
    }

    //protected Size ArrangeOverride2(Size arrangeSize)
    //{
    //    var finalRect = new Rect(0.0, 0.0, arrangeSize.Width / Columns, arrangeSize.Height / Rows);
    //    var width = finalRect.Width;
    //    var num = arrangeSize.Width - 1.0;

    //    finalRect.X += finalRect.Width * FirstColumn;

    //    if (IsReversed)
    //    {
    //        for (var i = InternalChildren.Count - 1; i >= 0; i--)
    //        {
    //            InternalChildren[i].Arrange(finalRect);

    //            if (InternalChildren[i].Visibility != Visibility.Collapsed)
    //            {
    //                finalRect.X += width;

    //                if (finalRect.X >= num)
    //                {
    //                    finalRect.Y += finalRect.Height;
    //                    finalRect.X = 0.0;
    //                }
    //            }
    //        }

    //        return arrangeSize;
    //    }

    //    foreach (UIElement internalChild in InternalChildren)
    //    {
    //        internalChild.Arrange(finalRect);

    //        if (internalChild.Visibility != Visibility.Collapsed)
    //        {
    //            finalRect.X += width;

    //            if (finalRect.X >= num)
    //            {
    //                finalRect.Y += finalRect.Height;
    //                finalRect.X = 0.0;
    //            }
    //        }
    //    }

    //    return arrangeSize;
    //}

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