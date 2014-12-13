using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Basic class of a Hideable TabControl.
    /// </summary>
    public class HideableTabControl : TabControl
    {
        #region Variables

        private Button _button;
        private TabPanel _tabPanel;
        private Border _border;

        #endregion

        static HideableTabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HideableTabControl), new FrameworkPropertyMetadata(typeof(HideableTabControl)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _button = Template.FindName("HideGrid", this) as Button;
            _tabPanel = Template.FindName("TabPanel", this) as TabPanel;
            _border = Template.FindName("ContentBorder", this) as Border;

            if (_button != null)
                _button.PreviewMouseUp += Button_Clicked;

            if (_tabPanel != null)
                foreach (TabItem tabItem in _tabPanel.Children)
                {
                    tabItem.PreviewMouseDown += TabItem_PreviewMouseDown;
                }
        }

        private void TabItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selected = sender as TabItem;

            if (selected != null)
                selected.IsSelected = true;

            _button.Visibility = Visibility.Visible;
            _border.Visibility = Visibility.Visible;
        }

        private void Button_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach (TabItem child in _tabPanel.Children)
            {
                child.IsSelected = false;
            }

            _border.Visibility = Visibility.Collapsed;
            _button.Visibility = Visibility.Collapsed;
        }
    }
}
