using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ScreenToGif.Util;

public static class VisualHelper
{
    public static readonly object LockObject = new();

    public static IntPtr GetHandle(this Window window) => new WindowInteropHelper(window).EnsureHandle();

    public static HwndSource GetHwndSource(this Window window) => HwndSource.FromHwnd(window.GetHandle());

    /// <summary>
    /// Gets the scale of the current window.
    /// </summary>
    /// <param name="window">The Window.</param>
    /// <returns>The scale of the given Window.</returns>
    public static double GetVisualScale(this Visual window)
    {
        var source = PresentationSource.FromVisual(window);

        return source?.CompositionTarget != null ? source.CompositionTarget.TransformToDevice.M11 : 1d;
    }

    public static TP GetParent<TP>(DependencyObject child, int i) where TP : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        var logicalParent = LogicalTreeHelper.GetParent(child);

        if (logicalParent is TP dependencyObject)
            return dependencyObject;

        if (i > 4 || parent == null || parent is TP)
            return parent as TP;

        return GetParent<TP>(parent, i + 1);
    }

    public static T GetVisualParent<T>(this DependencyObject child) where T : Visual
    {
        while (child != null && child is not T)
            child = VisualTreeHelper.GetParent(child);

        return child as T;
    }

    /// <summary>
    /// Checks whether the given coordinates are within given element bounds.
    /// </summary>
    /// <returns>True if the coordinates are within element bounds.</returns>
    public static bool HitTestElement(this FrameworkElement element, int x, int y)
    {
        var scale = element.GetVisualScale();
        var rect = new Rect(element.PointToScreen(new Point()), new Size(element.Width * scale, element.Height * scale));

        return rect.Contains(x, y);
    }

    public static TP GetParent<TP>(DependencyObject child, Type stopWhen) where TP : Visual
    {
        var parent = VisualTreeHelper.GetParent(child);
        var logicalParent = LogicalTreeHelper.GetParent(child);

        if (logicalParent is TP correctLogical)
            return correctLogical;

        if (parent is TP correctParent)
            return correctParent;

        if (parent == null || parent.GetType() == stopWhen)
            return null;

        return GetParent<TP>(parent, stopWhen);
    }

    public static bool HasParent<T>(DependencyObject child, Type stopWhen, bool checkSelf = false) where T : Visual
    {
        if (checkSelf && child is T)
            return true;

        var parent = VisualTreeHelper.GetParent(child);
        var logicalParent = LogicalTreeHelper.GetParent(child);

        if (logicalParent is T)
            return true;

        if (parent is T)
            return true;

        if (parent == null || parent.GetType() == stopWhen)
            return false;

        return HasParent<T>(parent, stopWhen);
    }

    public static T GetVisualChild<T>(Visual parent) where T : Visual
    {
        var child = default(T);
        var numVisuals = VisualTreeHelper.GetChildrenCount(parent);

        for (var i = 0; i < numVisuals; i++)
        {
            var v = (Visual)VisualTreeHelper.GetChild(parent, i);

            child = v as T ?? GetVisualChild<T>(v);

            if (child != null)
                break;
        }

        return child;
    }

    public static T DeepCopy<T>(UIElement source) where T : new()
    {
        if (source == null)
            return new T();

        var savedObject = System.Windows.Markup.XamlWriter.Save(source);

        var stringReader = new StringReader(savedObject);
        var xmlReader = System.Xml.XmlReader.Create(stringReader);

        return (T)System.Windows.Markup.XamlReader.Load(xmlReader);
    }

    public static Storyboard FindStoryboard(this FrameworkElement visual, string key)
    {
        if (visual.TryFindResource(key) is not Storyboard resource)
            return new Storyboard();

        return resource;
    }

    public static bool IsInDesignMode()
    {
        return (bool) DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty,
            typeof(FrameworkElement)).Metadata.DefaultValue;
    }

    /// <summary>
    /// Checks if the <see cref="FrameworkElement.DataContextProperty"/> is bound or not.
    /// </summary>
    /// <param name="element">The element to be checked.</param>
    /// <returns>True if the data context property is being managed by a binding expression.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="element"/> is a null reference.</exception>
    public static bool IsDataContextDataBound(this FrameworkElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        return element.GetBindingExpression(FrameworkElement.DataContextProperty) != null;
    }

    private static readonly Action EmptyDelegate = delegate { };

    public static void Refresh(this UIElement uiElement)
    {
        uiElement?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);
    }
}