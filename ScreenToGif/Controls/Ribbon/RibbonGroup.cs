using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls.Ribbon
{
    public class RibbonGroup : HeaderedItemsControl
    {
        public static readonly DependencyProperty GroupSizeDefinitionsProperty = DependencyProperty.Register(nameof(GroupSizeDefinitions), typeof(List<GroupSizeDefinition>), typeof(RibbonGroup), new PropertyMetadata(new List<GroupSizeDefinition>(), SizeDefinition_Changed));
        public static readonly DependencyProperty SizeIndexProperty = DependencyProperty.Register(nameof(SizeIndex), typeof(int), typeof(RibbonGroup), new PropertyMetadata(0, SizeDefinition_Changed));
        public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(default(bool)));

        /// <summary>
        /// From the size scheme for with more available space to the one with the least available space.
        /// </summary>
        public List<GroupSizeDefinition> GroupSizeDefinitions
        {
            get => (List<GroupSizeDefinition>) GetValue(GroupSizeDefinitionsProperty);
            set => SetValue(GroupSizeDefinitionsProperty, value);
        }

        public int SizeIndex
        {
            get => (int)GetValue(SizeIndexProperty);
            set => SetValue(SizeIndexProperty, value);
        }

        public bool IsCollapsed
        {
            get => (bool)GetValue(IsCollapsedProperty);
            set => SetValue(IsCollapsedProperty, value);
        }


        private static void SizeDefinition_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var group = d as RibbonGroup;
            group?.InvalidateVisual();
        }


        protected override Size MeasureOverride(Size constraint)
        {
            //Based on the current size definition
            //Ignore changes if in menu mode.

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            //Based on the current size definition
            //Ignore changes if in menu mode.

            return base.ArrangeOverride(arrangeBounds);
        }
    }
}