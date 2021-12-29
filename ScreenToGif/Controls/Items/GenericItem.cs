using System.Windows;

namespace ScreenToGif.Controls.Items;

public class GenericItem : FrameworkElement
{
    public static readonly DependencyProperty ImageIdProperty = DependencyProperty.Register(nameof(ImageId), typeof(string), typeof(GenericItem), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(GenericItem), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(GenericItem), new PropertyMetadata(default(string)));
        
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(GenericItem), new PropertyMetadata(default(object)));

    public string ImageId
    {
        get => (string)GetValue(ImageIdProperty);
        set => SetValue(ImageIdProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object Value
    {
        get => (object)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}