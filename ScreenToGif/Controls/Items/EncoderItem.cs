using System;
using System.Windows;

namespace ScreenToGif.Controls.Items;

public class EncoderItem : GenericItem
{
    public static readonly DependencyProperty EncoderTypeProperty = DependencyProperty.Register(nameof(EncoderType), typeof(Enum), typeof(EncoderItem), 
        new PropertyMetadata(default(Enum)));

    public Enum EncoderType
    {
        get => (Enum) GetValue(EncoderTypeProperty);
        set => SetValue(EncoderTypeProperty, value);
    }
}