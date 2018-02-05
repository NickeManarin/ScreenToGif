using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    [DefaultProperty("Content")]
    [ContentProperty("Content")]
    public class ImageCard : Button
    {
        #region Dependency Properties

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register("Key", typeof(string), typeof(ImageCard));

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(ImageCard));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ImageCard));

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(ExtrasStatus), typeof(ImageCard),
            new PropertyMetadata(ExtrasStatus.Available));

        #endregion

        #region Property Accessors

        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public ExtrasStatus Status
        {
            get => (ExtrasStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        #endregion

        static ImageCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageCard), new FrameworkPropertyMetadata(typeof(ImageCard)));
        }
    }
}