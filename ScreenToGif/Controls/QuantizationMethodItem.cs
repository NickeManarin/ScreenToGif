using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class QuantizationMethodItem : UIElement
    {
        public static readonly DependencyProperty ImageIdProperty = DependencyProperty.Register(nameof(ImageId), typeof(string), typeof(QuantizationMethodItem), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(nameof(Name), typeof(string), typeof(QuantizationMethodItem), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(QuantizationMethodItem), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty QuantizationTypeProperty = DependencyProperty.Register(nameof(QuantizationType), typeof(ColorQuantizationType), typeof(QuantizationMethodItem), 
            new PropertyMetadata(default(ColorQuantizationType)));


        public string ImageId
        {
            get => (string)GetValue(ImageIdProperty);
            set => SetValue(ImageIdProperty, value);
        }

        public string Name
        {
            get => (string) GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public ColorQuantizationType QuantizationType
        {
            get => (ColorQuantizationType) GetValue(QuantizationTypeProperty);
            set => SetValue(QuantizationTypeProperty, value);
        }
    }
}