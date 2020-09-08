using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace ScreenToGif.Controls.Ribbon
{
    [ContentProperty("Content")]
    public class RibbonItem : ButtonBase
    {
        public enum RibbonItemMode
        {
            Button,
            DropDownButton,
            SplitButton,
            Other
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(RibbonItem), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(RibbonItem));
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(RibbonItemMode), typeof(RibbonItem), new PropertyMetadata(RibbonItemMode.Button));
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(ItemSizeDefinition.IconSizeEnum), typeof(RibbonItem), new PropertyMetadata(ItemSizeDefinition.IconSizeEnum.Large));
        public static readonly DependencyProperty IsTextVisibleProperty = DependencyProperty.Register(nameof(IsTextVisible), typeof(bool), typeof(RibbonItem), new PropertyMetadata(true));
        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(nameof(Menu), typeof(List<FrameworkElement>), typeof(RibbonItem), new PropertyMetadata(new List<FrameworkElement>()));

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Brush Icon
        {
            get => (Brush)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public RibbonItemMode Mode
        {
            get => (RibbonItemMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public ItemSizeDefinition.IconSizeEnum IconSize
        {
            get => (ItemSizeDefinition.IconSizeEnum)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public bool IsTextVisible
        {
            get => (bool)GetValue(IsTextVisibleProperty);
            set => SetValue(IsTextVisibleProperty, value);
        }

        public List<FrameworkElement> Menu
        {
            get => (List<FrameworkElement>)GetValue(MenuProperty);
            set => SetValue(MenuProperty, value);
        }

        static RibbonItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonItem), new FrameworkPropertyMetadata(typeof(RibbonItem)));
        }
    }
}