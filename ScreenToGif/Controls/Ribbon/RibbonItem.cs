using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls.Ribbon
{
    public class RibbonItem : Button
    {
        public enum RibbonItemMode
        {
            Button,
            DropDownButton,
            SplitButton
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(RibbonItem), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(UIElement), typeof(RibbonItem), new PropertyMetadata(default(UIElement)));
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(RibbonItemMode), typeof(RibbonItem), new PropertyMetadata(RibbonItemMode.Button));
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(ItemSizeDefinition.IconSizeEnum), typeof(RibbonItem), new PropertyMetadata(ItemSizeDefinition.IconSizeEnum.Large));
        public static readonly DependencyProperty IsTextVisibleProperty = DependencyProperty.Register(nameof(IsTextVisible), typeof(bool), typeof(RibbonItem), new PropertyMetadata(true));
        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(nameof(Menu), typeof(List<FrameworkElement>), typeof(RibbonItem), new PropertyMetadata(new List<FrameworkElement>()));

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
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
    }
}