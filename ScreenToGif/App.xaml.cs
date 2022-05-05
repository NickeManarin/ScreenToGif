using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.InterProcessChannel;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel;
using ScreenToGif.Windows.Other;

namespace ScreenToGif;

public partial class App : IDisposable
{
    #region Properties

    internal static NotifyIcon NotifyIcon { get; private set; }

    internal static ApplicationViewModel MainViewModel { get; private set; }

    private Mutex _mutex;
    private bool _accepted;
    private readonly List<Exception> _exceptionList = new();
    private readonly object _lock = new();

    #endregion

    #region Events

    private void App_Startup(object sender, StartupEventArgs e)
    {
        Global.StartupDateTime = DateTime.Now;

        //Unhandled Exceptions.
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        //Increases the duration of the tooltip display.
        ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

        SetSecurityProtocol();

        //Parse arguments.
        Arguments.Prepare(e.Args);

        LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);
        ThemeHelper.SelectTheme(UserSettings.All.MainTheme);

        //Listen to changes in theme.
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        #region Download mode

        if (Arguments.IsInDownloadMode)
        {
            var downloader = new Downloader
            {
                DownloadMode = Arguments.DownloadMode,
                DestinationPath = Arguments.DownloadPath
            };
            downloader.ShowDialog();

            Environment.Exit(90);
            return;
        }

        #endregion

        #region Settings persistence mode

        if (Arguments.IsInSettingsMode)
        {
            SettingsPersistenceChannel.RegisterServer();
            return;
        }

        #endregion

        #region If set, it allows only one instance per user

        //The singleton works on a per-user and per-executable mode.
        //Meaning that a different user and/or a different executable instances can co-exist.
        //Part of this code won't work on debug mode, since the SetForegroundWindow() needs focus on the foreground window calling the method.
        if (UserSettings.All.SingleInstance && !Arguments.NewInstance)
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

                    //If the mutext failed to be accepted, it means that another process already opened it.
                    if (!_accepted)
                    {
                        var warning = true;

                        //Switch to the other app (get only one, if multiple available). Use name of assembly.
                        using (var process = Process.GetProcessesByName(thisProcess.ProcessName).FirstOrDefault(f => f.MainWindowHandle != thisProcess.MainWindowHandle))
                        {
                            if (process != null)
                            {
                                var handles = Native.Helpers.Windows.GetWindowHandlesFromProcess(process);

                                //Show the window before setting focus.
                                Native.External.User32.ShowWindow(handles.Count > 0 ? handles[0] : process.Handle, Domain.Enums.Native.ShowWindowCommands.Show);

                                //Set user the focus to the window.
                                Native.External.User32.SetForegroundWindow(handles.Count > 0 ? handles[0] : process.Handle);
                                warning = false;

                                InstanceSwitcherChannel.SendMessage(process.Id, e.Args);
                            }
                        }

                        //If no window available (app is in the system tray), display a warning.
                        if (warning)
                            Dialog.Ok(LocalizationHelper.Get("S.Warning.Single.Title"), LocalizationHelper.Get("S.Warning.Single.Header"), LocalizationHelper.Get("S.Warning.Single.Message"), Icons.Info);

                        Environment.Exit(0);
                        return;
                    }

                    //If this is the first instance, register the inter process channel to listen for other instances.
                    InstanceSwitcherChannel.RegisterServer(InstanceSwitch_Received);
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

        SetWorkaroundForDispatcher();

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

        //var test = new TestField(); test.ShowDialog(); Environment.Exit(1); return;
        //var test = new Windows.EditorEx(); test.ShowDialog(); return;
        //var test = new Windows.NewWebcam(); test.ShowDialog(); return;
        //var test = Settings.UserSettings.All.StartupTop;

        #region Tasks

        Task.Factory.StartNew(MainViewModel.ClearTemporaryFiles, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(async () => await MainViewModel.CheckForUpdates(),TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(MainViewModel.SendFeedback, TaskCreationOptions.LongRunning);

        #endregion

        #region Startup

        if (Arguments.Open)
            MainViewModel.Open.Execute(Arguments.WindownToOpen, true);
        else
            MainViewModel.Open.Execute(UserSettings.All.StartUp);

        #endregion
    }

    internal static void InstanceSwitch_Received(object _, InstanceSwitcherMessage message)
    {
        try
        {
            var args = message.Args;

            if (args?.Length > 0)
                Arguments.Prepare(args);

            if (Arguments.Open)
                MainViewModel.Open.Execute(Arguments.WindownToOpen, true);
            else
                MainViewModel.Open.Execute(UserSettings.All.StartUp);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Unable to execute arguments from IPC.");
        }
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
        if (e.ExceptionObject is not Exception exception)
            return;

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

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General)
            return;

        ThemeHelper.SelectTheme(UserSettings.All.MainTheme);

        if (UserSettings.All.GridColorsFollowSystem)
        {
            var isSystemUsingDark = ThemeHelper.IsSystemUsingDarkTheme();
            UserSettings.All.GridColor1 = isSystemUsingDark ? Constants.DarkEven : Constants.VeryLightEven;
            UserSettings.All.GridColor2 = isSystemUsingDark ? Constants.DarkOdd : Constants.VeryLightOdd;
        }
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

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
            SettingsExtension.ForceSave();
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

    private void SetSecurityProtocol()
    {
        try
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to set the network properties");
        }
    }

    private void SetWorkaroundForDispatcher()
    {
        try
        {
            if (UserSettings.All.WorkaroundQuota)
                BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Reset;

#if DEBUG

            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;

            BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Throw;

#endif
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to set the workaround for the quota crash");
        }
    }

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
        MainViewModel.RecorderGesture = screen ? Native.Helpers.Other.GetSelectKeyText(UserSettings.All.RecorderShortcut, UserSettings.All.RecorderModifiers, true, true) : "";
        MainViewModel.WebcamRecorderGesture = webcam ? Native.Helpers.Other.GetSelectKeyText(UserSettings.All.WebcamRecorderShortcut, UserSettings.All.WebcamRecorderModifiers, true, true) : "";
        MainViewModel.BoardRecorderGesture = board ? Native.Helpers.Other.GetSelectKeyText(UserSettings.All.BoardRecorderShortcut, UserSettings.All.BoardRecorderModifiers, true, true) : "";
        MainViewModel.EditorGesture = editor ? Native.Helpers.Other.GetSelectKeyText(UserSettings.All.EditorShortcut, UserSettings.All.EditorModifiers, true, true) : "";
        MainViewModel.OptionsGesture = options ? Native.Helpers.Other.GetSelectKeyText(UserSettings.All.OptionsShortcut, UserSettings.All.OptionsModifiers, true, true) : "";
        MainViewModel.ExitGesture = exit ? Native.Helpers.Other.GetSelectKeyText(UserSettings.All.ExitShortcut, UserSettings.All.ExitModifiers, true, true) : "";
    }

    private void ShowException(Exception exception)
    {
        lock(_lock)
        {
            //Avoid displaying an exception that is already being displayed.
            if (_exceptionList.Any(a => a.Message == exception.Message))
                return;

            //Adding to the list, so a second exception with the same name won't be displayed.
            _exceptionList.Add(exception);

            Current.Dispatcher.Invoke(() =>
            {
                if (Global.IsHotFix4055002Installed && exception is XamlParseException && exception.InnerException is TargetInvocationException)
                    ExceptionDialog.Ok(exception, "ScreenToGif", "Error while rendering visuals", exception.Message);
                else
                    ExceptionDialog.Ok(exception, "ScreenToGif", "Unhandled exception", exception.Message);
            });

            //By removing the exception, the same exception can be displayed later.
            _exceptionList.Remove(exception);
        }
    }

    public void Dispose()
    {
        if (_mutex != null && _accepted)
        {
            _mutex.ReleaseMutex();
            _accepted = false;
        }

        _mutex?.Dispose();
    }

    #endregion
}