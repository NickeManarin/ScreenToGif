using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Util;

public class ScrollSynchronizer : DependencyObject
{
    /// <summary>
    /// Identifies the attached property ScrollGroup
    /// </summary>
    public static readonly DependencyProperty ScrollGroupProperty = DependencyProperty.RegisterAttached("ScrollGroup", typeof(string), typeof(ScrollSynchronizer), new PropertyMetadata(OnScrollGroupChanged));

    /// <summary>
    /// List of all registered scroll viewers.
    /// </summary>
    private static readonly Dictionary<ScrollViewer, string> ScrollViewers = new();

    /// <summary>
    /// Contains the latest horizontal scroll offset for each scroll group.
    /// </summary>
    private static readonly Dictionary<string, double> HorizontalScrollOffsets = new();

    /// <summary>
    /// Contains the latest vertical scroll offset for each scroll group.
    /// </summary>
    private static readonly Dictionary<string, double> VerticalScrollOffsets = new();

    /// <summary>
    /// Sets the value of the attached property ScrollGroup.
    /// </summary>
    /// <param name="obj">Object on which the property should be applied.</param>
    /// <param name="scrollGroup">Value of the property.</param>
    public static void SetScrollGroup(DependencyObject obj, string scrollGroup)
    {
        obj.SetValue(ScrollGroupProperty, scrollGroup);
    }

    /// <summary>
    /// Gets the value of the attached property ScrollGroup.
    /// </summary>
    /// <param name="obj">Object for which the property should be read.</param>
    /// <returns>Value of the property StartTime</returns>
    public static string GetScrollGroup(DependencyObject obj)
    {
        return (string)obj.GetValue(ScrollGroupProperty);
    }

    /// <summary>
    /// Occurs, when the ScrollGroupProperty has changed.
    /// </summary>
    /// <param name="d">The DependencyObject on which the property has changed value.</param>
    /// <param name="e">Event data that is issued by any event that tracks changes to the effective value of this property.</param>
    private static void OnScrollGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
            return;

        if (!string.IsNullOrEmpty((string)e.OldValue))
        {
            // Remove scrollviewer
            if (ScrollViewers.ContainsKey(scrollViewer))
            {
                scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
                ScrollViewers.Remove(scrollViewer);
            }
        }

        if (!string.IsNullOrEmpty((string)e.NewValue))
        {
            // If group already exists, set scrollposition of new scrollviewer to the scrollposition of the group
            if (HorizontalScrollOffsets.Keys.Contains((string)e.NewValue))
                scrollViewer.ScrollToHorizontalOffset(HorizontalScrollOffsets[(string)e.NewValue]);
            else
                HorizontalScrollOffsets.Add((string)e.NewValue, scrollViewer.HorizontalOffset);

            if (VerticalScrollOffsets.Keys.Contains((string)e.NewValue))
                scrollViewer.ScrollToVerticalOffset(VerticalScrollOffsets[(string)e.NewValue]);
            else
                VerticalScrollOffsets.Add((string)e.NewValue, scrollViewer.VerticalOffset);

            // Add scrollviewer
            ScrollViewers.Add(scrollViewer, (string)e.NewValue);
            scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }
    }

    /// <summary>
    /// Occurs, when the scroll offset of one scrollviewer has changed.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">EventArgs of the event.</param>
    private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0 && e.HorizontalChange == 0)
            return;

        var changedScrollViewer = sender as ScrollViewer;
        Scroll(changedScrollViewer);
    }

    /// <summary>
    /// Scrolls all scroll viewers of a group to the position of the selected scroll viewer.
    /// </summary>
    /// <param name="changedScrollViewer">Sroll viewer, that specifies the current position of the group.</param>
    private static void Scroll(ScrollViewer changedScrollViewer)
    {
        var group = ScrollViewers[changedScrollViewer];
        VerticalScrollOffsets[group] = changedScrollViewer.VerticalOffset;
        HorizontalScrollOffsets[group] = changedScrollViewer.HorizontalOffset;

        foreach (var scrollViewer in ScrollViewers.Where((s) => s.Value == group && s.Key != changedScrollViewer))
        {
            if (scrollViewer.Key.VerticalOffset != changedScrollViewer.VerticalOffset)
                scrollViewer.Key.ScrollToVerticalOffset(changedScrollViewer.VerticalOffset);

            if (scrollViewer.Key.HorizontalOffset != changedScrollViewer.HorizontalOffset)
                scrollViewer.Key.ScrollToHorizontalOffset(changedScrollViewer.HorizontalOffset);
        }
    }
}