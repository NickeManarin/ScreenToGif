using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class HierarchicalItem : TreeViewItem
    {
        #region Dependency Properties

        public static readonly DependencyProperty FullPathProperty = DependencyProperty.Register("FullPath", typeof(string), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender, Path_PropertyChanged));

        public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register("Category", typeof(DirectoryType), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(DirectoryType.Folder));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(16.0));

        #endregion

        #region Property Accessors

        [Bindable(true), Category("Common")]
        public string FullPath
        {
            get { return (string)GetValue(FullPathProperty); }
            set { SetValue(FullPathProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public DirectoryType Category
        {
            get { return (DirectoryType)GetValue(CategoryProperty); }
            set { SetValue(CategoryProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public UIElement Image
        {
            get { return (UIElement)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public double MaxSize
        {
            get { return (double)GetValue(MaxSizeProperty); }
            set { SetValue(MaxSizeProperty, value); }
        }

        #endregion

        private static void Path_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as HierarchicalItem;

            if (item == null)
                return;

            item.Header = new DirectoryInfo(item.FullPath).Name;
        }

        static HierarchicalItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HierarchicalItem), new FrameworkPropertyMetadata(typeof(HierarchicalItem)));
        }

        protected override void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);

            try
            {
                #region Loads only the children that was not loaded before

                var folders = Directory.GetDirectories(FullPath).Where(x => !Items.OfType<HierarchicalItem>().Any(y => y.FullPath.Equals(x)));

                foreach (var folder in folders)
                {
                    Items.Add(new HierarchicalItem
                    {
                        FullPath = folder,
                        Category = DirectoryType.Folder
                    });
                }

                if (Items.Count == 0)
                    IsExpanded = false;

                #endregion
            }
            catch (Exception)
            {
                Category = DirectoryType.Protected;
            }
            
            #region Remove the innexistent folders

            var items = Items.OfType<HierarchicalItem>();

            foreach (var item in items)
            {
                if (!Directory.Exists(item.FullPath))
                    Items.Remove(item);
            }

            #endregion

            if (!Items.IsEmpty)
            {
                var first = Items[0] as HierarchicalItem;

                if (first != null)
                {
                    first.IsSelected = true;
                    Keyboard.Focus(first);
                    //first.Focus();
                }
            }
            //UpdateLayout();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.Return)
            {
                IsExpanded = !IsExpanded;
                return;
            }

            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                IsExpanded = true;
                return;
            }
        }
    }
}
