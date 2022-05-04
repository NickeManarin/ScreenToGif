using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

public class ExtendedComboBox : ComboBox
{
    public static readonly DependencyProperty SelectionItemBoxProperty = DependencyProperty.Register(nameof(SelectionItemBox), typeof(object), typeof(ExtendedComboBox), new PropertyMetadata(default(object)));
        
    public static readonly DependencyProperty SelectionItemBoxTemplateProperty = DependencyProperty.Register(nameof(SelectionItemBoxTemplate), typeof(DataTemplate), typeof(ExtendedComboBox), new PropertyMetadata(default(DataTemplate)));

    public static readonly DependencyProperty EmptyItemProperty = DependencyProperty.Register(nameof(EmptyItem), typeof(object), typeof(ExtendedComboBox), new PropertyMetadata(default(object)));
        
    public static readonly DependencyProperty NoSelectionItemProperty = DependencyProperty.Register(nameof(NoSelectionItem), typeof(object), typeof(ExtendedComboBox), new PropertyMetadata(default(object)));


    public object SelectionItemBox
    {
        get => GetValue(SelectionItemBoxProperty);
        set => SetValue(SelectionItemBoxProperty, value);
    }

    public DataTemplate SelectionItemBoxTemplate
    {
        get => (DataTemplate) GetValue(SelectionItemBoxTemplateProperty);
        set => SetValue(SelectionItemBoxTemplateProperty, value);
    }

    public object EmptyItem
    {
        get => GetValue(EmptyItemProperty);
        set => SetValue(EmptyItemProperty, value);
    }

    public object NoSelectionItem
    {
        get => GetValue(NoSelectionItemProperty);
        set => SetValue(NoSelectionItemProperty, value);
    }
        

    static ExtendedComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedComboBox), new FrameworkPropertyMetadata(typeof(ExtendedComboBox)));
    }
}