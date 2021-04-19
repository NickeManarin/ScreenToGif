﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Linq;
using System.Xml.XPath;
using Windows.Graphics.Capture;
using ScreenToGif.Controls;
using ScreenToGif.SystemCapture;
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
                        return Application.Current?.Windows.OfType<Window>().All(a => !(a is RecorderWindow)) ?? false;
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        var editor = a as Editor;

                        if (editor == null)
                            caller?.Hide();

                        if (UserSettings.All.NewRecorder)
                        {
                            var recorderNew = new NewRecorder();
                            recorderNew.Closed += (sender, args) =>
                            {
                                var window = sender as NewRecorder;

                                if (window?.Project != null && window.Project.Any)
                                {
                                    if (editor == null)
                                    {
                                        ShowEditor(window.Project);
                                        caller?.Close();
                                    }
                                    else
                                        editor.RecorderCallback(window.Project);
                                }
                                else
                                {
                                    if (editor == null)
                                    {
                                        caller?.Show();
                                        CloseOrNot();
                                    }
                                    else
                                        editor.RecorderCallback(null);
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
                                if (editor == null)
                                {
                                    ShowEditor(window.Project);
                                    caller?.Close();
                                }
                                else
                                    editor.RecorderCallback(window.Project);
                            }
                            else
                            {
                                if (editor == null)
                                {
                                    caller?.Show();
                                    CloseOrNot();
                                }
                                else
                                    editor.RecorderCallback(null);
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenSystemCapture
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current?.Windows.OfType<Window>().All(a => !(a is RecorderWindow)) ?? false;
                    },
                    ExecuteAction = async a =>
                    {
                        if (!GraphicsCaptureSession.IsSupported())
                        {
                            MessageBox.Show("Not supported");
                            return;
                        }

                        var caller = a as Window;
                        if (caller == null)
                            return;

                        var picker = new GraphicsCapturePicker();
                        var hwnd = new WindowInteropHelper(caller).Handle;
                        picker.SetWindow(hwnd);
                        var item = await picker.PickSingleItemAsync();
                        if (item == null)
                            return;

                        var editor = a as Editor;

                        if (editor == null)
                            caller.Hide();

                        var systemCapture = new Windows.SystemCapture(item);
                        systemCapture.Closed += (sender, args) =>
                        {
                            var window = sender as Windows.SystemCapture;

                            if (window?.Project != null && window.Project.Any)
                            {
                                if (editor == null)
                                {
                                    ShowEditor(window.Project);
                                    caller.Close();
                                }
                                else
                                    editor.RecorderCallback(window.Project);
                            }
                            else
                            {
                                if (editor == null)
                                {
                                    caller.Show();
                                    CloseOrNot();
                                }
                                else
                                    editor.RecorderCallback(null);
                            }
                        };

                        Application.Current.MainWindow = systemCapture;
                        systemCapture.Show();
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
                        return Application.Current?.Windows.OfType<Window>().All(a => !(a is RecorderWindow)) ?? false;
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        var editor = a as Editor;

                        if (editor == null)
                            caller?.Hide();

                        var recorder = new Windows.Webcam();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Windows.Webcam;

                            if (window?.Project != null && window.Project.Any)
                            {
                                if (editor == null)
                                {
                                    ShowEditor(window.Project);
                                    caller?.Close();
                                }
                                else
                                    editor.RecorderCallback(window.Project);
                            }
                            else
                            {
                                if (editor == null)
                                {
                                    caller?.Show();
                                    CloseOrNot();
                                }
                                else
                                    editor.RecorderCallback(null);
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
                        return Application.Current?.Windows.OfType<Window>().All(a => !(a is RecorderWindow)) ?? false;
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        var editor = a as Editor;

                        if (editor == null)
                            caller?.Hide();

                        var recorder = new Board();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Board;

                            if (window?.Project != null && window.Project.Any)
                            {
                                if (editor == null)
                                {
                                    ShowEditor(window.Project);
                                    caller?.Close();
                                }
                                else
                                    editor.RecorderCallback(window.Project);
                            }
                            else
                            {
                                if (editor == null)
                                {
                                    caller?.Show();
                                    CloseOrNot();
                                }
                                else
                                    editor.RecorderCallback(null);
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
                        ShowEditor(null, a is string[]);

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
                    CanExecutePredicate = a => true,
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

        public ICommand TrayLeftClick
        {
            get
            {
                return new RelayCommand
                {
                    ExecuteAction = a => Interact(UserSettings.All.LeftClickAction, UserSettings.All.LeftOpenWindow)
                };
            }
        }

        public ICommand TrayDoubleLeftClick
        {
            get
            {
                return new RelayCommand
                {
                    ExecuteAction = a => Interact(UserSettings.All.DoubleLeftClickAction, UserSettings.All.DoubleLeftOpenWindow)
                };
            }
        }

        public ICommand TrayMiddleClick
        {
            get
            {
                return new RelayCommand
                {
                    ExecuteAction = a => Interact(UserSettings.All.MiddleClickAction, UserSettings.All.MiddleOpenWindow)
                };
            }
        }

        public ICommand PromptUpdate
        {
            get
            {
                return new RelayCommand
                {
                    ExecuteAction = a =>
                    {
                        if (Global.UpdateAvailable == null)
                            return;

                        //Try to install the update, closing the app if sucessful.
                        if (InstallUpdate(true))
                            Application.Current.Shutdown(69);
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
                        return Application.Current?.Windows.OfType<RecorderWindow>().All(a => a.Stage != Stage.Recording) ?? false;
                    },
                    ExecuteAction = a =>
                    {
                        if (UserSettings.All.NotifyWhileClosingApp && !Dialog.Ask(LocalizationHelper.Get("S.Exiting.Title"), LocalizationHelper.Get("S.Exiting.Instruction"), LocalizationHelper.Get("S.Exiting.Message")))
                            return;

                        Application.Current.Shutdown(69);
                    }
                };
            }
        }

        #endregion

        #region Methods

        private void ShowEditor(ProjectInfo project = null, bool openMedia = false)
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
                else if (openMedia)
                    editor.LoadFromArguments();
            }

            Application.Current.MainWindow = editor;
            editor.Activate();
        }

        private void CloseOrNot()
        {
            //When closed, check if it's the last window, then close if it's the configured behavior.
            if (!UserSettings.All.ShowNotificationIcon || !UserSettings.All.KeepOpen)
            {
                //We only need to check loaded windows that have content, since any special window could be open.
                if (Application.Current.Windows.Cast<Window>().Count(window => window.HasContent) == 0)
                {
                    //Install the available update on closing.
                    if (UserSettings.All.InstallUpdates)
                        InstallUpdate();

                    Application.Current.Shutdown(2);
                }
            }
        }

        private void Interact(int action, int open)
        {
            switch (action)
            {
                case 1: //Open a window.
                    {
                        switch (open)
                        {
                            case 1: //Startup.
                                {
                                    OpenLauncher.Execute(null);
                                    break;
                                }
                            case 2: //Recorder.
                                {
                                    if (!OpenRecorder.CanExecute(null))
                                    {
                                        var rec = Application.Current.Windows.OfType<RecorderWindow>().FirstOrDefault();

                                        if (rec != null)
                                        {
                                            if (rec.WindowState == WindowState.Minimized)
                                                rec.WindowState = WindowState.Normal;

                                            //Bring to foreground.
                                            rec.Activate();
                                            return;
                                        }
                                    }

                                    OpenRecorder.Execute(null);
                                    return;
                                }
                            case 3: //Webcam.
                                {
                                    if (!OpenWebcamRecorder.CanExecute(null))
                                    {
                                        var rec = Application.Current.Windows.OfType<RecorderWindow>().FirstOrDefault();

                                        if (rec != null)
                                        {
                                            if (rec.WindowState == WindowState.Minimized)
                                                rec.WindowState = WindowState.Normal;

                                            //Bring to foreground.
                                            rec.Activate();
                                            return;
                                        }
                                    }

                                    OpenWebcamRecorder.Execute(null);
                                    break;
                                }
                            case 4: //Board.
                                {
                                    if (!OpenBoardRecorder.CanExecute(null))
                                    {
                                        var rec = Application.Current.Windows.OfType<RecorderWindow>().FirstOrDefault();

                                        if (rec != null)
                                        {
                                            if (rec.WindowState == WindowState.Minimized)
                                                rec.WindowState = WindowState.Normal;

                                            //Bring to foreground.
                                            rec.Activate();
                                            return;
                                        }
                                    }

                                    OpenBoardRecorder.Execute(null);
                                    break;
                                }
                            case 5: //Editor.
                                {
                                    OpenEditor.Execute(null);
                                    break;
                                }
                        }

                        break;
                    }

                case 2: //Minimize/restore all windows.
                    {
                        var all = Application.Current.Windows.OfType<Window>().Where(w => w.Content != null).ToList();

                        if (all.Count == 0)
                        {
                            Interact(1, open);
                            return;
                        }

                        if (all.Any(n => n.WindowState != WindowState.Minimized))
                        {
                            //Minimize all windows.
                            foreach (var window in all)
                                window.WindowState = WindowState.Minimized;
                        }
                        else
                        {
                            //Restore all windows.
                            foreach (var window in all)
                                window.WindowState = WindowState.Normal;
                        }

                        break;
                    }

                case 3: //Minimize all windows.
                    {
                        var all = Application.Current.Windows.OfType<Window>().Where(w => w.Content != null).ToList();

                        if (all.Count == 0)
                        {
                            Interact(1, open);
                            return;
                        }

                        foreach (var window in all)
                            window.WindowState = WindowState.Minimized;

                        break;
                    }

                case 4: //Restore all windows.
                    {
                        var all = Application.Current.Windows.OfType<Window>().Where(w => w.Content != null).ToList();

                        if (all.Count == 0)
                        {
                            Interact(1, open);
                            return;
                        }

                        foreach (var window in all)
                            window.WindowState = WindowState.Normal;

                        break;
                    }
            }
        }

        internal void ClearTemporaryFiles()
        {
            try
            {
                if (!UserSettings.All.AutomaticCleanUp || Global.IsCurrentlyDeletingFiles || string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolderResolved))
                    return;

                Global.IsCurrentlyDeletingFiles = true;

                ClearRecordingCache();
                ClearUpdateCache();
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

        private void ClearRecordingCache()
        {
            try
            {
                var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Recording");

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
                LogWriter.Log(ex, "Automatic clean up - Recordings");
            }
        }

        private void ClearUpdateCache()
        {
            try
            {
                var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Updates");

                if (!Directory.Exists(path))
                    return;

                var list = Directory.EnumerateFiles(path).Select(x => new FileInfo(x))
                    .Where(w => (DateTime.Now - w.CreationTime).TotalDays > (UserSettings.All.AutomaticCleanUpDays > 0 ? UserSettings.All.AutomaticCleanUpDays : 5)).ToList();

                foreach (var file in list)
                    File.Delete(file.FullName);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Automatic clean up - Updates");
            }
        }

        internal void CheckDiskSpace()
        {
            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolderResolved))
                return;

            try
            {
                var isRelative = !string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolderResolved) && !Path.IsPathRooted(UserSettings.All.TemporaryFolderResolved);
                var drive = new DriveInfo((isRelative ? Path.GetFullPath(UserSettings.All.TemporaryFolderResolved) : UserSettings.All.TemporaryFolderResolved).Substring(0, 1));

                Global.AvailableDiskSpacePercentage = drive.AvailableFreeSpace * 100d / drive.TotalSize; //Get the percentage of space left.
                Global.AvailableDiskSpace = drive.AvailableFreeSpace;

                //If there's less than 2GB left.
                if (drive.AvailableFreeSpace < 2_000_000_000)
                    Application.Current.Dispatcher?.Invoke(() => NotificationManager.AddNotification(LocalizationHelper.GetWithFormat("S.Editor.Warning.LowSpace", Math.Round(Global.AvailableDiskSpacePercentage, 2)),
                        StatusType.Warning, "disk", () => App.MainViewModel.OpenOptions.Execute(Options.TempFilesIndex)));
                else
                    Application.Current.Dispatcher?.Invoke(() => NotificationManager.RemoveNotification(r => r.Tag == "disk"));
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
                if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolderResolved))
                    return;

                var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Feedback");

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

        internal async void CheckForUpdates()
        {
            Global.UpdateAvailable = null;

#if UWP
            return;
#endif

            if (!UserSettings.All.CheckForUpdates)
                return;

            //Try checking for the update on Github first then fallbacks to Fosshub.
            if (!await CheckOnGithub())
                await CheckOnFosshub();
        }

        private async Task<bool> CheckOnGithub()
        {
            try
            {
                //If the app was installed by Chocolatey, avoid this.
                if (AppDomain.CurrentDomain.BaseDirectory.EndsWith(@"Chocolatey\lib\screentogif\content\"))
                    return true;

                #region GraphQL equivalent

                //query {
                //    repository(owner: "NickeManarin", name: "ScreenToGif") {
                //        releases(first: 1, orderBy: { field: CREATED_AT, direction: DESC}) {
                //            nodes {
                //                name
                //                tagName
                //                createdAt
                //                url
                //                isPrerelease
                //                description
                //                releaseAssets(last: 2) {
                //                    nodes {
                //                        name
                //                        downloadCount
                //                        downloadUrl
                //                        size
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}

                #endregion

                var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/NickeManarin/ScreenToGif/releases/latest");
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
                request.Proxy = WebHelper.GetProxy();

                var response = (HttpWebResponse)await request.GetResponseAsync();

                using (var resultStream = response.GetResponseStream())
                {
                    if (resultStream == null)
                        return false;

                    using (var reader = new StreamReader(resultStream))
                    {
                        var result = await reader.ReadToEndAsync();
                        var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new System.Xml.XmlDictionaryReaderQuotas());
                        var release = XElement.Load(jsonReader);

                        var version = Version.Parse(release.XPathSelectElement("tag_name")?.Value ?? "0.1");

                        if (version.Major == 0 || version <= Assembly.GetExecutingAssembly().GetName().Version)
                            return true;

                        Global.UpdateAvailable = new UpdateAvailable
                        {
                            Version = version,
                            Description = release.XPathSelectElement("body")?.Value ?? "",

                            PortableDownloadUrl = release.XPathSelectElement("assets/item[1]/browser_download_url")?.Value ?? "",
                            PortableSize = Convert.ToInt64(release.XPathSelectElement("assets/item[1]/size")?.Value ?? "0"),
                            PortableName = release.XPathSelectElement("assets/item[1]/name")?.Value ?? "ScreenToGif.zip",

                            InstallerDownloadUrl = release.XPathSelectElement("assets/item[2]/browser_download_url")?.Value ?? "",
                            InstallerSize = Convert.ToInt64(release.XPathSelectElement("assets/item[2]/size")?.Value ?? "0"),
                            InstallerName = release.XPathSelectElement("assets/item[2]/name")?.Value ?? "ScreenToGif.Setup.msi",
                        };

                        Application.Current.Dispatcher?.BeginInvoke(new Action(() => NotificationManager.AddNotification(string.Format(LocalizationHelper.Get("S.Updater.NewRelease.Info"),
                            Global.UpdateAvailable.Version), StatusType.Update, "update", PromptUpdate)));

                        //Download update to be installed when the app closes.
                        if (UserSettings.All.InstallUpdates && !string.IsNullOrEmpty(Global.UpdateAvailable.InstallerDownloadUrl))
                            await DownloadUpdate();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to check for updates on Github");
                return false;
            }
            finally
            {
                GC.Collect();
            }
        }

        private async Task CheckOnFosshub()
        {
            try
            {
                var proxy = WebHelper.GetProxy();
                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null,
                };

                using (var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.fosshub.com") })
                {
                    using (var response = await client.GetAsync("/feed/5bfc6fce8c9fe8186f809d24.json"))
                    {
                        var result = await response.Content.ReadAsStringAsync();

                        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(result)))
                        {
                            var ser = new DataContractJsonSerializer(typeof(FosshubResponse));
                            var obj = ser.ReadObject(ms) as FosshubResponse;

                            if (obj?.Release == null)
                                return;

                            var version = Version.Parse(obj.Release.Items[0].Version ?? "0.1");

                            if (version.Major == 0 || version <= Assembly.GetExecutingAssembly().GetName().Version)
                                return;

                            Global.UpdateAvailable = new UpdateAvailable
                            {
                                IsFromGithub = false,
                                Version = version,
                                PortableDownloadUrl = obj.Release.Items.FirstOrDefault(f => f.Title.EndsWith(".zip"))?.Link,
                                InstallerDownloadUrl = obj.Release.Items.FirstOrDefault(f => f.Title.EndsWith(".msi"))?.Link,
                            };

                            //With Fosshub, the download must be manual. 
                            Application.Current.Dispatcher?.BeginInvoke(new Action(() => NotificationManager.AddNotification(string.Format(LocalizationHelper.Get("S.Updater.NewRelease.Info"), Global.UpdateAvailable.Version), StatusType.Update, "update", PromptUpdate)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to check for updates on Fosshub");
            }
            finally
            {
                GC.Collect();
            }
        }

        internal async Task<bool> DownloadUpdate()
        {
            try
            {
                lock (UserSettings.Lock)
                {
                    if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolderResolved) || Global.UpdateAvailable.IsDownloading)
                        return false;

                    var folder = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Updates");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    Global.UpdateAvailable.ActivePath = Path.Combine(folder, Global.UpdateAvailable.ActiveName);

                    //Check if installer was alread downloaded.
                    if (File.Exists(Global.UpdateAvailable.ActivePath))
                    {
                        //Minor issue, if for some reason, the update has the exact same size, this won't work properly. I would need to check a hash.
                        if (GetSize(Global.UpdateAvailable.ActivePath) == Global.UpdateAvailable.ActiveSize)
                            return false;

                        File.Delete(Global.UpdateAvailable.ActivePath);
                    }

                    Global.UpdateAvailable.IsDownloading = true;
                }

                using (var webClient = new WebClient())
                {
                    webClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                    webClient.Proxy = WebHelper.GetProxy();

                    await webClient.DownloadFileTaskAsync(new Uri(Global.UpdateAvailable.ActiveDownloadUrl), Global.UpdateAvailable.ActivePath);
                }

                Global.UpdateAvailable.TaskCompletionSource?.TrySetResult(true);
                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to automatically download update");
                Global.UpdateAvailable.TaskCompletionSource?.TrySetResult(false);
                return false;
            }
            finally
            {
                Global.UpdateAvailable.IsDownloading = false;
            }
        }

        internal bool InstallUpdate(bool wasPromptedManually = false)
        {
            try
            {
                //No new release available.
                if (Global.UpdateAvailable == null)
                    return false;

                //TODO: Check if Windows is not turning off.

                //Prompt if:
                //Not configured to download the update automatically OR
                //Configured to download but set to prompt anyway OR
                //Download not completed (perharps because the notification was triggered by a query on Fosshub).
                if (UserSettings.All.PromptToInstall || !UserSettings.All.InstallUpdates || string.IsNullOrWhiteSpace(Global.UpdateAvailable.ActivePath))
                {
                    var download = new DownloadDialog { WasPromptedManually = wasPromptedManually };
                    var result = download.ShowDialog();

                    if (!result.HasValue || !result.Value)
                        return false;
                }

                //Only try to install if the update was downloaded.
                if (!File.Exists(Global.UpdateAvailable.ActivePath))
                    return false;

                if (UserSettings.All.PortableUpdate)
                {
                    //In portable mode, simply open the Zip file and close ScreenToGif.
                    Process.Start(Global.UpdateAvailable.PortablePath);
                    return true;
                }

                var files = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory).ToList();
                var isInstaller = files.Any(x => x.ToLowerInvariant().EndsWith("screentogif.visualelementsmanifest.xml"));
                var hasSharpDx = files.Any(x => x.ToLowerInvariant().EndsWith("sharpdx.dll"));
                var hasGifski = files.Any(x => x.ToLowerInvariant().EndsWith("gifski.dll"));
                var hasMenuShortcut = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "ScreenToGif.lnk"));
                var hasDesktopShortcut = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "ScreenToGif.lnk"));

                //MsiExec does not like relative paths.
                var isRelative = !string.IsNullOrWhiteSpace(Global.UpdateAvailable.InstallerPath) && !Path.IsPathRooted(Global.UpdateAvailable.InstallerPath);
                var nonRoot = isRelative ? Path.GetFullPath(Global.UpdateAvailable.InstallerPath) : Global.UpdateAvailable.InstallerPath;

                //msiexec /i PATH INSTALLDIR="" INSTALLAUTOMATICALLY=yes INSTALLPORTABLE=No ADDLOCAL=Binary
                //msiexec /a PATH TARGETDIR="" INSTALLAUTOMATICALLY=yes INSTALLPORTABLE=yes ADDLOCAL=Binary

                var startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $" {(isInstaller ? "/i" : "/a")} \"{nonRoot}\"" +
                                $" {(isInstaller ? "INSTALLDIR" : "TARGETDIR")}=\"{AppDomain.CurrentDomain.BaseDirectory}\" INSTALLAUTOMATICALLY=yes INSTALLPORTABLE={(isInstaller ? "no" : "yes")}" +
                                $" ADDLOCAL=Binary{(isInstaller ? ",Auxiliar" : "")}{(hasSharpDx ? ",SharpDX" : "")}{(hasGifski ? ",Gifski" : "")}" +
                                $" {(wasPromptedManually ? "RUNAFTER=yes" : "")}" +
                                (isInstaller ? $" INSTALLDESKTOPSHORTCUT={(hasDesktopShortcut ? "yes" : "no")} INSTALLSHORTCUT={(hasMenuShortcut ? "yes" : "no")}" : ""),
                    Verb = UserSettings.All.ForceUpdateAsAdmin ? "runas" : ""
                };

                using (var process = new Process { StartInfo = startInfo })
                    process.Start();

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to automatically install update");

                ErrorDialog.Ok("ScreenToGif", "It was not possible to install the update", ex.Message, ex);
                return false;
            }
        }

        private long GetSize(string path)
        {
            var info = new FileInfo(path);
            info.Refresh();

            return info.Length;
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