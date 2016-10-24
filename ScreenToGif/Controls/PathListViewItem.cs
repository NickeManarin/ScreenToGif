using System;
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
    }
}
