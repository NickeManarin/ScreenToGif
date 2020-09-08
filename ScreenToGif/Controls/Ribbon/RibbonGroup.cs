using System.Collections.Generic;
using System.Linq;
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


        static RibbonGroup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonGroup), new FrameworkPropertyMetadata(typeof(RibbonGroup)));
        }


        private static void SizeDefinition_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var group = d as RibbonGroup;
            group?.InvalidateVisual();
        }


        protected override Size MeasureOverride(Size constraint)
        {
            var sizeDefinition = GroupSizeDefinitions[SizeIndex];

            //Apply the sizing to the items on the group. ?
            if (!sizeDefinition.IsCollapsed)
            {
                var width = 0d;
                var height = 0d;
                var index = 0;

                foreach (var item in Items.OfType<RibbonItem>())
                {
                    item.IconSize = sizeDefinition.SizeDefinitions[index].IconSize;
                    item.IsTextVisible = sizeDefinition.SizeDefinitions[index].IsHeaderVisible;

                    item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    width += item.DesiredSize.Width;
                    height += item.DesiredSize.Height;

                    index++;
                }

                return new Size(width, height);
            }
            
            //Based on the current size definition
            //Ignore changes if in menu mode.

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var sizeDefinition = GroupSizeDefinitions[SizeIndex];

            if (!sizeDefinition.IsCollapsed)
            {
                var left = 0d;
                var top = 0d;

                foreach (var item in Items.OfType<RibbonItem>())
                {
                    item.Arrange(new Rect(left, top, item.DesiredSize.Width, item.DesiredSize.Height));
                    
                    left += item.DesiredSize.Width;
                    top += item.DesiredSize.Height;
                }

                return base.ArrangeOverride(arrangeBounds);
            }

            //Based on the current size definition
            //Ignore changes if in menu mode.
        
            return base.ArrangeOverride(arrangeBounds);
        }
    }
}