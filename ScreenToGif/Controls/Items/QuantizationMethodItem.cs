using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Controls.Items
{
    public class QuantizationMethodItem : GenericItem
    {
        public static readonly DependencyProperty QuantizationTypeProperty = DependencyProperty.Register(nameof(QuantizationType), typeof(ColorQuantizationType), typeof(QuantizationMethodItem), 
            new PropertyMetadata(default(ColorQuantizationType)));
        
        public ColorQuantizationType QuantizationType
        {
            get => (ColorQuantizationType) GetValue(QuantizationTypeProperty);
            set => SetValue(QuantizationTypeProperty, value);
        }
    }
}