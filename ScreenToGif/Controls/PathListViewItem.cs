using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class PathListViewItem : ListViewItem
    {
        #region Dependency Properties

        public static readonly DependencyProperty FullPathProperty = DependencyProperty.Register("FullPath", typeof(string), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, Path_PropertyChanged));

        public static readonly DependencyProperty HasFoldersProperty = DependencyProperty.Register("HasFolders", typeof(bool), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty HasFilesProperty = DependencyProperty.Register("HasFiles", typeof(bool), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register("Category", typeof(PathType), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(PathType.Folder));

        public static readonly DependencyProperty IsInaccessibleProperty = DependencyProperty.Register("IsInaccessible", typeof(bool), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(PathListViewItem),
            new FrameworkPropertyMetadata(18.0));

        #endregion

        #region Properties

        /// <summary>
        /// The full path.
        /// </summary>
        [Description("The full path."), Bindable(true), Category("Common")]
        public string FullPath
        {
            get => (string)GetValue(FullPathProperty);
            set => SetCurrentValue(FullPathProperty, value);
        }

        /// <summary>
        /// True if it has folders inside.
        /// </summary>
        [Description("True if it has folders inside."), Bindable(true), Category("Common")]
        public bool HasFolders
        {
            get => (bool)GetValue(HasFoldersProperty);
            set => SetCurrentValue(HasFoldersProperty, value);
        }

        /// <summary>
        /// True if it has files inside.
        /// </summary>
        [Description("True if it has files inside."), Bindable(true), Category("Common")]
        public bool HasFiles
        {
            get => (bool)GetValue(HasFilesProperty);
            set => SetCurrentValue(HasFilesProperty, value);
        }

        /// <summary>
        /// The type of the path.
        /// </summary>
        [Description("The type of the path."), Bindable(true), Category("Common")]
        public PathType Category
        {
            get => (PathType)GetValue(CategoryProperty);
            set => SetCurrentValue(CategoryProperty, value);
        }

        /// <summary>
        /// If the path is inaccessible.
        /// </summary>
        [Description("If the path is inaccessible."), Bindable(true), Category("Common")]
        public bool IsInaccessible
        {
            get => (bool)GetValue(IsInaccessibleProperty);
            set => SetCurrentValue(IsInaccessibleProperty, value);
        }

        /// <summary>
        /// The display text.
        /// </summary>
        [Description("The display text."), Bindable(true), Category("Common")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetCurrentValue(TextProperty, value);
        }

        /// <summary>
        /// The image/icon of the element.
        /// </summary>
        [Description("The image/icon of the element."), Bindable(true), Category("Common")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        /// <summary>
        /// The maximum size of the element.
        /// </summary>
        [Bindable(true), Category("Common")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetValue(MaxSizeProperty, value);
        }

        #endregion

        static PathListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PathListViewItem), new FrameworkPropertyMetadata(typeof(PathListViewItem)));
        }

        private static async void Path_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as PathListViewItem;

            if (item == null)
                return;

            if (item.Category == PathType.VirtualFolder)
                return;

            var info = new DirectoryInfo(item.FullPath);

            if (item.Text == null)
                item.Text = info.Name;

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
    }
}