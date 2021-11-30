using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using Panel = System.Windows.Controls.Panel;

namespace ScreenToGif.Util;

public static class UiElementExtensions
{
    public static T XamlClone<T>(this T original) where T : class
    {
        if (original == null)
            return null;

        object clone;
        using (var stream = new MemoryStream())
        {
            XamlWriter.Save(original, stream);
            stream.Seek(0, SeekOrigin.Begin);
            clone = XamlReader.Load(stream);
        }

        return clone as T;
    }

    //public static T Clone<T>(this T source)
    //{
    //    var sb = new StringBuilder();
    //    var writer = XmlWriter.Create(sb, new XmlWriterSettings
    //    {
    //        Indent = true,
    //        ConformanceLevel = ConformanceLevel.Fragment,
    //        OmitXmlDeclaration = true,
    //        NamespaceHandling = NamespaceHandling.OmitDuplicates,
    //    });

    //    var mgr = new XamlDesignerSerializationManager(writer);
    //    mgr.XamlWriterMode = XamlWriterMode.Expression;

    //    XamlWriter.Save(source, mgr);

    //    return null; // sb.ToString();
    //}

    public static T DeepClone<T>(this T source) where T : UIElement
    {
        var type = source.GetType();

        var result = Activator.CreateInstance(type) as T;

        CopyProperties(source, result, type);
        DeepCopyChildren(source, result);

        return result;
    }

    private static void DeepCopyChildren<T>(T source, T result) where T : UIElement
    {
        // Deep copy children.
        var sourcePanel = source as Panel;

        if (sourcePanel == null)
            return;

        var resultPanel = result as Panel;

        if (resultPanel == null)
            return;

        foreach (UIElement child in sourcePanel.Children)
        {
            var childClone = DeepClone(child);
            resultPanel.Children.Add(childClone);
        }
    }

    private static void CopyProperties<T>(T source, T result, Type type) where T : UIElement
    {
        //Copy all properties.
        var properties = type.GetRuntimeProperties();

        foreach (var property in properties)
        {
            if (property.Name == "Name")
                continue;

            if (!property.CanWrite || !property.CanRead)
                continue;

            var sourceProperty = property.GetValue(source);

            if (sourceProperty is UIElement element)
            {
                var propertyClone = element.DeepClone();
                property.SetValue(result, propertyClone);
            }
            else
            {
                try
                {
                    property.SetValue(result, sourceProperty);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}