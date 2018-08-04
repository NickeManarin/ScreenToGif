using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
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

        #endregion

        #region Events

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Global.StartupDateTime = DateTime.Now;

            //Unhandled Exceptions.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //Increases the duration of the tooltip display.
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            //Parse arguments.
            if (e.Args.Length > 0)
                Argument.Prepare(e.Args);

            LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);

            if (UserSettings.All.DisableHardwareAcceleration)
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            #region Net Framework

            var array = Type.GetType("System.Array");
            var method = array?.GetMethod("Empty");

            if (array == null || method == null)
            {
                var ask = Dialog.Ask("Missing Dependency", "Net Framework 4.6.1 is not present", "In order to properly use this app, you need to download the correct version of the .Net Framework. Open the web page to download?");

                if (ask)
                {
                    Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=49981");
                    return;
                }
            }

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
                NotifyIcon.Visibility = UserSettings.All.ShowNotificationIcon || UserSettings.All.StartUp == 5 ? Visibility.Visible : Visibility.Collapsed;

            MainViewModel = (ApplicationViewModel)FindResource("AppViewModel") ?? new ApplicationViewModel();

            RegisterShortcuts();

            #endregion

            //var select = new SelectFolderDialog(); select.ShowDialog(); return;
            //var select = new TestField(); select.ShowDialog(); return;
            //var select = new Encoder(); select.ShowDialog(); return;

            #region Tasks

            Task.Factory.StartNew(MainViewModel.ClearTemporaryFilesTask, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(MainViewModel.UpdateTask, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(MainViewModel.SendFeedback, TaskCreationOptions.LongRunning);

            #endregion

            #region Startup

            if (UserSettings.All.StartUp == 4 || Argument.FileNames.Any())
            {
                MainViewModel.OpenEditor.Execute(null);
                return;
            }

            if (UserSettings.All.StartUp == 0)
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

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            //TODO: Use a try catch for each one.

            MutexList.RemoveAll();

            NotifyIcon?.Dispose();

            UserSettings.Save();

            HotKeyCollection.Default.Dispose();
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogWriter.Log(e.Exception, "On Dispacher Unhandled Exception - Unknown");

            try
            {
                ShowException(e.Exception);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while displaying the error.");
                //Ignored.
            }

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!(e.ExceptionObject is Exception exception)) return;

            LogWriter.Log(exception, "Current Domain Unhandled Exception - Unknown");

            try
            {
                ShowException(exception);
            }
            catch (Exception)
            {
                //Ignored.
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
            if (Global.IsHotFix4055002Installed && exception is XamlParseException && exception.InnerException is TargetInvocationException)
                ExceptionDialog.Ok(exception, "ScreenToGif", "Error while rendering visuals", exception.Message);
            else
                ExceptionDialog.Ok(exception, "ScreenToGif", "Unhandled exception", exception.Message);
        }

        #endregion
    }
}