using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Windows.Other;

namespace ScreenToGif
{
    public partial class App
    {
        #region Properties

        internal static NotifyIcon NotifyIcon { get; private set; }

        internal static ApplicationViewModel MainViewModel { get; set; }

        private Mutex _mutex;
        private bool _accepted;
        private readonly List<Exception> _exceptionList = new List<Exception>();
        private readonly object _lock = new object();

        #endregion

        #region Events

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Global.StartupDateTime = DateTime.Now;

            //Unhandled Exceptions.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            //Increases the duration of the tooltip display.
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
            
            #region Set network connection properties

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to set the network properties");
            }

            #endregion

            //Parse arguments.
            if (e.Args.Length > 0)
                Argument.Prepare(e.Args);

            LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);
            ThemeHelper.SelectTheme(UserSettings.All.MainTheme.ToString());

            #region If set, it allows only one instance per user

            //The singleton works on a per-user and per-executable mode.
            //Meaning that a different user and/or a different executable intances can co-exist.
            //Part of this code wont work on debug mode, since the SetForegroundWindow() needs focus on the foreground window calling the method.
            if (UserSettings.All.SingleInstance)
            {
                try
                {
                    using (var thisProcess = Process.GetCurrentProcess())
                    {
                        var user = System.Security.Principal.WindowsIdentity.GetCurrent().User;
                        var name = thisProcess.MainModule?.FileName ?? Assembly.GetEntryAssembly()?.Location ?? "ScreenToGif";
                        var location = Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
                        var mutexName = (user?.Value ?? Environment.UserName) + "_" + location;

                        _mutex = new Mutex(true, mutexName, out _accepted);

                        //If the mutext failed to be accepted, it means that another process already openned it.
                        if (!_accepted)
                        {
                            var warning = true;

                            //Switch to the other app (get only one, if multiple available). Use name of assembly.
                            using (var process = Process.GetProcessesByName(thisProcess.ProcessName).FirstOrDefault(f => f.MainWindowHandle != thisProcess.MainWindowHandle))
                            {
                                if (process != null)
                                {
                                    var handles = Native.GetWindowHandlesFromProcess(process);

                                    //Show the window before setting focus.
                                    Native.ShowWindow(handles.Count > 0 ? handles[0] : process.Handle, Native.ShowWindowEnum.Show);

                                    //Set user the focus to the window.
                                    Native.SetForegroundWindow(handles.Count > 0 ? handles[0] : process.Handle);
                                    warning = false;
                                }
                            }

                            //If no window available (app is in the system tray), display a warning.
                            if (warning)
                                Dialog.Ok(LocalizationHelper.Get("S.Warning.Single.Title"), LocalizationHelper.Get("S.Warning.Single.Header"), LocalizationHelper.Get("S.Warning.Single.Message"), Icons.Info);

                            Environment.Exit(0);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Impossible to check if another instance is running");
                }
            }

            #endregion

            //Render mode.
            RenderOptions.ProcessRenderMode = UserSettings.All.DisableHardwareAcceleration ? RenderMode.SoftwareOnly : RenderMode.Default;

            #region Net Framework

            if (!FrameworkHelper.HasFramework())
            {
                var ask = Dialog.Ask(LocalizationHelper.Get("S.Warning.Net.Title"), LocalizationHelper.Get("S.Warning.Net.Header"), LocalizationHelper.Get("S.Warning.Net.Message"));

                if (ask)
                {
                    Process.Start("http://go.microsoft.com/fwlink/?LinkId=2085155");
                    return;
                }
            }

            #endregion

            #region Set the workaround

            try
            {
                if (UserSettings.All.WorkaroundQuota)
                    BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Reset;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to set the workaround for the quota crash");
            }

            #if DEBUG

            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;

            BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Throw;
            
            #endif

            #endregion

            #region Net Framework HotFixes

            //Only runs on Windows 7 SP1.
            if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var search = new ManagementObjectSearcher("SELECT HotFixID FROM Win32_QuickFixEngineering WHERE HotFixID = 'KB4055002'").Get();
                        Global.IsHotFix4055002Installed = search.Count > 0;
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Error while trying to know if a hot fix was installed.");
                    }
                });
            }

            #endregion

            #region Tray icon and view model

            NotifyIcon = (NotifyIcon)FindResource("NotifyIcon");

            if (NotifyIcon != null)
            {
                NotifyIcon.Visibility = UserSettings.All.ShowNotificationIcon || UserSettings.All.StartMinimized || UserSettings.All.StartUp == 5 ? Visibility.Visible : Visibility.Collapsed;

                //Replace the old option with the new setting.
                if (UserSettings.All.StartUp == 5)
                {
                    UserSettings.All.StartMinimized = true;
                    UserSettings.All.ShowNotificationIcon = true;
                    UserSettings.All.StartUp = 0;
                }

                //using (var iconStream = GetResourceStream(new Uri("pack://application:,,,/Resources/Logo.ico"))?.Stream)
                //{
                //    if (iconStream != null)
                //        NotifyIcon.Icon = new System.Drawing.Icon(iconStream);
                //}
            }

            MainViewModel = (ApplicationViewModel)FindResource("AppViewModel") ?? new ApplicationViewModel();

            RegisterShortcuts();

            #endregion

            //var select = new SelectFolderDialog(); select.ShowDialog(); return;
            //var select = new TestField(); select.ShowDialog(); return;
            //var select = new Encoder(); select.ShowDialog(); return;
            //var select = new EditorEx(); select.ShowDialog(); return;

            #region Tasks

            Task.Factory.StartNew(MainViewModel.ClearTemporaryFiles, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(MainViewModel.CheckForUpdates, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(MainViewModel.SendFeedback, TaskCreationOptions.LongRunning);

            #endregion

            #region Startup

            //When starting minimized, the 
            if (UserSettings.All.StartMinimized)
                return;

            if (UserSettings.All.StartUp == 4 || Argument.FileNames.Any())
            {
                MainViewModel.OpenEditor.Execute(null);
                return;
            }

            if (UserSettings.All.StartUp < 1 || UserSettings.All.StartUp > 4)
            {
                MainViewModel.OpenLauncher.Execute(null);
                return;
            }

            if (UserSettings.All.StartUp == 1)
            {
                MainViewModel.OpenRecorder.Execute(null);
                return;
            }

            if (UserSettings.All.StartUp == 2)
            {
                MainViewModel.OpenWebcamRecorder.Execute(null);
                return;
            }

            if (UserSettings.All.StartUp == 3)
                MainViewModel.OpenBoardRecorder.Execute(null);

            #endregion
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogWriter.Log(e.Exception, "On dispacher unhandled exception - Unknown");

            try
            {
                ShowException(e.Exception);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while displaying the error.");
                //Ignored.
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!(e.ExceptionObject is Exception exception)) return;

            LogWriter.Log(exception, "Current domain unhandled exception - Unknown");

            try
            {
                ShowException(exception);
            }
            catch (Exception)
            {
                //Ignored.
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                //This is used when trying to load missing assemblies, which are not located in same folder as the main executable.

                var name = args.Name.Split(',').First();

                if (!name.StartsWith("SharpDX"))
                    return null;

                var path = Other.AdjustPath(UserSettings.All.SharpDxLocationFolder ?? "");

                return Assembly.LoadFrom(System.IO.Path.Combine(path, $"{name}.dll"));
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Error loading assemblies");
                return null;
            }
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                MutexList.RemoveAll();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to remove all mutexes of the opened projects.");
            }

            try
            {
                NotifyIcon?.Dispose();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to dispose the system tray icon.");
            }

            try
            {
                EncodingManager.StopAllEncodings();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to cancel all encodings.");
            }

            try
            {
                UserSettings.Save();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to save the user settings.");
            }

            try
            {
                if (_mutex != null && _accepted)
                {
                    _mutex.ReleaseMutex();
                    _accepted = false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to release the single instance mutex.");
            }

            try
            {
                HotKeyCollection.Default.Dispose();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to dispose the hotkeys.");
            }
        }

        #endregion

        #region Methods

        internal static void RegisterShortcuts()
        {
            //TODO: If startup/editor is open and focused, should I let the hotkeys work? 

            //Registers all shortcuts. 
            var screen = HotKeyCollection.Default.TryRegisterHotKey(UserSettings.All.RecorderModifiers, UserSettings.All.RecorderShortcut, () =>
                { if (!Global.IgnoreHotKeys && MainViewModel.OpenRecorder.CanExecute(null)) MainViewModel.OpenRecorder.Execute(null); }, true);

            var webcam = HotKeyCollection.Default.TryRegisterHotKey(UserSettings.All.WebcamRecorderModifiers, UserSettings.All.WebcamRecorderShortcut, () =>
                { if (!Global.IgnoreHotKeys && MainViewModel.OpenWebcamRecorder.CanExecute(null)) MainViewModel.OpenWebcamRecorder.Execute(null); }, true);

            var board = HotKeyCollection.Default.TryRegisterHotKey(UserSettings.All.BoardRecorderModifiers, UserSettings.All.BoardRecorderShortcut, () =>
                { if (!Global.IgnoreHotKeys && MainViewModel.OpenBoardRecorder.CanExecute(null)) MainViewModel.OpenBoardRecorder.Execute(null); }, true);

            var editor = HotKeyCollection.Default.TryRegisterHotKey(UserSettings.All.EditorModifiers, UserSettings.All.EditorShortcut, () =>
                { if (!Global.IgnoreHotKeys && MainViewModel.OpenEditor.CanExecute(null)) MainViewModel.OpenEditor.Execute(null); }, true);

            var options = HotKeyCollection.Default.TryRegisterHotKey(UserSettings.All.OptionsModifiers, UserSettings.All.OptionsShortcut, () =>
                { if (!Global.IgnoreHotKeys && MainViewModel.OpenOptions.CanExecute(null)) MainViewModel.OpenOptions.Execute(null); }, true);

            var exit = HotKeyCollection.Default.TryRegisterHotKey(UserSettings.All.ExitModifiers, UserSettings.All.ExitShortcut, () =>
                { if (!Global.IgnoreHotKeys && MainViewModel.ExitApplication.CanExecute(null)) MainViewModel.ExitApplication.Execute(null); }, true);

            //Updates the input gesture text of each command.
            MainViewModel.RecorderGesture = screen ? Native.GetSelectKeyText(UserSettings.All.RecorderShortcut, UserSettings.All.RecorderModifiers, true, true) : "";
            MainViewModel.WebcamRecorderGesture = webcam ? Native.GetSelectKeyText(UserSettings.All.WebcamRecorderShortcut, UserSettings.All.WebcamRecorderModifiers, true, true) : "";
            MainViewModel.BoardRecorderGesture = board ? Native.GetSelectKeyText(UserSettings.All.BoardRecorderShortcut, UserSettings.All.BoardRecorderModifiers, true, true) : "";
            MainViewModel.EditorGesture = editor ? Native.GetSelectKeyText(UserSettings.All.EditorShortcut, UserSettings.All.EditorModifiers, true, true) : "";
            MainViewModel.OptionsGesture = options ? Native.GetSelectKeyText(UserSettings.All.OptionsShortcut, UserSettings.All.OptionsModifiers, true, true) : "";
            MainViewModel.ExitGesture = exit ? Native.GetSelectKeyText(UserSettings.All.ExitShortcut, UserSettings.All.ExitModifiers, true, true) : "";
        }

        internal void ShowException(Exception exception)
        {
            lock(_lock)
            {
                //Avoid displaying an exception that is already being displayed.
                if (_exceptionList.Any(a => a.Message == exception.Message))
                    return;

                //Adding to the list, so a second exception with the same name won't be displayed.
                _exceptionList.Add(exception);

                Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Global.IsHotFix4055002Installed && exception is XamlParseException && exception.InnerException is TargetInvocationException)
                        ExceptionDialog.Ok(exception, "ScreenToGif", "Error while rendering visuals", exception.Message);
                    else
                        ExceptionDialog.Ok(exception, "ScreenToGif", "Unhandled exception", exception.Message);
                }));

                //By removing the exception, the same exception can be displayed later.  
                _exceptionList.Remove(exception);
            }
        }

        #endregion
    }
}