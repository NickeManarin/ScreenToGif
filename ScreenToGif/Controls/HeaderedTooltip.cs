﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ScreenToGif.Controls
{
    [ContentProperty("Inlines")]
    [TemplatePart(Name = "PART_InlinesPresenter", Type = typeof(TextBlock))]
    public class HeaderedTooltip : ToolTip
    {
        #region Variables

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(HeaderedTooltip),
            new FrameworkPropertyMetadata("Header"));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(HeaderedTooltip),
            new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register("TextAlignment", typeof(TextAlignment),
            typeof(HeaderedTooltip), new FrameworkPropertyMetadata(TextAlignment.Left));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement),
            typeof(HeaderedTooltip));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(HeaderedTooltip),
            new FrameworkPropertyMetadata(14.0));

        private Collection<Inline> _inlines = new Collection<Inline>();
        private TextBlock _inlinesPresenter = null;

        #endregion

        #region Properties

        /// <summary>
        /// The header of the tooltip.
        /// </summary>
        [Description("The header of the tooltip.")]
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetCurrentValue(HeaderProperty, value);
        }

        /// <summary>
        /// The text of the description.
        /// </summary>
        [Description("The text of the description.")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetCurrentValue(TextProperty, value);
        }

        /// <summary>
        /// The text alignment of the description.
        /// </summary>
        [Description("The text alignment of the description.")]
        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetCurrentValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// The Image of the Tooltip.
        /// </summary>
        [Description("The Image of the Tooltip.")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetCurrentValue(ImageProperty, value);
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image.")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetCurrentValue(MaxSizeProperty, value);
        }

        public Collection<Inline> Inlines
        {
            get => _inlines;
            set
            {
                _inlines = value;

                UpdateInlines();
            }
        }

        #endregion

        static HeaderedTooltip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedTooltip), new FrameworkPropertyMetadata(typeof(HeaderedTooltip)));
        }

        public override void OnApplyTemplate()
        {
            base.ApplyTemplate();

            _inlinesPresenter = GetTemplateChild("PART_InlinesPresenter") as TextBlock;

            if (_inlinesPresenter != null && Inlines.Any())
            {
                Text = "";

                var targetInlines = _inlinesPresenter.Inlines;

                foreach (var inline in Inlines)
                {
                    targetInlines.Add(inline);
                }
            }
        }

        public void Clear()
        {
            Text = "";
            Inlines.Clear();
        }

        public void UpdateInlines()
        {
            if (_inlinesPresenter == null)
                return;

            _inlinesPresenter.Inlines.Clear();
            _inlinesPresenter.Inlines.AddRange(Inlines);
        }
    }
}