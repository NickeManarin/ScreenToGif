using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenToGif.Native.External;

namespace ScreenToGif.Util;

public static class TaskbarHelper
{
    private static readonly DependencyPropertyKey IsUpdatingPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsUpdating", typeof(bool), typeof(TaskbarHelper), new PropertyMetadata(false));

    public static readonly DependencyProperty IsUpdatingProperty = IsUpdatingPropertyKey.DependencyProperty;

    public static bool GetIsUpdating(DependencyObject window) => (bool)window.GetValue(IsUpdatingProperty);

    internal static void ShowInTaskbarChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        QueueRepaint(dependencyObject as Window);
    }

    internal static void QueueRepaint(Window window)
    {
        if (window == null || !window.IsLoaded || !window.IsVisible || GetIsUpdating(window))
            return;

        window.SetValue(IsUpdatingPropertyKey, true);

        //WPF hides and re-shows the native window when ShowInTaskbar changes. Repaint the
        //retained child visuals after that transition, coalescing changes before the next frame.
        window.Dispatcher.InvokeAsync(() =>
        {
            if (!window.IsVisible || PresentationSource.FromVisual(window) == null)
            {
                window.SetValue(IsUpdatingPropertyKey, false);
                return;
            }

            InvalidateVisualTree(window);
            window.UpdateLayout();

            //Let WPF submit the refreshed visuals before invalidating the native surface.
            window.Dispatcher.InvokeAsync(() =>
            {
                if (window.IsVisible && PresentationSource.FromVisual(window) is HwndSource source)
                {
                    const uint redrawFlags = 0x0001 | 0x0080 | 0x0100; //RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW.
                    User32.RedrawWindow(source.Handle, IntPtr.Zero, IntPtr.Zero, redrawFlags);
                }

                window.Dispatcher.InvokeAsync(() => window.SetValue(IsUpdatingPropertyKey, false), DispatcherPriority.ContextIdle);
            }, DispatcherPriority.Loaded);
        }, DispatcherPriority.Render);
    }

    private static void InvalidateVisualTree(DependencyObject visual)
    {
        if (visual is UIElement element)
        {
            element.InvalidateMeasure();
            element.InvalidateArrange();
            element.InvalidateVisual();
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(visual); index++)
            InvalidateVisualTree(VisualTreeHelper.GetChild(visual, index));
    }
}