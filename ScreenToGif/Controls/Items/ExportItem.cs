using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Controls.Items
{
    public class ExportItem : GenericItem
    {
        public static readonly DependencyProperty ExportTypeProperty = DependencyProperty.Register(nameof(ExportType), typeof(string), typeof(ExportItem),
            new PropertyMetadata(default(string)));

        public static readonly DependencyProperty FileTypeProperty = DependencyProperty.Register(nameof(FileType), typeof(Export), typeof(ExportItem), 
            new PropertyMetadata(default(Export)));

        
        public string ExportType
        {
            get => (string) GetValue(ExportTypeProperty);
            set => SetValue(ExportTypeProperty, value);
        }

        public Export FileType
        {
            get => (Export) GetValue(FileTypeProperty);
            set => SetValue(FileTypeProperty, value);
        }
    }
}