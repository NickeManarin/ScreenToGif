using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Model
{
    internal class ApplicationViewModel : ApplicationModel
    {
        #region Commands

        public ICommand OpenLauncher
        {
            get
            {
                return new RelayCommand
                {
                    ExecuteAction = a =>
                    {
                        var startup = Application.Current.Windows.OfType<Startup>().FirstOrDefault();

                        if (startup == null)
                        {
                            startup = new Startup();
                            startup.Closed += (sender, args) => { CloseOrNot(); };

                            startup.Show();
                        }
                        else
                        {
                            if (startup.WindowState == WindowState.Minimized)
                                startup.WindowState = WindowState.Normal;

                            startup.Activate();
                        }
                    }
                };
            }
        }

        public ICommand OpenRecorder
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        caller?.Hide();

                        if (UserSettings.All.NewRecorder)
                        {
                            var recorderNew = new RecorderNew();
                            recorderNew.Closed += (sender, args) =>
                            {
                                var window = sender as RecorderNew;

                                if (window?.Project != null && window.Project.Any)
                                {
                                    ShowEditor(window.Project);
                                    caller?.Close();
                                }
                                else
                                {
                                    caller?.Show();
                                    CloseOrNot();
                                }
                            };

                            Application.Current.MainWindow = recorderNew;
                            recorderNew.Show();

                            return;
                        }

                        var recorder = new Recorder();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Recorder;

                            if (window?.Project != null && window.Project.Any)
                            {
                                ShowEditor(window.Project);
                                caller?.Close();
                            }
                            else
                            {
                                caller?.Show();
                                CloseOrNot();
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenWebcamRecorder
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        caller?.Hide();

                        var recorder = new Windows.Webcam();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Windows.Webcam;

                            if (window?.Project != null && window.Project.Any)
                            {
                                ShowEditor(window.Project);
                                caller?.Close();
                            }
                            else
                            {
                                caller?.Show();
                                CloseOrNot();
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenBoardRecorder
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        caller?.Hide();

                        var recorder = new Board();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Board;

                            if (window?.Project != null && window.Project.Any)
                            {
                                ShowEditor(window.Project);
                                caller?.Close();
                            }
                            else
                            {
                                caller?.Show();
                                CloseOrNot();
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenEditor
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true, //TODO: Always let this window opens or check if there's any other recorder active?
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;

                        //TODO: Should it behave the same way as it does after a recording? Always open a new one or simply show all/one that was already opened?
                        ShowEditor();

                        caller?.Close();
                    }
                };
            }
        }

        public ICommand OpenOptions
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true, //TODO: Always let this window opens or check if there's any other recorder active?
                    ExecuteAction = a =>
                    {
                        var options = Application.Current.Windows.OfType<Options>().FirstOrDefault();
                        var tab = a as int? ?? 0; //Parameter that selects which tab to be displayed.

                        if (options == null)
                        {
                            options = new Options(tab);
                            options.Closed += (sender, args) =>
                            {
                                CloseOrNot();
                            };

                            //TODO: Open as dialog or not? Block other windows?
                            options.Show();
                        }
                        else
                        {
                            if (options.WindowState == WindowState.Minimized)
                                options.WindowState = WindowState.Normal;

                            options.SelectTab(tab);
                            options.Activate();
                        }
                    }
                };
            }
        }

        public ICommand OpenFeedback
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true, //TODO: Always let this window opens or check if there's any other recorder active?
                    ExecuteAction = a =>
                    {
                        var feedback = Application.Current.Windows.OfType<Feedback>().FirstOrDefault();

                        if (feedback == null)
                        {
                            feedback = new Feedback();
                            feedback.Closed += async (sender, args) =>
                            {
                                await Task.Factory.StartNew(App.MainViewModel.SendFeedback, TaskCreationOptions.LongRunning);

                                CloseOrNot();
                            };

                            feedback.ShowDialog();
                        }
                        else
                        {
                            if (feedback.WindowState == WindowState.Minimized)
                                feedback.WindowState = WindowState.Normal;

                            feedback.Activate();
                        }
                    }
                };
            }
        }

        public ICommand OpenTroubleshoot
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true, //TODO: Always let this window opens or check if there's any other recorder active?
                    ExecuteAction = a =>
                    {
                        var trouble = Application.Current.Windows.OfType<Troubleshoot>().FirstOrDefault();

                        if (trouble == null)
                        {
                            trouble = new Troubleshoot();
                            trouble.Closed += (sender, args) =>
                            {
                                CloseOrNot();
                            };

                            trouble.ShowDialog();
                        }
                        else
                        {
                            if (trouble.WindowState == WindowState.Minimized)
                                trouble.WindowState = WindowState.Normal;

                            trouble.Activate();
                        }
                    }
                };
            }
        }

        public ICommand OpenHelp
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true,
                    ExecuteAction = a =>
                    {
                        try
                        {
                            Process.Start("https://github.com/NickeManarin/ScreenToGif/wiki/Help");
                        }
                        catch (Exception ex)
                        {
                            LogWriter.Log(ex, "Openning the Help link");
                        }
                    }
                };
            }
        }

        public ICommand ExitApplication
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //TODO: Check if there's anything open or anything happening with editors.
                        return Application.Current.Windows.OfType<RecorderWindow>().All(a => a.Stage != Stage.Recording);
                    },
                    ExecuteAction = a =>
                    {
                        if (UserSettings.All.NotifyWhileClosingApp && !Dialog.Ask(LocalizationHelper.Get("Application.Exiting.Title"), LocalizationHelper.Get("Application.Exiting.Instruction"), LocalizationHelper.Get("Application.Exiting.Message")))
                            return;

                        Application.Current.Shutdown(69);
                    }
                };
            }
        }

        #endregion

        #region Methods

        private void ShowEditor(ProjectInfo project = null)
        {
            var editor = Application.Current.Windows.OfType<Editor>().FirstOrDefault(f => f.Project == null || !f.Project.Any);

            if (editor == null)
            {
                editor = new Editor { Project = project };
                editor.Closed += (sender, args) => CloseOrNot();
                editor.Show();
            }
            else
            {
                //TODO: Three modes for opening the editor:
                //Always open a new window.
                //Open a new window if there's no window without any project loaded.
                //Open a new window if there's no idle window (with a project loaded).

                //TODO: Detect if the last state was normal/maximized.
                if (editor.WindowState == WindowState.Minimized)
                    editor.WindowState = WindowState.Normal;

                if (project != null)
                    editor.LoadProject(project, true, false);

                editor.Activate();
            }

            Application.Current.MainWindow = editor;
        }

        private void CloseOrNot()
        {
            //When closed, check if it's the last window, then close if it's the configured behavior.
            if (!UserSettings.All.ShowNotificationIcon || !UserSettings.All.KeepOpen)
            {
                //We only need to check loaded windows that have content
                if (Application.Current.Windows.Cast<Window>().Count(window => window.HasContent) == 0)
                    Application.Current.Shutdown(2);
            }
        }

        internal void ClearTemporaryFilesTask()
        {
            try
            {
                if (!UserSettings.All.AutomaticCleanUp || Global.IsCurrentlyDeletingFiles)
                    return;

                Global.IsCurrentlyDeletingFiles = true;

                var path = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording");

                if (!Directory.Exists(path))
                    return;

                var list = Directory.GetDirectories(path).Select(x => new DirectoryInfo(x))
                    .Where(w => (DateTime.Now - w.CreationTime).TotalDays > (UserSettings.All.AutomaticCleanUpDays > 0 ? UserSettings.All.AutomaticCleanUpDays : 5)).ToList();

                //var list = Directory.GetDirectories(path).Select(x => new DirectoryInfo(x));
                
                foreach (var folder in list)
                {
                    if (MutexList.IsInUse(folder.Name))
                        continue;

                    Directory.Delete(folder.FullName, true);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Automatic clean up");
            }
            finally
            {
                Global.IsCurrentlyDeletingFiles = false;
                CheckDiskSpace();
            }
        }

        internal void CheckDiskSpace()
        {
            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                return;

            try
            {
                var isRelative = !string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder) && !Path.IsPathRooted(UserSettings.All.TemporaryFolder);
                var drive = new DriveInfo((isRelative ? Path.GetFullPath(UserSettings.All.TemporaryFolder) : UserSettings.All.TemporaryFolder).Substring(0, 1));

                Global.AvailableDiskSpacePercentage = drive.AvailableFreeSpace * 100d / drive.TotalSize; //Get the percentage of space left.
                Global.AvailableDiskSpace = drive.AvailableFreeSpace;

                //If there's less than 2GB left.
                if (drive.AvailableFreeSpace < 2000000000)
                    Application.Current.Dispatcher.Invoke(() => NotificationManager.AddNotification(LocalizationHelper.GetWithFormat("Editor.Warning.LowSpace", Math.Round(Global.AvailableDiskSpacePercentage, 2)),
                        StatusType.Warning, "disk", () => App.MainViewModel.OpenOptions.Execute(5)));
                else
                    Application.Current.Dispatcher.Invoke(() => NotificationManager.RemoveNotification(r => r.Tag == "disk"));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while checking the space left in disk");
            }
        }

        internal void SendFeedback()
        {
            try
            {
                var path = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Feedback");

                if (!Directory.Exists(path))
                    return;

                var list = new DirectoryInfo(path).EnumerateFiles("*.html", SearchOption.TopDirectoryOnly);

                foreach (var file in list)
                {
                    //Get zip with same name as file
                    var zip = Path.Combine(file.DirectoryName, file.Name.Replace(".html", ".zip"));

                    List<string> fileList = null;

                    if (File.Exists(zip))
                        fileList = new List<string> { zip };

                    if (!FeedbackHelper.Send(File.ReadAllText(file.FullName), fileList))
                        continue;

                    File.Delete(file.FullName);

                    if (File.Exists(zip))
                        File.Delete(zip);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Automatic feedback");
            }
        }

        internal void UpdateTask()
        {
            Global.UpdateModel = null;

            if (!UserSettings.All.CheckForUpdates)
                return;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/NickeManarin/ScreenToGif/releases/latest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
                request.Proxy = WebHelper.GetProxy();

                var response = (HttpWebResponse)request.GetResponse();

                using (var resultStream = response.GetResponseStream())
                {
                    if (resultStream == null)
                        return;

                    using (var reader = new StreamReader(resultStream))
                    {
                        var result = reader.ReadToEnd();

                        var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new System.Xml.XmlDictionaryReaderQuotas());

                        var release = XElement.Load(jsonReader);

                        var version = Version.Parse(release.XPathSelectElement("tag_name")?.Value ?? "0.1");

                        if (version.Major == 0 || version <= Assembly.GetExecutingAssembly().GetName().Version)
                            return;

                        Global.UpdateModel = new UpdateModel
                        {
                            Version = version,
                            Description = release.XPathSelectElement("body")?.Value ?? "",
                            InstallerDownloadUrl = release.XPathSelectElement("assets/item[2]/browser_download_url")?.Value ?? "",
                            InstallerSize = Convert.ToInt64(release.XPathSelectElement("assets/item[2]/size")?.Value ?? "0"),
                            PortableDownloadUrl = release.XPathSelectElement("assets/item[1]/browser_download_url")?.Value ?? "",
                            PortableSize = Convert.ToInt64(release.XPathSelectElement("assets/item[1]/size")?.Value ?? "0")
                        };

                        Application.Current.Dispatcher.Invoke(() => NotificationManager.AddNotification(string.Format(LocalizationHelper.Get("Update.NewRelease.Verbose"), Global.UpdateModel.Version), StatusType.Update, "update", UpdateAction));
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Check for update task");
            }

            GC.Collect();
        }

        internal void UpdateAction()
        {
            if (Global.UpdateModel == null)
                return;

            var download = new DownloadDialog();
            var result = download.ShowDialog();

            if (result.HasValue && result.Value)
            {
                NotificationManager.RemoveNotification(s => s.Kind == StatusType.Update);

                //TODO: Check if possible to close.

                if (Dialog.Ask("ScreenToGif", LocalizationHelper.Get("Update.CloseThis"), LocalizationHelper.Get("Update.CloseThis.Detail")))
                    Application.Current.Shutdown(69);
            }
        }

        #endregion
    }

    internal class RelayCommand : ICommand
    {
        public Predicate<object> CanExecutePredicate { get; set; }
        public Action<object> ExecuteAction { get; set; }

        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
        {
            CanExecutePredicate = canExecute;
            ExecuteAction = execute;
        }

        public RelayCommand()
        { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return CanExecutePredicate == null || CanExecutePredicate(parameter);
        }

        public void Execute(object parameter)
        {
            ExecuteAction(parameter);
        }
    }

    internal class AdvancedRelayCommand : RoutedUICommand, ICommand
    {
        public Predicate<object> CanExecutePredicate { get; set; }
        public Action<object> ExecuteAction { get; set; }

        public AdvancedRelayCommand()
        { }

        public AdvancedRelayCommand(string text, string name, Type ownerType, InputGestureCollection inputGestures) : base(text, name, ownerType, inputGestures)
        { }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecutePredicate == null || CanExecutePredicate(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAction(parameter);
        }

        //public bool CanExecute(object parameter)
        //{
        //    return CanExecutePredicate == null || CanExecutePredicate(parameter);
        //}

        //public void Execute(object parameter)
        //{
        //    ExecuteAction(parameter);
        //}
    }
}