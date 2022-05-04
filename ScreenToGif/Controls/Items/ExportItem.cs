using System.Windows;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Controls.Items;

public class ExportItem : GenericItem
{
    public static readonly DependencyProperty ExportTypeProperty = DependencyProperty.Register(nameof(ExportType), typeof(string), typeof(ExportItem),
        new PropertyMetadata(default(string)));

    public static readonly DependencyProperty FileTypeProperty = DependencyProperty.Register(nameof(FileType), typeof(ExportFormats), typeof(ExportItem), 
        new PropertyMetadata(default(ExportFormats)));

        
    public string ExportType
    {
        get => (string) GetValue(ExportTypeProperty);
        set => SetValue(ExportTypeProperty, value);
    }

    public ExportFormats FileType
    {
        get => (ExportFormats) GetValue(FileTypeProperty);
        set => SetValue(FileTypeProperty, value);
    }
}