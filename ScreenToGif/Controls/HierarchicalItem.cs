using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public static readonly DependencyProperty HasFoldersProperty = DependencyProperty.Register("HasFolders", typeof(bool), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register("Category", typeof(DirectoryType), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(DirectoryType.Folder));

        public static readonly DependencyProperty IsInaccessibleProperty = DependencyProperty.Register("IsInaccessible", typeof(bool), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(HierarchicalItem),
            new FrameworkPropertyMetadata(18.0));

        #endregion

        #region Property Accessors

        [Bindable(true), Category("Common")]
        public string FullPath
        {
            get => (string)GetValue(FullPathProperty);
            set => SetValue(FullPathProperty, value);
        }

        [Bindable(true), Category("Common")]
        public bool HasFolders
        {
            get => (bool)GetValue(HasFoldersProperty);
            set => SetValue(HasFoldersProperty, value);
        }

        [Bindable(true), Category("Common")]
        public DirectoryType Category
        {
            get => (DirectoryType)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        [Bindable(true), Category("Common")]
        public bool IsInaccessible
        {
            get => (bool)GetValue(IsInaccessibleProperty);
            set => SetValue(IsInaccessibleProperty, value);
        }

        [Bindable(true), Category("Common")]
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        [Bindable(true), Category("Common")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        [Bindable(true), Category("Common")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetValue(MaxSizeProperty, value);
        }

        #endregion

        private static async void Path_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as HierarchicalItem;

            if (item == null)
                return;

            if (item.Category == DirectoryType.ThisComputer)
                return;

            var info = new DirectoryInfo(item.FullPath);

            if (item.Header == null)
                item.Header = info.Name;

            #region Verifies if there's at least one folder inside this current one

            var result = Task<bool>.Factory.StartNew(() =>
            {
                try
                {
                    return info.EnumerateDirectories().Any();
                }
                catch (Exception)
                {
                    item.Dispatcher.Invoke(() => item.IsInaccessible = true);
                    return false;
                }
            });

            item.HasFolders = await result;

            #endregion
        }

        static HierarchicalItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HierarchicalItem), new FrameworkPropertyMetadata(typeof(HierarchicalItem)));
        }

        protected override async void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);

            //TODO: load new drives or remove the removed.
            if (Category == DirectoryType.ThisComputer)
            {
                SelectFirst();
                return;
            }

            try
            {
                #region Loads only the children that was not loaded before

                var folders = Directory.GetDirectories(FullPath)
                    .Where(x => !Items.OfType<HierarchicalItem>().Any(y => y.FullPath.Equals(x)));

                var result = Task.Factory.StartNew(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var folder in folders)
                        {
                            Items.Add(new HierarchicalItem
                            {
                                FullPath = folder,
                                Category = DirectoryType.Folder
                            });
                        }
                    });
                });

                await result;

                IsExpanded = Items != null && Items.Count > 0;

                #endregion
            }
            catch (Exception)
            {
                IsInaccessible = true;
            }

            #region Remove the innexistent folders

            var items = Items.OfType<HierarchicalItem>();

            foreach (var item in items)
            {
                if (!Directory.Exists(item.FullPath))
                    Items.Remove(item);
            }

            #endregion

            SelectFirst();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.Return)
            {
                IsExpanded = !IsExpanded;
                return;
            }

            if (e.Key == Key.Right)
            {
                IsExpanded = true;
                return;
            }

            if (e.Key == Key.Left)
            {
                IsExpanded = false;
                return;
            }
        }

        /// <summary>
        /// Selects and sets focus to the first child element.
        /// </summary>
        private void SelectFirst()
        {
            if (Items == null || Items.IsEmpty)
                return;

            var first = Items[0] as HierarchicalItem;

            if (first == null)
                return;

            first.IsSelected = true;
            Keyboard.Focus(first);
        }
    }
}