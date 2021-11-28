using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Translator.Util;

public static class VisualHelper
{
    public static TP GetParent<TP>(DependencyObject child, int i) where TP : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        var logicalParent = LogicalTreeHelper.GetParent(child);

        if (logicalParent is TP)
            return logicalParent as TP;

        if (i > 4 || parent == null || parent is TP)
            return parent as TP;

        return GetParent<TP>(parent, i + 1);
    }

    public static TP GetParent<TP>(DependencyObject child, Type stopWhen) where TP : Visual
    {
        var parent = VisualTreeHelper.GetParent(child);
        var logicalParent = LogicalTreeHelper.GetParent(child);

        if (logicalParent is TP)
            return logicalParent as TP;

        if (parent is TP)
            return parent as TP;

        if (parent == null || parent.GetType() == stopWhen)
            return null;

        return GetParent<TP>(parent, stopWhen);
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
        var resource = visual.FindResource(key) as Storyboard;

        if (resource == null)
            return new Storyboard();

        return resource;
    }
}
