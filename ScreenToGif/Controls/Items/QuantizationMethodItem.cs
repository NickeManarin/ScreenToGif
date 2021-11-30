using System.Windows;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Controls.Items;

public class QuantizationMethodItem : GenericItem
{
    public static readonly DependencyProperty QuantizationTypeProperty = DependencyProperty.Register(nameof(QuantizationType), typeof(ColorQuantizationTypes), typeof(QuantizationMethodItem), 
        new PropertyMetadata(default(ColorQuantizationTypes)));
        
    public ColorQuantizationTypes QuantizationType
    {
        get => (ColorQuantizationTypes) GetValue(QuantizationTypeProperty);
        set => SetValue(QuantizationTypeProperty, value);
    }
}