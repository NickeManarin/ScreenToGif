using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;

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

        #region Methods

        private void AddRoot(DirectoryType type, string header = null, string path = null)
        {
            Dispatcher.Invoke(() =>
            {
                MainTreeView.Items.Add(new HierarchicalItem
                {
                    Category = type,
                    IsExpanded = true,
                    Header = header,
                    FullPath = path
                });
            });
        }

        private void AddFolders(string path, DirectoryType type, string header = null, string description = null)
        {
            Dispatcher.Invoke(() =>
            {
                ((HierarchicalItem)MainTreeView.Items[0]).Items.Add(new HierarchicalItem
                {
                    Category = type,
                    Header = header,
                    FullPath = path,
                    Description = description
                });
            });
        }

        #endregion

        #region Async Loading

        private delegate bool Crawl(string path);

        private Crawl _crawlDel;

        private bool CrawlFolders(string path)
        {
            try
            {
                #region This Computer as root

                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    AddRoot(DirectoryType.ThisComputer, "This Computer"); //TODO: Localize or add machine's name.

                    AddFolders(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), DirectoryType.Desktop);
                    AddFolders(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DirectoryType.Documents);
                    AddFolders(Native.GetKnowFolderPath(Native.KnownFolder.Downloads), DirectoryType.Downloads);
                    AddFolders(Native.GetKnowFolderPath(Native.KnownFolder.Pictures), DirectoryType.Images);
                    AddFolders(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), DirectoryType.Music);
                    AddFolders(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), DirectoryType.Videos);

                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        var header = drive.IsReady ? $"{drive.VolumeLabel} ({drive.Name})" : drive.Name;
                        AddFolders(drive.Name, DirectoryType.Drive, header, drive.IsReady ? "" : "Unavailable");
                    }

                    return true;
                }

                #endregion

                #region Selected path as root

                AddRoot(DirectoryType.Folder, null, path);

                foreach (var drive in Directory.GetDirectories(path))
                {
                    AddFolders(drive, DirectoryType.Folder);
                }

                return true;

                #endregion
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

                    //Selects the first child element of the first element.
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

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            _crawlDel = CrawlFolders;
            _crawlDel.BeginInvoke(RootPath, CrawlCallback, null);
        }

        private async void MainTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = e.NewValue as HierarchicalItem;

            if (item == null)
                return;

            //TODO: Show folders, with icons
            //If no folder present inside, show option to select the current folder anyway.

            #region This Computer (a virtual folder)

            if (item.Category == DirectoryType.ThisComputer)
            {
                var drives = Task<DriveInfo[]>.Factory.StartNew(DriveInfo.GetDrives);

                MainListView.ItemsSource = await drives;
                return;
            }

            #endregion

            #region Folders

            var info = new DirectoryInfo(item.FullPath);

            var result = Task<IEnumerable<string>>.Factory.StartNew(() =>
            {
                try
                {
                    return info.EnumerateDirectories().Select(x => x.FullName);
                }
                catch (Exception)
                {
                    return new List<string>();
                }
            });

            var pathList = new List<PathListViewItem>();
            foreach (var directory in await result)
            {
                pathList.Add(new PathListViewItem
                {
                    FullPath = directory,
                    Image = FindResource("Vector.Folder") as Canvas,
                });
            }

            MainListView.ItemsSource = pathList;

            #endregion
        }

        private void Ok_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //TODO: Multiple Files/Folders.
            e.CanExecute = MainListView.SelectedItem != null;
        }

        private void Ok_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Validate, Select itens.

            GC.Collect(1);

            DialogResult = true;
        }

        private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion
    }
}
