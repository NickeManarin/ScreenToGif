using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.Writers;

namespace ScreenToGif.Windows.Other
{
    public partial class SelectFolderDialog : Window
    {
        #region Variables

        private readonly ObservableCollection<HierarchicalItem> _rootItems = new ObservableCollection<HierarchicalItem>();

        #endregion

        #region Properties

        public string RootPath { get; set; }

        public string SelectedPath { get; set; }

        #endregion

        public SelectFolderDialog()
        {
            InitializeComponent();
        }

        private void AddRoot(string path, DirectoryType type)
        {
            Dispatcher.Invoke(() =>
            {
                MainTreeView.Items.Add(new HierarchicalItem
                {
                    FullPath = path,
                    Category = type
                });
            });
        }

        private void AddChilds(HierarchicalItem parent, string path)
        {
            Dispatcher.Invoke(() =>
            {
                if (parent == null)
                    return;

                parent.Items.Add(new HierarchicalItem
                {
                    FullPath = path,
                    Category = DirectoryType.Folder
                });
            });
        }

        #region Async Loading

        private delegate bool Crawl(string path);

        private Crawl _crawlDel;

        private bool CrawlFolders(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        AddRoot(drive.Name, DirectoryType.Drive);
                    }
                }
                else
                {
                    foreach (var drive in Directory.GetDirectories(path))
                    {
                        AddRoot(drive, DirectoryType.Folder);
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    var item = MainTreeView.Items[0] as HierarchicalItem;

                    if (item != null)
                        item.IsSelected = true;
                });


                //Load selected item's files and folders onto the listview. 

                Dispatcher.Invoke(() => StatusBand.Hide());
                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Load root folders");
                Dispatcher.Invoke(() => StatusBand.Error("Error while loading folders: " + ex.Message));
                return false;
            }
        }

        private void CrawlCallback(IAsyncResult ar)
        {
            var result = _crawlDel.EndInvoke(ar);

            if (result)
            {
                Dispatcher.Invoke(() =>
                {
                    MainTreeView.Focus();
                    MainTreeView.Items.MoveCurrentToFirst();

                    var first = MainTreeView.Items.OfType<HierarchicalItem>().FirstOrDefault();

                    Keyboard.Focus(first);
                    first?.Focus();
                });
            }

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.Arrow;
            });

            GC.Collect();
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusBand.Info("Loading folders...");

            _crawlDel = CrawlFolders;
            _crawlDel.BeginInvoke(RootPath, CrawlCallback, null);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect(1);

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
