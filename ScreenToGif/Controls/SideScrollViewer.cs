using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using ScreenToGif.Controls.Ribbon;

namespace ScreenToGif.Controls
{
    public class SideScrollViewer : ScrollViewer
    {
        private RepeatButton _leftButton;
        private RepeatButton _rightButton;
        private Border _selectionBorder;

        #region Properties

        public static readonly DependencyProperty DisplayLeftButtonProperty = DependencyProperty.Register(nameof(DisplayLeftButton), typeof(bool), typeof(SideScrollViewer), new PropertyMetadata(false));

        public static readonly DependencyProperty DisplayRightButtonProperty = DependencyProperty.Register(nameof(DisplayRightButton), typeof(bool), typeof(SideScrollViewer), new PropertyMetadata(false));

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SideScrollViewer), new PropertyMetadata(-1, SelectedIndex_PropertyChanged));


        public bool DisplayLeftButton
        {
            get => (bool)GetValue(DisplayLeftButtonProperty);
            set => SetValue(DisplayLeftButtonProperty, value);
        }

        public bool DisplayRightButton
        {
            get => (bool)GetValue(DisplayRightButtonProperty);
            set => SetValue(DisplayRightButtonProperty, value);
        }

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        #endregion

        static SideScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SideScrollViewer), new FrameworkPropertyMetadata(typeof(SideScrollViewer)));
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            _leftButton = Template.FindName("LineLeftButton", this) as RepeatButton;
            _rightButton = Template.FindName("LineRightButton", this) as RepeatButton;
            _selectionBorder = Template.FindName("SelectionBorder", this) as Border;

            if (_leftButton != null)
                _leftButton.Click += LeftButton_Click;

            if (_rightButton != null)
                _rightButton.Click += RightButton_Click;

            base.OnApplyTemplate();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            DetectVisibility();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (sizeInfo.WidthChanged)
                DetectVisibility();
        }

        protected override void OnChildDesiredSizeChanged(UIElement child)
        {
            base.OnChildDesiredSizeChanged(child);

            DetectVisibility();
        }

        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            DetectVisibility();

            base.OnScrollChanged(e);
        }

        #endregion

        private static void SelectedIndex_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                //Animate the border until it reaches the selected item.
                if (!(obj is SideScrollViewer viewer))
                    return;

                if (!(viewer.Content is StackPanel stack))
                    return;

                //Selection validation.
                var oldIndex = e.OldValue as int? ?? 0;
                var newIndex = e.NewValue as int? ?? 0;

                if (newIndex < 0)
                {
                    viewer._selectionBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                if (oldIndex < 0)
                    return;

                viewer._selectionBorder.Visibility = Visibility.Visible;

                if (!(stack.Children[oldIndex] is RibbonTab previousTab) || !(stack.Children[newIndex] is RibbonTab nextTab))
                    return;

                //Get offset of current item and the new one.
                var offsetOld = viewer.GetOffsetOfItem(stack, previousTab);
                var offsetNew = viewer.GetOffsetOfItem(stack, stack.Children[newIndex] as RibbonTab);

                //Movement to the right.
                if (offsetNew > offsetOld)
                {
                    //If the accent is id the middle of the animation and it's currently closer to the new position.
                    if (viewer._selectionBorder.Margin.Left > offsetOld)
                        offsetOld = viewer._selectionBorder.Margin.Left;
                }
                else if (offsetNew < offsetOld)
                {
                    //If the accent is id the middle of the animation and it's currently closer to the new position.
                    if (viewer._selectionBorder.Margin.Left < offsetOld)
                        offsetOld = viewer._selectionBorder.Margin.Left;
                }

                //Display the accent border and animate to the next position.
                viewer._selectionBorder.BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0))),
                        new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 5))),
                    }
                });

                //Resize and move the accent border to the correct position.
                //Then reduce the size of the accent border
                var sizeStory = new Storyboard
                {
                    Children =
                    {
                        new DoubleAnimation(previousTab.ActualWidth, nextTab.ActualWidth, new Duration(new TimeSpan(0, 0, 0, 1))) { EasingFunction = new PowerEase { Power = 9 }},
                        new DoubleAnimation(nextTab.ActualWidth, nextTab.ActualWidth - 20, new Duration(new TimeSpan(0, 0, 0, 0, 500))) { BeginTime = new TimeSpan(0, 0, 0, 1), EasingFunction = new PowerEase { Power = 9 }},
                    }
                };
                Storyboard.SetTargetProperty(sizeStory, new PropertyPath("Width"));
                viewer._selectionBorder.BeginStoryboard(sizeStory);

                var marginStory = new Storyboard
                {
                    Children =
                    {
                        new ThicknessAnimation(new Thickness(offsetOld, 0, 0, 1), new Thickness(offsetNew, 0, 0, 1), new Duration(new TimeSpan(0, 0, 0, 1))) { EasingFunction = new PowerEase { Power = 9 }},
                        new ThicknessAnimation(new Thickness(offsetNew, 0, 0, 1), new Thickness(offsetNew + 10, 0, 0, 1), new Duration(new TimeSpan(0, 0, 0, 0, 500))) { BeginTime = new TimeSpan(0, 0, 0, 1), EasingFunction = new PowerEase { Power = 9 }}
                    }
                };
                Storyboard.SetTargetProperty(marginStory, new PropertyPath("Margin"));
                viewer._selectionBorder.BeginStoryboard(marginStory);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            //Go one item in length to the left.
            var leftItemOffset = Math.Max(HorizontalOffset - Margin.Left, 0);

            var leftItem = GetItemByOffset(leftItemOffset);
            ScrollToItem(leftItem);

            DetectVisibility();
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            //Go one item in length to the right.
            // added margin left for sure that the item will be scrolled
            var rightItemOffset = Math.Min(HorizontalOffset + ViewportWidth + Margin.Left, ExtentWidth);

            var rightItem = GetItemByOffset(rightItemOffset);
            ScrollToItem(rightItem);

            DetectVisibility();
        }

        private void DetectVisibility()
        {
            //Determine the visibility of the Left and Right buttons based on the position of the scrollbar and the measured size of the Child.
            if (!(Content is UIElement child))
                return;

            //Display the left button when the HorizontalOffset is greater than zero.
            DisplayLeftButton = HorizontalOffset > 0;

            //Display the right button when the width of the Viewport + the scroll amount is more less than the desired width of the children.
            DisplayRightButton = (ViewportWidth + HorizontalOffset < child.DesiredSize.Width);
        }

        private double GetOffsetOfItem(UIElement stack, UIElement tab)
        {
            return tab.TranslatePoint(new Point(0, 0), stack).X;
        }

        private TabItem GetItemByOffset(double offset)
        {
            if (!(Content is StackPanel stack))
                return null;

            //If support for other items are needed: .Select(item => ItemContainerGenerator.ContainerFromItem(item) as TabItem)
            var tabItems = stack.Children.Cast<TabItem>().ToList();

            double currentItemsWidth = 0;

            //Get tabs one by one and calculate their aggregated width until the offset value is reached.
            foreach (var ti in tabItems)
            {
                if (currentItemsWidth + ti.ActualWidth >= offset)
                    return ti;

                currentItemsWidth += ti.ActualWidth;
            }

            return tabItems.LastOrDefault();
        }

        private void ScrollToItem(TabItem si)
        {
            if (si == null || !(Content is StackPanel stack))
                return;

            var tabItems = stack.Children.Cast<TabItem>().ToList();
            var leftItems = tabItems.Where(ti => ti != null).TakeWhile(ti => ti != si).ToList();
            var leftItemsWidth = leftItems.Sum(ti => ti.ActualWidth);

            //If the selected item is situated somewhere to the right.
            if (leftItemsWidth + si.ActualWidth > HorizontalOffset + ViewportWidth)
            {
                var currentHorizontalOffset = (leftItemsWidth + si.ActualWidth) - ViewportWidth;

                //The selected item has extra width, so add that to the offset.
                var hMargin = !leftItems.Any(ti => ti.IsSelected) && !si.IsSelected ? Margin.Left + Margin.Right : 0;
                currentHorizontalOffset += hMargin;

                ScrollToHorizontalOffset(currentHorizontalOffset);
                return;
            }

            //if the selected item somewhere to the left.
            if (leftItemsWidth < HorizontalOffset)
            {
                var currentHorizontalOffset = leftItemsWidth;

                //The selected item has extra width, so remove that from the offset
                var hMargin = leftItems.Any(ti => ti.IsSelected) ? Margin.Left + Margin.Right : 0;
                currentHorizontalOffset -= hMargin;

                ScrollToHorizontalOffset(currentHorizontalOffset);
            }
        }
    }
}