using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Util;

public class ComboBoxItemTemplateSelector : DataTemplateSelector
{
    #region SelectedTemplate

    public static DependencyProperty SelectedTemplateProperty = DependencyProperty.RegisterAttached("SelectedTemplate", typeof(DataTemplate), typeof(ComboBoxItemTemplateSelector), new UIPropertyMetadata(null));

    [AttachedPropertyBrowsableForType(typeof(ComboBox))]
    public static DataTemplate GetSelectedTemplate(ComboBox obj)
    {
        return (DataTemplate)obj.GetValue(SelectedTemplateProperty);
    }

    public static void SetSelectedTemplate(ComboBox obj, DataTemplate value)
    {
        obj.SetValue(SelectedTemplateProperty, value);
    }

    #endregion

    #region DropDownTemplate

    public static DependencyProperty DropDownTemplateProperty = DependencyProperty.RegisterAttached("DropDownTemplate", typeof(DataTemplate), typeof(ComboBoxItemTemplateSelector), new UIPropertyMetadata(null));

    [AttachedPropertyBrowsableForType(typeof(ComboBox))]
    public static DataTemplate GetDropDownTemplate(ComboBox obj)
    {
        return (DataTemplate)obj.GetValue(DropDownTemplateProperty);
    }

    public static void SetDropDownTemplate(ComboBox obj, DataTemplate value)
    {
        obj.SetValue(DropDownTemplateProperty, value);
    }

    #endregion

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var comboBoxItem = container.GetVisualParent<ComboBoxItem>();
        return comboBoxItem == null ? GetSelectedTemplate(container.GetVisualParent<ComboBox>()) : GetDropDownTemplate(ItemsControl.ItemsControlFromItemContainer(comboBoxItem) as ComboBox);
    }
}