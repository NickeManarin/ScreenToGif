using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;
using ScreenToGif.Controls;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.ViewModel;

internal class ApplicationViewModel : ApplicationBaseViewModel
{
    #region Commands

    public IExtendedCommand<int, bool> Open
    {
        get
        {
            return new AdvancedRelayCommand<int, bool>
            {
                ExecuteAction = (startup, fromConsole) =>
                {
                    if (!fromConsole && UserSettings.All.StartMinimized)
                        startup = -1;

                    //If files are being sent via parameter, force the editor to open.
                    if (!fromConsole && Arguments.FileNames.Any())
                        startup = 4;

                    switch (startup)
                    {
                        case -1: //Minimized.
                        {
                            return;
                        }

                        case 1: //Screen recorder.
                        {
                            if (OpenRecorder.CanExecute(null))
                                OpenRecorder.Execute(null);
                            return;
                        }

                        case 2: //Webcam recorder.
                        {
                            if (OpenWebcamRecorder.CanExecute(null))
                                OpenWebcamRecorder.Execute(null);
                            return;
                        }

                        case 3: //Board recorder.
                        {
                            if (OpenBoardRecorder.CanExecute(null))
                                OpenBoardRecorder.Execute(null);
                            return;
                        }

                        case 4: //Editor.
                        {
                            OpenEditor.Execute(null);
                            return;
                        }

                        case 5: //Options.
                        {
                            OpenOptions.Execute(null);
                            return;
                        }

                        default: //Startup.
                        {
                            OpenLauncher.Execute(null);
                            return;
                        }
                    }
                }
            };
        }
    }

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
                    return Application.Current?.Windows.OfType<Window>().All(a => !(a is BaseRecorder)) ?? false;
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

    public ICommand OpenWebcamRecorder
    {
        get
        {
            return new RelayCommand
            {
                CanExecutePredicate = o =>
                {
                    //True if all windows are not Recorders.
                    return Application.Current?.Windows.OfType<Window>().All(a => !(a is BaseRecorder)) ?? false;
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
                    return Application.Current?.Windows.OfType<Window>().All(a => !(a is BaseRecorder)) ?? false;
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
                        ProcessHelper.StartWithShell("https://github.com/NickeManarin/ScreenToGif/wiki/Help");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Opening the Help link");
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

                    //Try to install the update, closing the app if successful.
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
                    return Application.Current?.Windows.OfType<BaseRecorder>().All(a => a.Stage != RecorderStages.Recording) ?? false;
                },
                ExecuteAction = a =>
                {
                    if (UserSettings.All.NotifyWhileClosingApp && !Dialog.Ask(LocalizationHelper.Get("S.Exiting.Title"), LocalizationHelper.Get("S.Exiting.Instruction"), LocalizationHelper.Get("S.Exiting.Message")))
                        return;

                    if (UserSettings.All.DeleteCacheWhenClosing)
                        StorageUtils.PurgeCache();

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
        if (UserSettings.All.ShowNotificationIcon && UserSettings.All.KeepOpen)
            return;

        //We only need to check loaded windows that have content, since any special window could be open.
        if (Application.Current.Windows.Cast<Window>().Count(window => window.HasContent) == 0)
        {
            //Install the available update on closing.
            if (UserSettings.All.InstallUpdates)
                InstallUpdate();

            if (UserSettings.All.DeleteCacheWhenClosing)
                StorageUtils.PurgeCache();

            Application.Current.Shutdown(2);
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
                            var rec = Application.Current.Windows.OfType<BaseRecorder>().FirstOrDefault();

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
                            var rec = Application.Current.Windows.OfType<BaseRecorder>().FirstOrDefault();

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
                            var rec = Application.Current.Windows.OfType<BaseRecorder>().FirstOrDefault();

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
                    //Minimize all windows, disabling before to prevent some behaviors.
                    foreach (var f in all)
                        f.IsEnabled = false;

                    foreach (var f in all)
                        f.WindowState = WindowState.Minimized;

                    foreach (var f in all)
                        f.IsEnabled = true;
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

    internal async Task CheckForUpdates(bool forceCheck = false)
    {
        Global.UpdateAvailable = null;

#if FULL_MULTI_MSIX_STORE
            return;
#endif

        if (!forceCheck && !UserSettings.All.CheckForUpdates)
            return;

        //If the app was installed by Chocolatey, avoid updating via normal means.
        if (await IsChocolateyPackage())
            return;

        //Try checking for the update on Github first then fallbacks to Fosshub.
        if (!await CheckOnGithub())
            await CheckOnFosshub();
    }

    private async Task<bool> IsChocolateyPackage()
    {
        try
        {
            //Binaries distributed via Chocolatey are of Installer or Portable types.
            if (IdentityHelper.ApplicationType != ApplicationTypes.FullSingle && IdentityHelper.ApplicationType != ApplicationTypes.DependantSingle)
                return false;

            //If Chocolatey is installed and ScreenToGif was installed via its service, it will be listed.
            var choco = await ProcessHelper.Start("choco list -l screentogif");

            if (!choco.Contains("screentogif"))
                return false;

            //The Portable package gets shimmed when installing via choco.
            //As for the Installer package, I'm letting it to be updated via normal means too (for now).
            var shim = await ProcessHelper.Start("$a='path to executable: '; (ScreenToGif.exe --shimgen-noop | Select-String $a) -split $a | ForEach-Object Trim");
            var path = ProcessHelper.GetEntryAssemblyPath();

            return shim.Contains(path);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Not possible to detect Chocolatey package.");
            return false;
        }
    }

    private async Task<bool> CheckOnGithub()
    {
        try
        {
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

            var proxy = WebHelper.GetProxy();
            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = proxy != null
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393");
            using var response = await client.GetAsync("https://api.github.com/repos/NickeManarin/ScreenToGif/releases/latest");
            var result = await response.Content.ReadAsStringAsync();

            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new System.Xml.XmlDictionaryReaderQuotas());
            var release = XElement.Load(jsonReader);

            var version = Version.Parse(release.XPathSelectElement("tag_name")?.Value ?? "0.1");

            if (version.Major == 0 || version <= Assembly.GetExecutingAssembly().GetName().Version)
                return true;

            ParseDownloadUrls(release, version);

            Application.Current.Dispatcher?.Invoke(() => NotificationManager.AddNotification(string.Format(LocalizationHelper.Get("S.Updater.NewRelease.Info"),
                Global.UpdateAvailable.Version), StatusType.Update, "update", PromptUpdate));

            //Download update to be installed when the app closes.
            if (UserSettings.All.InstallUpdates && Global.UpdateAvailable.HasDownloadLink)
                await DownloadUpdate();

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

    private bool ParseDownloadUrls(XElement release, Version version, bool fromGithub = true)
    {
        var moniker = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => "arm64"
        };

        switch (IdentityHelper.ApplicationType)
        {
            case ApplicationTypes.FullMultiMsix:
            {
                //Only get Msix files.
                //ScreenToGif.2.36.Package.x64.msix
                //ScreenToGif.2.36.Package.msix

                var package = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                {
                    var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                    return name.EndsWith(".package." + moniker + ".msix") || name.EndsWith("package.msix");
                });

                return SetDownloadDetails(fromGithub, version, release, package);
            }
            case ApplicationTypes.DependantSingle:
            {
                //Get portable or installer packages, light or not.
                //ScreenToGif.2.36.Light.Portable.x64.zip
                //ScreenToGif.2.36.Light.Portable.zip
                //Or
                //ScreenToGif.2.36.Light.Setup.x64.msi
                //ScreenToGif.2.36.Light.Setup.msi

                var portable = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                {
                    var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                    return name.EndsWith(".light.portable." + moniker + ".zip") || name.EndsWith(".light.portable.zip");
                });
                var installer = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                {
                    var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                    return name.EndsWith(".light.setup." + moniker + ".msi") || name.EndsWith(".light.setup.msi");
                });

                //If missing light (framework dependent) variant, download full package.
                if (installer == null)
                {
                    portable = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                    {
                        var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                        return name.EndsWith(".portable." + moniker + ".zip") || name.EndsWith(".portable.zip");
                    });
                    installer = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                    {
                        var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                        return name.EndsWith(".setup." + moniker + ".msi") || name.EndsWith(".setup.msi");
                    });
                }

                return SetDownloadDetails(fromGithub, version, release, installer, portable);
            }
            default:
            {
                //Get portable or installer packages, light or not.
                //ScreenToGif.2.36.Portable.x64.zip
                //ScreenToGif.2.36.Portable.zip
                //Or
                //ScreenToGif.2.36.Setup.x64.msi
                //ScreenToGif.2.36.Setup.msi

                var portable = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                {
                    var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                    return (name.EndsWith(".portable." + moniker + ".zip") || name.EndsWith("portable.zip")) && !name.Contains(".light.");
                });
                var installer = (release.Element("assets") ?? release.Element("items"))?.Elements().FirstOrDefault(f =>
                {
                    var name = (f.Element("name")?.Value ?? f.Element("title")?.Value ?? "").ToLower();

                    return (name.EndsWith(".setup." + moniker + ".msi") || name.EndsWith("setup.msi")) && !name.Contains(".light.");
                });

                return SetDownloadDetails(fromGithub, version, release, installer, portable);
            }
        }
    }

    private bool SetDownloadDetails(bool fromGithub, Version version, XElement release, XElement installer, XElement portable = null)
    {
        if (installer == null)
        {
            Global.UpdateAvailable = new UpdateAvailable
            {
                IsFromGithub = fromGithub,
                Version = version,
                Description = release.XPathSelectElement("body")?.Value ?? "",
                MustDownloadManually = true
            };

            return false;
        }

        if (fromGithub)
        {
            Global.UpdateAvailable = new UpdateAvailable
            {
                Version = version,
                Description = release.XPathSelectElement("body")?.Value ?? "",

                PortableDownloadUrl = portable?.Element("browser_download_url")?.Value ?? "",
                PortableSize = Convert.ToInt64(portable?.Element("size")?.Value ?? "0"),
                PortableName = portable?.Element("name")?.Value ?? "ScreenToGif.zip",

                InstallerDownloadUrl = installer.Element("browser_download_url")?.Value ?? "",
                InstallerSize = Convert.ToInt64(installer.Element("size")?.Value ?? "0"),
                InstallerName = installer.Element("name")?.Value ?? "ScreenToGif.Setup.msi"
            };

            return true;
        }

        Global.UpdateAvailable = new UpdateAvailable
        {
            IsFromGithub = false,
            Version = version,
            PortableDownloadUrl = portable?.Element("link")?.Value ?? "",
            InstallerDownloadUrl = installer.Element("link")?.Value ?? "",
        };

        return true;
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

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393");
            using var response = await client.GetAsync("https://www.fosshub.com/feed/5bfc6fce8c9fe8186f809d24.json");
            var result = await response.Content.ReadAsStringAsync();

            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new System.Xml.XmlDictionaryReaderQuotas());
            var release = XElement.Load(jsonReader);

            var version = Version.Parse(release.XPathSelectElement("release/items")?.FirstNode?.XPathSelectElement("version")?.Value ?? "0.1");

            if (version.Major == 0 || version <= Assembly.GetExecutingAssembly().GetName().Version)
                return;

            ParseDownloadUrls(release, version);

            //With Fosshub, the download must be manual.
            Application.Current.Dispatcher?.Invoke(() => NotificationManager.AddNotification(string.Format(LocalizationHelper.Get("S.Updater.NewRelease.Info"), Global.UpdateAvailable.Version),
                StatusType.Update, "update", PromptUpdate));
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

                //Check if installer was already downloaded.
                if (File.Exists(Global.UpdateAvailable.ActivePath))
                {
                    //Minor issue, if for some reason, the update has the exact same size, this won't work properly. I would need to check a hash.
                    if (GetSize(Global.UpdateAvailable.ActivePath) == Global.UpdateAvailable.ActiveSize)
                        return false;

                    File.Delete(Global.UpdateAvailable.ActivePath);
                }

                Global.UpdateAvailable.IsDownloading = true;
            }

            var proxy = WebHelper.GetProxy();
            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = proxy != null,
            };

            //TODO: Use HttpClientFactory
            //https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
            //https://marcominerva.wordpress.com/2019/03/13/using-httpclientfactory-with-wpf-on-net-core-3-0/

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393");

                var response = await client.GetAsync(Global.UpdateAvailable.ActiveDownloadUrl);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var fileInfo = new FileInfo(Global.UpdateAvailable.ActivePath);
                    await using var fileStream = fileInfo.OpenWrite();
                    await stream.CopyToAsync(fileStream);
                }
                else
                {
                    throw new FileNotFoundException("Impossible to download update.");
                }
            }

            Global.UpdateAvailable.MustDownloadManually = false;
            Global.UpdateAvailable.TaskCompletionSource?.TrySetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to automatically download update");
            Global.UpdateAvailable.MustDownloadManually = true;
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

            var runAfterwards = false;

            //Prompt if:
            //Not configured to download the update automatically OR
            //Configured to download but set to prompt anyway OR
            //Update binary detection failed (manual update required) OR
            //Download not completed (perharps because the notification was triggered by a query on Fosshub).
            if (UserSettings.All.PromptToInstall || !UserSettings.All.InstallUpdates || string.IsNullOrWhiteSpace(Global.UpdateAvailable.ActivePath) || Global.UpdateAvailable.MustDownloadManually)
            {
                var download = new DownloadDialog { WasPromptedManually = wasPromptedManually };
                var result = download.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return false;

                runAfterwards = download.RunAfterwards;
            }

            //Only try to install if the update was downloaded.
            if (!File.Exists(Global.UpdateAvailable.ActivePath))
                return false;

            if (UserSettings.All.PortableUpdate || IdentityHelper.ApplicationType == ApplicationTypes.FullMultiMsix)
            {
                //In portable or Msix mode, simply open the zip/msix file and close ScreenToGif.
                ProcessHelper.StartWithShell(Global.UpdateAvailable.ActivePath);
                return true;
            }

            //Detect installed components.
            var files = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory).ToList();
            var isInstaller = files.Any(x => x.ToLowerInvariant().EndsWith("screentogif.visualelementsmanifest.xml"));
            var hasGifski = files.Any(x => x.ToLowerInvariant().EndsWith("gifski.dll"));
            var hasDesktopShortcut = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ScreenToGif.lnk")) ||
                                     File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "ScreenToGif.lnk"));
            var hasMenuShortcut = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "ScreenToGif.lnk")) ||
                                  File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "ScreenToGif.lnk"));

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
                            $" ADDLOCAL=Binary{(isInstaller ? ",Auxiliar" : "")}{(hasGifski ? ",Gifski" : "")}" +
                            $" {(wasPromptedManually && runAfterwards ? "RUNAFTER=yes" : "")}" +
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

internal interface IExtendedCommand<in T, in TR>
{
    event EventHandler CanExecuteChanged;

    bool CanExecute(object parameter);

    void Execute(T parameter, TR secondParameter = default);
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

internal class AdvancedRelayCommand<T, TR> : IExtendedCommand<T, TR>
{
    public Predicate<object> CanExecutePredicate { get; set; }
    public Action<T, TR> ExecuteAction { get; set; }

    public AdvancedRelayCommand(Predicate<object> canExecute, Action<T, TR> execute)
    {
        CanExecutePredicate = canExecute;
        ExecuteAction = execute;
    }

    public AdvancedRelayCommand()
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

    public void Execute(T parameter, TR secondParamater = default)
    {
        ExecuteAction(parameter, secondParamater);
    }
}

internal class ObsoleteAdvancedRelayCommand : RoutedUICommand, ICommand
{
    public Predicate<object> CanExecutePredicate { get; set; }
    public Action<object> ExecuteAction { get; set; }

    public ObsoleteAdvancedRelayCommand()
    { }

    public ObsoleteAdvancedRelayCommand(string text, string name, Type ownerType, InputGestureCollection inputGestures) : base(text, name, ownerType, inputGestures)
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