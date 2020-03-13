using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Util.Converters;

namespace ScreenToGif.Controls.Ribbon
{
    public class RibbonPanel : Panel
    {
        private int[] _order = null;

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(Ribbon.Modes), typeof(RibbonPanel), 
            new FrameworkPropertyMetadata(Ribbon.Modes.Ribbon, FrameworkPropertyMetadataOptions.AffectsRender, Mode_Changed));
        public static readonly DependencyProperty ReductionOrderProperty = DependencyProperty.Register(nameof(ReductionOrder), typeof(string[]), typeof(RibbonPanel), 
            new PropertyMetadata(default(string[]), ReductionOrder_Changed));

        public Ribbon.Modes Mode
        {
            get => (Ribbon.Modes)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        [TypeConverter(typeof(StringArrayTypeConverter))]
        public string[] ReductionOrder
        {
            get => (string[])GetValue(ReductionOrderProperty);
            set => SetValue(ReductionOrderProperty, value);
        }


        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            OrderChildren();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Mode == Ribbon.Modes.Menu)
                return base.MeasureOverride(availableSize);

            var availableWidth = availableSize.Width;
            var availableHeight = double.IsInfinity(availableSize.Height) ? 90 : availableSize.Height;

            //Get the size reduction ordering, to know which group should be reducted next.
            if (Children.Count != _order?.Length)
                OrderChildren();

            //Detect which size definition is the best for the available width.



            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Mode == Ribbon.Modes.Menu)
                return base.ArrangeOverride(finalSize);

            double width = 0;

            foreach (UIElement element in Children)
            {
                var size = element.DesiredSize;
                element.Arrange(new Rect(width, 0, size.Width, finalSize.Height));
                width += size.Width;
            }

            return new Size(width, finalSize.Height);
        }

        private static void Mode_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as RibbonPanel;
            element?.SwitchMode();
        }

        private static void ReductionOrder_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as RibbonPanel;
            element?.OrderChildren();
        }

        private void SwitchMode()
        {
            switch (Mode)
            {
                case Ribbon.Modes.Ribbon:
                {
                    break;
                }
                case Ribbon.Modes.Menu:
                {

                    break;
                }
            }

            InvalidateVisual();
        }

        private void OrderChildren()
        {
            if (Children.Count == 0)
            {
                _order = new int[0];
                return;
            }

            var order = new List<int>();

            foreach (UIElement child in Children)
            {
                if (ReductionOrder == null || !(child is FrameworkElement element) || string.IsNullOrEmpty(element.Name))
                {
                    order.Add(Children.IndexOf(child));
                    continue;
                }

                var index = Array.IndexOf(ReductionOrder, element.Name);

                order.Add(index < 0 ? Children.IndexOf(child) : index);
            }

            _order = order.ToArray();
        }
    }
}